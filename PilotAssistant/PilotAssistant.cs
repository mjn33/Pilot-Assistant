using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace PilotAssistant
{
    using Presets;
    using Utility;
    using PID;
    using UI;

    public enum PIDList
    {
        HdgBank,
        HdgYaw,
        Aileron,
        Rudder,
        Altitude,
        VertSpeed,
        Elevator,
        Throttle
    }

    public class PilotAssistant
    {
        private PilotAssistantModule module;
        private PAMainWindow gui;

        private PAPreset defaultPAPreset;

        private PAPreset activePAPreset;

        // Reference to FlightData stored in vessel module, here for convenience
        private FlightData flightData;
        private PID_Controller[] controllers = new PID_Controller[Enum.GetNames(typeof(PIDList)).Length];

        // Whether PA has been paused by the user, does not account for SAS being turned on.
        // Use SASCheck() as well.
        private bool isPaused = false;
        // Roll Controller
        private bool isHdgActive = false;
        // Pitch Controller
        private bool isVertActive = false;
        // Altitude / vertical speed
        private bool isAltitudeHoldActive = false;
        // Wing leveller / Heading control
        private bool isWingLvlActive = false;
        // Surface speed / throttle control
        private bool isThrottleActive = false;

        public PilotAssistant(PilotAssistantModule module)
        {
            this.module = module;
            this.gui = new PAMainWindow(this);
            this.flightData = module.FlightData;
        }

        public void Start()
        {
            // Initializing default PA preset
            PID_Tuning[] paTunings = new PID_Tuning[Enum.GetNames(typeof(PIDList)).Length];
            paTunings[(int)PIDList.HdgBank]   = new PID_Tuning(2,    0.1,   0,    -30, 30, -0.5,  0.5);
            paTunings[(int)PIDList.HdgYaw]    = new PID_Tuning(0,    0,     0.01, -2,  2,  -0.5,  0.5);
            paTunings[(int)PIDList.Aileron]   = new PID_Tuning(0.02, 0.005, 0.01, -1,  1,  -0.4,  0.4);
            paTunings[(int)PIDList.Rudder]    = new PID_Tuning(0.1,  0.08,  0.05, -1,  1,  -0.4,  0.4);
            paTunings[(int)PIDList.Altitude]  = new PID_Tuning(0.15, 0.01,  0,    -50, 50, -0.01, 0.01);
            paTunings[(int)PIDList.VertSpeed] = new PID_Tuning(2,    0.8,   2,    -10, 10, -5,    5);
            paTunings[(int)PIDList.Elevator]  = new PID_Tuning(0.05, 0.01,  0.1,  -1,  1,  -0.4,  0.4);
            // Was: Kp -- 0.2, Ki -- 0.08, Kd -- 0.1
            // FIXME: This doesn't work as well as it could with AJE
            paTunings[(int)PIDList.Throttle]  = new PID_Tuning(0.15, 0.1,   0.5,  -1,  0,  -1,    0.4);
            defaultPAPreset = new PAPreset("Default", paTunings);
            activePAPreset = defaultPAPreset;

            // Initializing controllers from preset
            foreach (PIDList id in Enum.GetValues(typeof(PIDList)))
            {
                controllers[(int)id] = new PID_Controller(paTunings[(int)id]);
            }

            // PID inits
            GetController(PIDList.Aileron).Tuning.InMin = -180;
            GetController(PIDList.Aileron).Tuning.InMax = 180;
            GetController(PIDList.Altitude).Tuning.InMin = 0;
            GetController(PIDList.Throttle).Tuning.InMin = 0;
        }

        public void OnGUI()
        {
            gui.OnGUI();
        }

        public PID_Controller GetController(PIDList id)
        {
            // Make accessing controllers a bit cleaner
            return controllers[(int)id];
        }

        public void OnDestroy()
        {
            isHdgActive = false;
            isVertActive = false;

            for (int i = 0; i < controllers.Length; i++)
                controllers[i] = null;
        }

        public void Update()
        {
            KeyPressChanges();
        }

        public void VesselController(FlightCtrlState state)
        {
            if (isPaused || SASCheck())
                return;

            bool useIntegral = !flightData.Vessel.checkLanded() && flightData.Vessel.IsControllable;

            // Heading Control
            if (isHdgActive)
            {
                // Don't follow target heading when getting close to poles.
                if (!isWingLvlActive &&
                    flightData.Vessel.latitude < 88 && flightData.Vessel.latitude > -88)
                {
                    // Calculate the bank angle response based on the current heading
                    double hdgBankReponse = GetController(PIDList.HdgBank).Response(
                        Functions.CalcRelativeAngle(flightData.Heading, GetController(PIDList.HdgBank).SetPoint), useIntegral);
                    // Aileron setpoint updated, bank angle also used for yaw calculations (don't go direct to rudder
                    // because we want yaw stabilisation *or* turn assistance)
                    GetController(PIDList.Aileron).SetPoint = hdgBankReponse;
                    GetController(PIDList.HdgYaw).SetPoint = hdgBankReponse;
                    GetController(PIDList.Rudder).SetPoint = -GetController(PIDList.HdgYaw).Response(flightData.Yaw, useIntegral);
                }
                else
                {
                    GetController(PIDList.Aileron).SetPoint = 0;
                    GetController(PIDList.Rudder).SetPoint = 0;
                }
                state.roll = (float)Functions.Clamp(GetController(PIDList.Aileron).Response(-flightData.Roll, useIntegral) + state.roll, -1, 1);
                state.yaw = (float)Functions.Clamp(GetController(PIDList.Rudder).Response(flightData.Yaw, useIntegral), -1, 1);
            }

            if (isVertActive)
            {
                // Set requested vertical speed
                if (isAltitudeHoldActive)
                    GetController(PIDList.VertSpeed).SetPoint = -GetController(PIDList.Altitude).Response(flightData.Vessel.altitude, useIntegral);

                GetController(PIDList.Elevator).SetPoint = -GetController(PIDList.VertSpeed).Response(flightData.Vessel.verticalSpeed, useIntegral);
                state.pitch = (float)Functions.Clamp(-GetController(PIDList.Elevator).Response(flightData.AoA, useIntegral), -1, 1);
            }

            if (isThrottleActive && GetController(PIDList.Throttle).SetPoint != 0)
            {
                double response = -GetController(PIDList.Throttle).Response(flightData.Vessel.srfSpeed, useIntegral);
                state.mainThrottle = (float)Functions.Clamp(response, 0, 1);
            }
            else if (isThrottleActive && GetController(PIDList.Throttle).SetPoint == 0)
                // Easy case, we don't want any throttle for this target speed
                state.mainThrottle = 0;
        }

        public void SetHdgActive(double newHdg)
        {
            // Set heading control on to specified heading
            GetController(PIDList.HdgBank).SetPoint = newHdg;
            GetController(PIDList.HdgYaw).SetPoint = newHdg;
            gui.UpdateHeadingField();
            isHdgActive = true;
            module.SurfSAS.SetOperational(false);
            isPaused = false;
        }

        public void SetVertSpeedActive(double newSpd)
        {
            // Set vertical control on
            GetController(PIDList.VertSpeed).SetPoint = newSpd;
            gui.UpdateVertSpeedField();
            isVertActive = true;
            isAltitudeHoldActive = false;
            module.SurfSAS.SetOperational(false);
            isPaused = false;
        }

        public void SetAltitudeHoldActive(double newAlt)
        {
            // Set vertical control on
            GetController(PIDList.Altitude).SetPoint = newAlt;
            gui.UpdateAltitudeField();
            isVertActive = true;
            isAltitudeHoldActive = true;
            module.SurfSAS.SetOperational(false);
            isPaused = false;
        }

        public void SetThrottleActive(double newSrfSpeed)
        {
            GetController(PIDList.Throttle).SetPoint = newSrfSpeed;
            gui.UpdateSrfSpeedField();
            isThrottleActive = true;
            module.SurfSAS.SetOperational(false);
            isPaused = false;
        }

        public void ToggleHdg()
        {
            isHdgActive = !isHdgActive;
            if (isHdgActive)
            {
                // Set heading control on, use current heading
                GetController(PIDList.HdgBank).SetPoint = flightData.Heading;
                GetController(PIDList.HdgYaw).SetPoint = flightData.Heading; // added
                gui.UpdateHeadingField();
                module.SurfSAS.SetOperational(false);
                isPaused = false;
            }
            else
            {
                // Turn it off
                GetController(PIDList.HdgBank).Clear();
                GetController(PIDList.HdgYaw).Clear();
                GetController(PIDList.Aileron).Clear();
                GetController(PIDList.Rudder).Clear();
            }
        }

        public void ToggleWingLvl()
        {
            isWingLvlActive = !isWingLvlActive;
            if (!isWingLvlActive)
            {
                GetController(PIDList.HdgBank).SetPoint = flightData.Heading;
                GetController(PIDList.HdgYaw).SetPoint = flightData.Heading;
            }
        }

        public void ToggleVert()
        {
            isVertActive = !isVertActive;
            if (isVertActive)
            {
                if (isAltitudeHoldActive)
                {
                    GetController(PIDList.Altitude).SetPoint = flightData.Vessel.altitude;
                    gui.UpdateAltitudeField();
                }
                else
                {
                    GetController(PIDList.VertSpeed).SetPoint = flightData.Vessel.verticalSpeed;
                    gui.UpdateVertSpeedField();
                }
                module.SurfSAS.SetOperational(false);
                isPaused = false;
            }
            else
            {
                // Turn it off
                GetController(PIDList.Altitude).Clear();
                GetController(PIDList.HdgBank).Clear();
                GetController(PIDList.Elevator).Clear();
            }
        }

        public void ToggleAltitudeHold()
        {
            isAltitudeHoldActive = !isAltitudeHoldActive;
            if (isAltitudeHoldActive)
            {
                GetController(PIDList.Altitude).SetPoint = flightData.Vessel.altitude;
                gui.UpdateAltitudeField();
            }
            else
            {
                GetController(PIDList.VertSpeed).SetPoint = flightData.Vessel.verticalSpeed;
                gui.UpdateVertSpeedField();
            }
        }

        public void ToggleThrottleControl()
        {
            isThrottleActive = !isThrottleActive;
            if (isThrottleActive)
            {
                GetController(PIDList.Throttle).SetPoint = flightData.Vessel.srfSpeed;
                gui.UpdateSrfSpeedField();
                module.SurfSAS.SetOperational(false);
                isPaused = false;
            }
        }

        private bool SASCheck()
        {
            return module.SurfSAS.IsSSASOperational ||
                   module.SurfSAS.IsStockSASOperational;
        }

        private void KeyPressChanges()
        {
            // Respect current input locks
            if (InputLockManager.IsLocked(ControlTypes.ALL_SHIP_CONTROLS))
                return;
            bool mod = GameSettings.MODIFIER_KEY.GetKey();

            // Pause key
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                // When active and paused, unpause.
                if ((IsHdgActive || IsVertActive) && (isPaused || SASCheck()))
                {
                    isPaused = false;
                    module.SurfSAS.SetOperational(false);
                    GeneralUI.PostMessage("Pilot assistant unpaused.");
                }
                // Otherwise, when active and not paused, pause.
                else if (IsHdgActive || IsVertActive)
                {
                    isPaused = true;
                    GeneralUI.PostMessage("Pilot assistant paused.");
                }
            }

            // SAS activation change, only show messages when active and not paused.
            if ((GameSettings.SAS_TOGGLE.GetKeyDown() || GameSettings.SAS_HOLD.GetKeyDown() || GameSettings.SAS_HOLD.GetKeyUp())
                && !isPaused && (IsHdgActive || IsVertActive))
            {
                if (SASCheck())
                    GeneralUI.PostMessage("Pilot Assistant control handed to SAS.");
                else
                    GeneralUI.PostMessage("Pilot Assistant control retrieved from SAS.");
            }

            // Level wings and set vertical speed to 0.
            if (mod && Input.GetKeyDown(KeyCode.X))
            {
                // Set controller and modes.
                GetController(PIDList.VertSpeed).SetPoint = 0;
                gui.UpdateVertSpeedField();
                isVertActive = true;
                isAltitudeHoldActive = false;
                isHdgActive = true;
                isWingLvlActive = true;
                // Make sure we are not paused and SAS is off.
                isPaused = false;
                module.SurfSAS.SetOperational(false);
                GeneralUI.PostMessage("Pilot Assistant is levelling off.");
            }

            // Only update target when not paused.
            if (!isPaused && !SASCheck())
            {
                double scale;
                if (FlightInputHandler.fetch.precisionMode)
                    scale = mod ? 0.01 : 0.1;
                else
                    scale = mod ? 10 : 1;

                // Update heading based on user control input
                if (isHdgActive && !isWingLvlActive)
                {
                    double hdg = GetController(PIDList.HdgBank).SetPoint;
                    if (GameSettings.YAW_LEFT.GetKey())
                        hdg -= 0.4 * scale;
                    else if (GameSettings.YAW_RIGHT.GetKey())
                        hdg += 0.4 * scale;
                    else if (!GameSettings.AXIS_YAW.IsNeutral())
                        hdg += 0.4 * scale * GameSettings.AXIS_YAW.GetAxis();

                    if (hdg < 0)
                        hdg += 360;
                    else if (hdg > 360)
                        hdg -= 360;
                    GetController(PIDList.HdgBank).SetPoint = hdg;
                    GetController(PIDList.HdgYaw).SetPoint = hdg;
                    gui.UpdateHeadingField();
                }

                // Update target vertical speed based on user control input
                if (isVertActive && !isAltitudeHoldActive)
                {
                    double vert = GetController(PIDList.VertSpeed).SetPoint;
                    if (GameSettings.PITCH_DOWN.GetKey())
                        vert -= 0.4 * scale;
                    else if (GameSettings.PITCH_UP.GetKey())
                        vert += 0.4 * scale;
                    else if (!GameSettings.AXIS_PITCH.IsNeutral())
                        vert += 0.4 * scale * GameSettings.AXIS_PITCH.GetAxis();

                    GetController(PIDList.VertSpeed).SetPoint = vert;
                    gui.UpdateVertSpeedField();
                }

                // Update target altitude based on user control input
                if (isVertActive && isAltitudeHoldActive)
                {
                    double alt = GetController(PIDList.Altitude).SetPoint;
                    if (GameSettings.PITCH_DOWN.GetKey())
                        alt -= 4 * scale;
                    else if (GameSettings.PITCH_UP.GetKey())
                        alt += 4 * scale;
                    else if (!GameSettings.AXIS_PITCH.IsNeutral())
                        alt += 4 * scale * GameSettings.AXIS_PITCH.GetAxis();

                    if (alt < 0)
                        alt = 0;
                    GetController(PIDList.Altitude).SetPoint = alt;
                    gui.UpdateAltitudeField();
                }
            }
        }

        public PAPreset ActivePAPreset
        {
            get { return activePAPreset; }
            set { activePAPreset = value; }
        }

        public PAPreset DefaultPAPreset
        {
            get { return defaultPAPreset; }
        }

        public FlightData FlightData
        {
            get { return flightData; }
        }

        public PID_Controller[] Controllers
        {
            get { return controllers; }
        }

        public bool IsPaused
        {
            get { return isPaused || SASCheck(); }
        }

        public bool IsHdgActive
        {
            get { return isHdgActive; }
        }

        public bool IsWingLvlActive
        {
            get { return isWingLvlActive; }
        }

        public bool IsVertActive
        {
            get { return isVertActive; }
        }

        public bool IsAltitudeHoldActive
        {
            get { return isAltitudeHoldActive; }
        }

        public bool IsThrottleActive
        {
            get { return isThrottleActive; }
        }
    }
}
