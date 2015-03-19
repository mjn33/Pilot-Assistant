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

        private FlightData flightData;
        private PID_Controller[] controllers = new PID_Controller[3];

        // Current mode
        private bool ssasMode = false;
        // Used to monitor the use of SAS_TOGGLE key for SSAS.
        private bool ssasToggleKey = false;
        // Used to monitor the use of SAS_HOLD key for SSAS.
        private bool ssasHoldKey = false;

        // A class to group together several variables relating to the state of an axis.
        private class AxisState
        {
            // Used to selectively enable SSAS on a per axis basis
            public bool enabled = true;
            // Used to monitor user input, and pause SSAS on a per axis basis
            public bool paused = false;
            // How long we delay control fading after an axis is unpaused
            public readonly double activationDelay;
            // This is the initial axis control divisor, just after an axis is unpaused
            public readonly double activationFadeInitial;
            // Time for activationFadeCurrent to go from "activationFadeInitial" to 1, measured in 1/100 of a second
            public readonly double activationTimeMax;
            public readonly double activationFadeConstant;
            // This is the current axis control divisor
            public double activationFadeCurrent = 1;
            // How much time has elapsed since this axis was unpaused
            public double activationTimeElapsed;

            public AxisState(double activationDelay, double activationFadeInitial, double activationTimeMax)
            {
                this.activationDelay = activationDelay;
                this.activationFadeInitial = activationFadeInitial;
                this.activationTimeMax = activationTimeMax;
                this.activationFadeConstant = (1 / activationTimeMax) * Math.Log(1 / activationFadeInitial);
            }
        }

        private AxisState[] axisState = new AxisState[3];

        // rollState: false = surface mode, true = vector mode
        private bool rollState = false;
        private Vector3d rollTarget = Vector3d.zero;

        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            flightData = new FlightData(FlightGlobals.ActiveVessel);

            // grab stock PID values
            PID_Controller pitch = new PID.PID_Controller(0.15, 0.0, 0.06, -1, 1, -0.2, 0.2, 3);
            PID_Controller roll = new PID.PID_Controller(0.1, 0.0, 0.06, -1, 1, -0.2, 0.2, 3);
            PID_Controller yaw = new PID.PID_Controller(0.15, 0.0, 0.06, -1, 1, -0.2, 0.2, 3);
            controllers[(int)SASList.Pitch] = pitch;
            controllers[(int)SASList.Roll] = roll;
            controllers[(int)SASList.Yaw] = yaw;

            // Set up a default preset that can be easily returned to
            PresetManager.Instance.InitDefaultSASTuning(controllers);
            PresetManager.Instance.InitDefaultStockSASTuning(flightData.Vessel.Autopilot.SAS);

            axisState[(int)SASList.Pitch] = new AxisState(20, 10, 75);
            axisState[(int)SASList.Roll] = new AxisState(20, 10, 75);
            axisState[(int)SASList.Yaw] = new AxisState(20, 10, 75);

            // register vessel
            flightData.Vessel.OnAutopilotUpdate += new FlightInputCallback(VesselController);
            GameEvents.onVesselChange.Add(VesselSwitch);
        }

        private void VesselSwitch(Vessel v)
        {
            flightData.Vessel.OnAutopilotUpdate -= new FlightInputCallback(VesselController);
            flightData.Vessel = v;
            flightData.Vessel.OnAutopilotUpdate += new FlightInputCallback(VesselController);
        }

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(VesselSwitch);
            PresetManager.Instance.SavePresetsToFile();
            ssasMode = false;
            ssasToggleKey = false;
            ssasHoldKey = false;

            for (int i = 0; i < controllers.Length; i++)
                controllers[i] = null;

            flightData.Vessel.OnAutopilotUpdate -= new FlightInputCallback(VesselController);
        }

        public void Update()
        {
            if (ssasMode)
                flightData.Vessel.ActionGroups[KSPActionGroup.SAS] = false;

            if (ssasMode && flightData.Vessel.staticPressure == 0)
            {
                // Try to seamlessly switch to stock SAS
                ToggleSSASMode();
            }

            KeyPressChanges();
        }

        private void KeyPressChanges()
        {
            // Respect current input locks
            if (InputLockManager.IsLocked(ControlTypes.ALL_SHIP_CONTROLS))
                return;

            if (ssasMode && GameSettings.SAS_TOGGLE.GetKeyDown())
            {
                ssasToggleKey = !ssasToggleKey;
                // If the change made SSAS operational, update target
                if (IsSSASOperational())
                    UpdateTarget();
            }

            // Allow for temporarily enabling/disabling SAS
            if (GameSettings.SAS_HOLD.GetKeyDown())
            {
                ssasHoldKey = true;
                // If the change made SSAS operational, update target
                if (IsSSASOperational())
                    UpdateTarget();
            }
            if (GameSettings.SAS_HOLD.GetKeyUp())
            {
                ssasHoldKey = false;
                // If the change made SSAS operational, update target
                if (IsSSASOperational())
                    UpdateTarget();
            }
        }

        private void VesselController(FlightCtrlState state)
        {
            flightData.UpdateAttitude();

            if (!IsSSASOperational())
                return;

            PauseManager(state); // manage activation of SAS axes depending on user input

            double vertResponse = 0;
            if (IsSSASAxisEnabled(SASList.Pitch))
                vertResponse = -GetController(SASList.Pitch).Response(flightData.Pitch);

            double hrztResponse = 0;
            // Don't follow target heading when getting close to poles.
            if (IsSSASAxisEnabled(SASList.Yaw) &&
                flightData.Vessel.latitude < 88 && flightData.Vessel.latitude > -88)
            {
                hrztResponse = -GetController(SASList.Yaw).Response(
                    Functions.CalcRelativeAngle(flightData.Heading, GetController(SASList.Yaw).SetPoint));
            }

            double rollRad = Math.PI / 180 * flightData.Roll;

            if ((!IsPaused(SASList.Pitch) && IsSSASAxisEnabled(SASList.Pitch)) ||
                (!IsPaused(SASList.Yaw) && IsSSASAxisEnabled(SASList.Yaw)))
            {
                state.pitch = (float)((vertResponse * Math.Cos(rollRad) - hrztResponse * Math.Sin(rollRad)) /
                                      axisState[(int)SASList.Pitch].activationFadeCurrent);
                state.yaw   = (float)((vertResponse * Math.Sin(rollRad) + hrztResponse * Math.Cos(rollRad)) /
                                      axisState[(int)SASList.Yaw].activationFadeCurrent);
            }

            RollResponse(state);

            UpdateActivationFade(SASList.Pitch);
            UpdateActivationFade(SASList.Yaw);
            UpdateActivationFade(SASList.Roll);
        }

        private void UpdateActivationFade(SASList id)
        {
            // If activationFadeCurrent has already reached 1, just return
            AxisState state = axisState[(int)id];
            if (state.activationFadeCurrent <= 1)
            {
                state.activationFadeCurrent = 1;
                return;
            }

            state.activationTimeElapsed += TimeWarp.fixedDeltaTime * 100.0; // 1 == 1/100th of a second
            if (state.activationTimeElapsed < state.activationDelay)
            {
                // Different actions depending on axis
                if (id == SASList.Pitch || id == SASList.Yaw)
                {
                    GetController(SASList.Yaw).SetPoint = flightData.Heading;
                    GetController(SASList.Pitch).SetPoint = flightData.Pitch;
                }
                else if (id == SASList.Roll)
                {
                    if (rollState)
                        rollTarget = flightData.Vessel.ReferenceTransform.right;
                    else
                        GetController(SASList.Roll).SetPoint = flightData.Roll;
                }
            }
            else
            {
                double a = state.activationFadeInitial;
                double k = state.activationFadeConstant;
                double t = state.activationTimeElapsed - state.activationDelay;
                state.activationFadeCurrent = Math.Max(a * Math.Exp(k * t), 1);
            }
        }

        private void RollResponse(FlightCtrlState state)
        {
            if (!IsPaused(SASList.Roll) && IsSSASAxisEnabled(SASList.Roll))
            {
                bool rollStateWas = rollState;
                // switch tracking modes
                if (rollState) // currently in vector mode
                {
                    if (flightData.Pitch < 25 && flightData.Pitch > -25)
                        rollState = false; // fall back to surface mode
                }
                else // surface mode
                {
                    if (flightData.Pitch > 30 || flightData.Pitch < -30)
                        rollState = true; // go to vector mode
                }

                // Above 30 degrees pitch, rollTarget should always lie on the horizontal plane of the vessel
                // Below 25 degrees pitch, use the surface roll logic.
                // Hysteresis on the switch ensures it doesn't bounce back and forth and lose the lock.
                if (rollState)
                {
                    if (!rollStateWas)
                    {
                        GetController(SASList.Roll).SetPoint = 0;
                        GetController(SASList.Roll).SkipDerivative = true;
                        rollTarget = flightData.Vessel.ReferenceTransform.right;
                    }

                    Vector3 proj = flightData.Vessel.ReferenceTransform.up * Vector3.Dot(flightData.Vessel.ReferenceTransform.up, rollTarget)
                        + flightData.Vessel.ReferenceTransform.right * Vector3.Dot(flightData.Vessel.ReferenceTransform.right, rollTarget);
                    double roll = Vector3.Angle(proj, rollTarget) * Math.Sign(Vector3.Dot(flightData.Vessel.ReferenceTransform.forward, rollTarget));

                    state.roll = (float)(GetController(SASList.Roll).Response(roll) /
                                         axisState[(int)SASList.Roll].activationFadeCurrent);
                }
                else
                {
                    if (rollStateWas)
                    {
                        GetController(SASList.Roll).SetPoint = flightData.Roll;
                        GetController(SASList.Roll).SkipDerivative = true;
                    }

                    double rollResponse = GetController(SASList.Roll).Response(
                        Functions.CalcRelativeAngle(flightData.Roll, GetController(SASList.Roll).SetPoint));
                    state.roll = (float)(rollResponse / axisState[(int)SASList.Roll].activationFadeCurrent);
                }
            }
        }

        public FlightData GetFlightData() { return flightData; }

        public PID_Controller GetController(SASList id)
        {
            return controllers[(int)id];
        }

        public void ToggleSSASMode()
        {
            // Swap modes, ensure operational state doesn't change.
            bool wasOperational = IsSSASOperational() || IsStockSASOperational();
            ssasMode = !ssasMode;
            SetOperational(wasOperational);
        }

        public void ToggleOperational()
        {
            if (ssasMode)
                SetOperational(!IsSSASOperational());
            else
                SetOperational(!IsStockSASOperational());
        }

        public void SetOperational(bool operational)
        {
            if (ssasMode)
            {
                bool wasOperational = IsSSASOperational();
                // Behave the same a stock SAS
                if (wasOperational != ssasToggleKey && wasOperational != operational)
                    ssasToggleKey = !operational;
                else if (wasOperational != operational)
                    ssasToggleKey = operational;
                // If only just switched on, update target
                if (IsSSASOperational() && !wasOperational)
                    UpdateTarget();
            }
            else
            {
                flightData.Vessel.ActionGroups[KSPActionGroup.SAS]
                    = operational;
            }
        }

        public bool IsSSASOperational()
        {
            // ssasHoldKey toggles the main state, i.e. active --> off, off --> active
            return (ssasToggleKey != ssasHoldKey) && ssasMode;
        }

        public bool IsStockSASOperational()
        {
            return flightData.Vessel.ActionGroups[KSPActionGroup.SAS];
        }

        public bool IsSSASAxisEnabled(SASList id)
        {
            return axisState[(int)id].enabled;
        }

        public void SetSSASAxisEnabled(SASList id, bool enabled)
        {
            axisState[(int)id].enabled = enabled;
        }

        public bool IsSSASMode()
        {
            return ssasMode;
        }

        private bool IsPaused(SASList id)
        {
            return axisState[(int)id].paused;
        }

        private void SetPaused(SASList id, bool val)
        {
            axisState[(int)id].paused = val;
        }

        private void ResetActivationFade(SASList id)
        {
            AxisState state = axisState[(int)id];
            state.activationFadeCurrent = state.activationFadeInitial;
            state.activationTimeElapsed = 0;
        }

        public void UpdatePreset()
        {
            SASPreset p = PresetManager.Instance.GetActiveSASPreset();
            if (p != null)
                p.Update(controllers);
            PresetManager.Instance.SavePresetsToFile();
        }

        public void RegisterNewPreset(string name)
        {
            PresetManager.Instance.RegisterSASPreset(controllers, name);
        }

        public void LoadPreset(SASPreset p)
        {
            PresetManager.Instance.LoadSASPreset(controllers, p);
        }

        public void UpdateStockPreset()
        {
            SASPreset p = PresetManager.Instance.GetActiveStockSASPreset();
            if (p != null)
                p.UpdateStock(flightData.Vessel.Autopilot.SAS);
            PresetManager.Instance.SavePresetsToFile();
        }

        public void RegisterNewStockPreset(string name)
        {
            PresetManager.Instance.RegisterStockSASPreset(flightData.Vessel.Autopilot.SAS, name);
        }

        public void LoadStockPreset(SASPreset p)
        {
            PresetManager.Instance.LoadStockSASPreset(flightData.Vessel.Autopilot.SAS, p);
        }

        public void UpdateTarget()
        {
            if (rollState)
                GetController(SASList.Roll).SetPoint = 0;
            else
                GetController(SASList.Roll).SetPoint = flightData.Roll;

            GetController(SASList.Pitch).SetPoint = flightData.Pitch;
            GetController(SASList.Yaw).SetPoint = flightData.Heading;

            rollTarget = flightData.Vessel.ReferenceTransform.right;

            ResetActivationFade(SASList.Pitch);
            ResetActivationFade(SASList.Roll);
            ResetActivationFade(SASList.Yaw);
        }

        private void PauseManager(FlightCtrlState state)
        {
            if (state.pitch != 0.0 || state.yaw != 0.0)
            {
                SetPaused(SASList.Pitch, true);
                SetPaused(SASList.Yaw, true);
            }
            else if (IsPaused(SASList.Pitch) || IsPaused(SASList.Yaw))
            {
                SetPaused(SASList.Pitch, false);
                SetPaused(SASList.Yaw, false);
                if (IsSSASAxisEnabled(SASList.Pitch) || IsSSASAxisEnabled(SASList.Yaw))
                {
                    GetController(SASList.Pitch).SetPoint = flightData.Pitch;
                    GetController(SASList.Yaw).SetPoint = flightData.Heading;
                    if (IsSSASAxisEnabled(SASList.Pitch))
                        ResetActivationFade(SASList.Pitch);
                    if (IsSSASAxisEnabled(SASList.Yaw))
                        ResetActivationFade(SASList.Yaw);
                }
            }

            if (state.roll != 0.0)
            {
                SetPaused(SASList.Roll, true);
            }
            else if (IsPaused(SASList.Roll))
            {
                SetPaused(SASList.Roll, false);
                if (IsSSASAxisEnabled(SASList.Roll))
                {
                    if (rollState)
                        rollTarget = flightData.Vessel.ReferenceTransform.right;
                    else
                        GetController(SASList.Roll).SetPoint = flightData.Roll;
                    ResetActivationFade(SASList.Roll);
                }
            }
        }
    }
}
