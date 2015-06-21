using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant
{
    using PID;
    using Utility;
    using UI;
    using Presets;

    public enum SASList
    {
        Pitch,
        Roll,
        Yaw
    }

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class SurfSAS : MonoBehaviour
    {
        // Singleton pattern, as opposed to using semi-static classes
        private static SurfSAS instance;
        public static SurfSAS Instance
        {
            get { return instance; }
        }

        // The roll angle below which pitch and yaw unlock seperately
        private const double ROLL_ANGLE_SYNC = 5;

        private SASPreset defaultSSASPreset;
        private SASPreset defaultStockPreset;

        private SASPreset activeSSASPreset = null;
        private SASPreset activeStockPreset = null;

        private FlightData flightData;
        private PID_Controller[] controllers = new PID_Controller[Enum.GetNames(typeof(SASList)).Length];

        // Current mode: SSAS = true, stock SAS = false
        private bool ssasMode = false;

        // A class to group together several variables relating to the state of an axis.
        private class AxisState
        {
            // Used to selectively enable SSAS on a per axis basis
            public bool enabled = true;
            // Used to monitor user input, and pause SSAS on a per axis basis
            public bool paused = false;
            // True if we are currently waiting for this axis to settle down after being paused
            public bool fadingIn = false;
        }

        // Used to keep track of changes in SAS being turned on/off
        private bool wasStockSASActive;
        // Used to track changes in "autopilot mode", so UpdateTarget can be performed
        private VesselAutopilot.AutopilotMode currentAutopilotMode = VesselAutopilot.AutopilotMode.StabilityAssist;
        // Used to track changes in "speed display mode", so orbitalTarget can be updated at the correct time
        private FlightUIController.SpeedDisplayModes currentSpeedMode = FlightUIController.SpeedDisplayModes.Surface;

        private AxisState[] axisState = new AxisState[3];

        // Only used when not in "surface" mode, i.e. orbital mode
        private Quaternion orbitalTarget = Quaternion.identity;

        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            flightData = new FlightData(FlightGlobals.ActiveVessel);

            // Initializing default SSAS preset
            PID_Tuning[] tunings = new PID_Tuning[Enum.GetNames(typeof(SASList)).Length];
            tunings[(int)SASList.Pitch] = new PID_Tuning(0.15, 0.0, 0.06, -1, 1, -0.2, 0.2, 3);
            tunings[(int)SASList.Roll]  = new PID_Tuning(0.1,  0.0, 0.06, -1, 1, -0.2, 0.2, 3);
            tunings[(int)SASList.Yaw]   = new PID_Tuning(0.15, 0.0, 0.06, -1, 1, -0.2, 0.2, 3);
            defaultSSASPreset = new SASPreset("Default", tunings);
            activeSSASPreset = defaultSSASPreset;

            // Initializing controllers from preset
            foreach (SASList id in Enum.GetValues(typeof(SASList)))
            {
                controllers[(int)id] = new PID_Controller(tunings[(int)id]);
                axisState[(int)id] = new AxisState();
            }

            // grab stock PID values
            defaultStockPreset = new SASPreset("Stock", flightData.Vessel.Autopilot.SAS);
            activeStockPreset = defaultStockPreset;

            // register vessel
            flightData.Vessel.OnPostAutopilotUpdate += new FlightInputCallback(VesselController);
            GameEvents.onVesselChange.Add(VesselSwitch);
        }

        private void VesselSwitch(Vessel v)
        {
            flightData.Vessel.OnPostAutopilotUpdate -= new FlightInputCallback(VesselController);
            flightData = new FlightData(v);
            flightData.Vessel.OnPostAutopilotUpdate += new FlightInputCallback(VesselController);
        }

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(VesselSwitch);
            PresetManager.Instance.SavePresetsToFile();
            ssasMode = false;

            for (int i = 0; i < controllers.Length; i++)
                controllers[i] = null;

            flightData.Vessel.OnPostAutopilotUpdate -= new FlightInputCallback(VesselController);
        }

        public void Update()
        {
            if (!wasStockSASActive && flightData.Vessel.ActionGroups[KSPActionGroup.SAS])
                UpdateTarget();

            // If we just switched to using the "stability assist" setting, we need to update the target
            if (currentAutopilotMode != VesselAutopilot.AutopilotMode.StabilityAssist &&
                flightData.Vessel.Autopilot.Mode == VesselAutopilot.AutopilotMode.StabilityAssist)
                UpdateTarget();

            if (currentSpeedMode == FlightUIController.SpeedDisplayModes.Surface &&
                currentSpeedMode != FlightUIController.speedDisplayMode)
                orbitalTarget = flightData.Vessel.transform.rotation;

            currentAutopilotMode = flightData.Vessel.Autopilot.Mode;
            currentSpeedMode     = FlightUIController.speedDisplayMode;
            wasStockSASActive    = flightData.Vessel.ActionGroups[KSPActionGroup.SAS];
        }

        private void VesselController(FlightCtrlState state)
        {
            flightData.UpdateAttitude();

            if (!IsSSASOperational || !flightData.Vessel.IsControllable)
                return;

            FadeInAxis(SASList.Pitch);
            FadeInAxis(SASList.Roll);
            FadeInAxis(SASList.Yaw);

            // Manage activation of SAS axes depending on user input
            PauseManager();

            // Facing vectors : vessel (vesRefTrans.up) and target (targetRot * Vector3.forward)
            Vessel v = flightData.Vessel;
            Transform vesRefTrans = v.ReferenceTransform.transform;
            Quaternion targetRot = TargetModeSwitch();
            double angleError = Vector3d.Angle(vesRefTrans.up, targetRot * Vector3d.forward);

            // Pitch / yaw response ratio. Original method from MJ attitude controller
            Vector3d relativeTargetFacing = vesRefTrans.rotation.Inverse() * targetRot * Vector3d.forward;
            Vector2d PYerror = (new Vector2d(relativeTargetFacing.x, -relativeTargetFacing.z)).normalized * angleError;

            // Roll error is dependant on path taken in pitch/yaw plane. Minimise unnecesary rotation by evaluating the roll error relative to that path
            Vector3d normVec = Vector3d.Cross(targetRot * Vector3d.forward, vesRefTrans.up).normalized; // axis normal to desired plane of travel
            //Quaternion rollTargetRot = Quaternion.AngleAxis((float)angleError, normVec) * targetRot; // rotation with facing aligned. Direction is taken care of by the orientation of the normVec
            Vector3d rollTargetRight = Quaternion.AngleAxis((float)angleError, normVec) * targetRot * Vector3d.right;
            double rollError = Vector3d.Angle(vesRefTrans.right, rollTargetRight) * Math.Sign(Vector3d.Dot(rollTargetRight, vesRefTrans.forward)); // signed angle difference between vessel.right and rollTargetRot.right

            state.roll  = GetCtrlState(SASList.Roll, rollError, v.angularVelocity.y * Mathf.Rad2Deg, state.roll);
            state.pitch = GetCtrlState(SASList.Pitch, PYerror.y, v.angularVelocity.x * Mathf.Rad2Deg, state.pitch);
            state.yaw   = GetCtrlState(SASList.Yaw, PYerror.x, v.angularVelocity.z * Mathf.Rad2Deg, state.yaw);
        }

        private bool HasAxisInput(SASList id)
        {
            switch (id)
            {
                case SASList.Pitch:
                    return Functions.HasPitchInput();
                case SASList.Roll:
                    return Functions.HasRollInput();
                case SASList.Yaw:
                    return Functions.HasYawInput();
                default:
                    throw new ArgumentException("Unhandled axis in HasAxisInput");
            }
        }

        private float GetCtrlState(SASList id, double error, double rate, double axisCtrlState)
        {
            bool useIntegral = true;
            if (!flightData.Vessel.checkLanded() && flightData.Vessel.IsControllable)
                useIntegral = false; // no integral when it can't do anything useful

            if (IsSSASAxisEnabled(id) && !IsPaused(id))
                return (float)GetController(id).Response(error, rate, useIntegral);
            else if (!HasAxisInput(id))
                return 0.0f; // kill off stock SAS inputs
            else
                return (float)axisCtrlState; // nothing happens if player input is present
        }


        Quaternion TargetModeSwitch()
        {
            Quaternion target = Quaternion.identity;
            Vessel vessel = flightData.Vessel;
            switch (vessel.Autopilot.Mode)
            {
                case VesselAutopilot.AutopilotMode.StabilityAssist:
                    if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface)
                    {
                        float hdgAngle   = (float)(IsSSASAxisEnabled(SASList.Yaw)   ? GetController(SASList.Yaw).SetPoint   : flightData.Heading);
                        float pitchAngle = (float)(IsSSASAxisEnabled(SASList.Pitch) ? GetController(SASList.Pitch).SetPoint : flightData.Pitch);

                        target = Quaternion.LookRotation(flightData.PlanetNorth, flightData.PlanetUp);
                        target = Quaternion.AngleAxis(hdgAngle, target * Vector3.up) * target; // heading rotation
                        target = Quaternion.AngleAxis(pitchAngle, target * -Vector3.right) * target; // pitch rotation
                    }
                    else
                        return orbitalTarget * Quaternion.Euler(-90, 0, 0);
                    break;
                case VesselAutopilot.AutopilotMode.Prograde:
                    if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Orbit)
                        target = Quaternion.LookRotation(vessel.obt_velocity, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface)
                        target = Quaternion.LookRotation(vessel.srf_velocity, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Target)
                        target = Quaternion.LookRotation(vessel.obt_velocity - vessel.targetObject.GetVessel().obt_velocity, flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.Retrograde:
                    if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Orbit)
                        target = Quaternion.LookRotation(-vessel.obt_velocity, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface)
                        target = Quaternion.LookRotation(vessel.srf_velocity, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Target)
                        target = Quaternion.LookRotation(vessel.targetObject.GetVessel().obt_velocity - vessel.obt_velocity, flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.RadialOut:
                    if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Orbit || FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Target)
                        target = Quaternion.LookRotation(flightData.ObtRadial, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface)
                        target = Quaternion.LookRotation(flightData.SrfRadial, flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.RadialIn:
                    if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Orbit || FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Target)
                        target = Quaternion.LookRotation(-flightData.ObtRadial, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface)
                        target = Quaternion.LookRotation(-flightData.SrfRadial, flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.Normal:
                    if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Orbit || FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Target)
                        target = Quaternion.LookRotation(flightData.ObtNormal, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface)
                        target = Quaternion.LookRotation(flightData.SrfNormal, flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.Antinormal:
                    if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Orbit || FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Target)
                        target = Quaternion.LookRotation(-flightData.ObtNormal, flightData.PlanetUp);
                    else if (FlightUIController.speedDisplayMode == FlightUIController.SpeedDisplayModes.Surface)
                        target = Quaternion.LookRotation(-flightData.SrfNormal, flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.Target:
                    if (vessel.targetObject != null)
                        target = Quaternion.LookRotation(vessel.targetObject.GetVessel().GetWorldPos3D() - vessel.GetWorldPos3D(), flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.AntiTarget:
                    if (vessel.targetObject != null)
                        target = Quaternion.LookRotation(vessel.GetWorldPos3D() - vessel.targetObject.GetVessel().GetWorldPos3D(), flightData.PlanetUp);
                    break;
                case VesselAutopilot.AutopilotMode.Maneuver:
                    if (vessel.patchedConicSolver.maneuverNodes != null && vessel.patchedConicSolver.maneuverNodes.Count > 0)
                        target = vessel.patchedConicSolver.maneuverNodes[0].nodeRotation;
                    break;
            }
            float rollAngle = (float)(IsSSASAxisEnabled(SASList.Roll) ? GetController(SASList.Roll).SetPoint : flightData.Roll);
            target = Quaternion.AngleAxis(-rollAngle, target * Vector3.forward) * target; // roll rotation
            return target;
        }

        private void StartFadeInAxis(SASList id)
        {
            axisState[(int)id].fadingIn = true;
            // Clear the integral
            GetController(id).Clear();
        }

        /// <summary>
        /// Wait for rate of rotation to fall below 10 degrees / s before locking in the target.
        /// Derivative action only until that time.
        /// </summary>
        private void FadeInAxis(SASList id)
        {
            if (!axisState[(int)id].fadingIn)
                return;

            double axisValue = 0.0;
            double axisAngularVel = 0.0;

            switch (id)
            {
                case SASList.Pitch:
                    axisValue = flightData.Pitch;
                    axisAngularVel = flightData.Vessel.angularVelocity.x;
                    break;
                case SASList.Roll:
                    axisValue = flightData.Roll;
                    axisAngularVel = flightData.Vessel.angularVelocity.y;
                    break;
                case SASList.Yaw:
                    axisValue = flightData.Heading;
                    axisAngularVel = flightData.Vessel.angularVelocity.z;
                    break;
                default:
                    throw new ArgumentException("Unhandled axis in FadeInAxis");
            }

            GetController(id).SetPoint = axisValue;
            if (Math.Abs(axisAngularVel) > 0.1745329252) // ~10 degrees in rad
            {
                GetController(id).SetPoint = axisValue;
            }
            else
            {
                orbitalTarget = flightData.Vessel.transform.rotation;
                axisState[(int)id].fadingIn = false;
            }
        }

        public PID_Controller GetController(SASList id)
        {
            return controllers[(int)id];
        }

        public void ToggleSSASMode()
        {
            // Swap modes, don't touch axis state
            ssasMode = !ssasMode;
        }

        public void ToggleOperational()
        {
            SetOperational(!flightData.Vessel.ActionGroups[KSPActionGroup.SAS]);
        }

        public void SetOperational(bool operational)
        {
            if (ssasMode)
            {
                bool wasOperational = flightData.Vessel.ActionGroups[KSPActionGroup.SAS];
                flightData.Vessel.ActionGroups[KSPActionGroup.SAS] = operational;
                // If only just switched on, update target
                if (operational && !wasOperational)
                    UpdateTarget();
            }
            else
            {
                flightData.Vessel.ActionGroups[KSPActionGroup.SAS] = operational;
            }
        }

        public bool IsSSASAxisEnabled(SASList id)
        {
            return axisState[(int)id].enabled;
        }

        public void SetSSASAxisEnabled(SASList id, bool enabled)
        {
            axisState[(int)id].enabled = enabled;
        }

        private bool IsPaused(SASList id)
        {
            return axisState[(int)id].paused;
        }

        private void SetPaused(SASList id, bool val)
        {
            axisState[(int)id].paused = val;
        }

        public void UpdateTarget()
        {
            StartFadeInAxis(SASList.Pitch);
            StartFadeInAxis(SASList.Roll);
            StartFadeInAxis(SASList.Yaw);
            orbitalTarget = flightData.Vessel.transform.rotation;
        }

        private void PauseManager()
        {
            bool hasPitch   = Functions.HasPitchInput();
            bool hasRoll    = Functions.HasRollInput();
            bool hasYaw     = Functions.HasYawInput();
            bool largeRoll  = Math.Abs(flightData.Roll) > ROLL_ANGLE_SYNC;
            double absPitch = Math.Abs(flightData.Pitch);
            // If the pitch control is not paused, and there is pitch input or there is yaw input and the bank angle is
            // greater than 5 degrees, pause the pitch lock
            if (!IsPaused(SASList.Pitch) && (hasPitch || (hasYaw && largeRoll)))
                SetPaused(SASList.Pitch, true);
            // If the pitch control is paused, and there is no pitch input, and there is no yaw input or the bank angle
            // is less than 5 degrees, unpause the pitch lock
            else if (IsPaused(SASList.Pitch) && !hasPitch && (!hasYaw || !largeRoll))
            {
                SetPaused(SASList.Pitch, false);
                if (IsSSASAxisEnabled(SASList.Pitch))
                    StartFadeInAxis(SASList.Pitch);
            }

            // If the heading control is not paused, and there is yaw input input or there is pitch input and the bank
            // angle is greater than 5 degrees, pause the heading lock
            if (!IsPaused(SASList.Yaw) && (hasYaw || (hasPitch && largeRoll)))
                SetPaused(SASList.Yaw, true);
            // If the heading control is paused, and there is no yaw input, and there is no pitch input or the bank
            // angle is less than 5 degrees, unpause the heading lock
            else if (IsPaused(SASList.Yaw) && !hasYaw && (!hasPitch || !largeRoll))
            {
                SetPaused(SASList.Yaw, false);
                if (IsSSASAxisEnabled(SASList.Yaw))
                    StartFadeInAxis(SASList.Yaw);
            }

            // If the roll control is not paused, and there is roll input or the vessel pitch is > 70 degrees and there
            // is pitch/yaw input
            if (!IsPaused(SASList.Roll) && (hasRoll || (absPitch > 70 && (hasPitch || hasYaw))))
                SetPaused(SASList.Roll, true);
            // If the roll control is paused, and there is not roll input and not any pitch/yaw input if pitch < 60
            // degrees
            else if (IsPaused(SASList.Roll) && !(hasRoll || (absPitch > 60 && (hasPitch || hasYaw))))
            {
                SetPaused(SASList.Roll, false);
                if (IsSSASAxisEnabled(SASList.Roll))
                    StartFadeInAxis(SASList.Roll);
            }
        }

        public SASPreset ActiveStockPreset
        {
            get { return activeStockPreset; }
            set
            {
                if (!value.IsStockPreset)
                    throw new ArgumentException("SSAS preset set where stock preset expected.");
                activeStockPreset = value;
            }
        }

        public SASPreset ActiveSSASPreset
        {
            get { return activeSSASPreset; }
            set
            {
                if (value.IsStockPreset)
                    throw new ArgumentException("Stock preset set where SSAS preset expected.");
                activeSSASPreset = value;
            }
        }

        public SASPreset DefaultStockPreset
        {
            get { return defaultStockPreset; }
        }

        public SASPreset DefaultSSASPreset
        {
            get { return defaultSSASPreset; }
        }

        public PID_Controller[] Controllers
        {
            get { return controllers; }
        }

        public FlightData FlightData
        {
            get { return flightData; }
        }

        public bool IsSSASOperational
        {
            get { return flightData.Vessel.ActionGroups[KSPActionGroup.SAS] && ssasMode; }
        }

        public bool IsStockSASOperational
        {
            get { return flightData.Vessel.ActionGroups[KSPActionGroup.SAS] && !ssasMode; }
        }

        public bool IsSSASMode
        {
            get { return ssasMode; }
        }
    }
}
