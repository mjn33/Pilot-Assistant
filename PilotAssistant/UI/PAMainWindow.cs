using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.UI
{
    using Presets;
    using Utility;

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class PAMainWindow : MonoBehaviour
    {
        // Singleton pattern, as opposed to using semi-static classes
        private static PAMainWindow instance;
        public static PAMainWindow Instance
        {
            get { return instance; }
        }

        private Rect windowRect = new Rect(10, 50, 10, 10);
        private Rect presetWindowRect = new Rect(0, 0, 200, 10);

        private bool isVisible = false;
        private bool showPresets = false;

        private bool[] pidDisplay;

        private bool showPIDLimits = false;
        private bool showControlSurfaces = false;

        private double targetVert = 0.0;
        private double targetAlt = 0.0;
        private double targetHeading = 0.0;
        private double targetSrfSpeed = 0.0;
        private string newPresetName = "";

        private const int WINDOW_ID = 34244;
        private const int PRESET_WINDOW_ID = 34245;

        private void Awake()
        {
            instance = this;
            pidDisplay = new bool[Enum.GetNames(typeof(PIDList)).Length];
        }

        private void Start()
        {
            RenderingManager.AddToPostDrawQueue(5, DrawGUI);
        }

        private void OnDestroy()
        {
            RenderingManager.RemoveFromPostDrawQueue(5, DrawGUI);
        }

        private void DrawGUI()
        {
            GUI.skin = GeneralUI.Skin;
            if (isVisible)
            {
                windowRect = GUILayout.Window(WINDOW_ID, windowRect, DrawMainWindow, "Pilot Assistant", GUILayout.Width(0),
                                              GUILayout.Height(0));

                if (showPresets)
                {
                    presetWindowRect.x = windowRect.x + windowRect.width;
                    presetWindowRect.y = windowRect.y;
                    presetWindowRect = GUILayout.Window(PRESET_WINDOW_ID, presetWindowRect, DrawPresetWindow, "Presets",
                                                        GUILayout.Width(200), GUILayout.Height(0));
                }
                else
                    GeneralUI.ClearLocks(PRESET_WINDOW_ID);
            }
            else
            {
                GeneralUI.ClearLocks(WINDOW_ID);
                GeneralUI.ClearLocks(PRESET_WINDOW_ID);
            }
        }

        public void UpdateHeadingField()
        {
            targetHeading = PilotAssistant.Instance.GetController(PIDList.HdgBank).SetPoint;
        }

        public void UpdateVertSpeedField()
        {
            targetVert = PilotAssistant.Instance.GetController(PIDList.VertSpeed).SetPoint;
        }

        public void UpdateAltitudeField()
        {
            targetAlt = PilotAssistant.Instance.GetController(PIDList.Altitude).SetPoint;
        }

        public void UpdateSrfSpeedField()
        {
            targetSrfSpeed = PilotAssistant.Instance.GetController(PIDList.Throttle).SetPoint;
        }

        private void DrawMainWindow(int windowId)
        {
            GUILayout.BeginVertical(GUILayout.Height(0), GUILayout.Width(0), GUILayout.ExpandHeight(true));
            if (PilotAssistant.Instance.IsPaused && (PilotAssistant.Instance.IsHdgActive || PilotAssistant.Instance.IsVertActive))
            {
                GUILayout.Label("CONTROL PAUSED", GeneralUI.Style(UIStyle.AlertLabel), GUILayout.ExpandWidth(true));
            }
            GUILayout.BeginHorizontal();
            showPresets         = GUILayout.Toggle(showPresets, "Presets",
                                                   GeneralUI.Style(UIStyle.ToggleButton), GUILayout.ExpandWidth(true));
            showPIDLimits       = GUILayout.Toggle(showPIDLimits, "PID Limits",
                                                   GeneralUI.Style(UIStyle.ToggleButton), GUILayout.ExpandWidth(true));
            showControlSurfaces = GUILayout.Toggle(showControlSurfaces, "Ctrl Surfaces",
                                                   GeneralUI.Style(UIStyle.ToggleButton), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            DrawHeadingControls(windowId);

            DrawVerticalControls(windowId);

            DrawThrottleControls(windowId);

            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFields(windowId, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawHeadingControls(int windowId)
        {
            bool isHdgActive = PilotAssistant.Instance.IsHdgActive;
            bool isWingLvlActive = PilotAssistant.Instance.IsWingLvlActive;
            FlightData flightData = PilotAssistant.Instance.FlightData;

            // Heading
            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection),
                                    GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isHdgActive, isHdgActive ? "On" : "Off", GeneralUI.Style(UIStyle.ToggleButton),
                                 GUILayout.ExpandWidth(false)) != isHdgActive)
            {
                PilotAssistant.Instance.ToggleHdg();
            }
            GUILayout.Label("Roll and Yaw Control", GeneralUI.Style(UIStyle.BoldLabel), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode:");
            bool tmpToggle1 = GUILayout.Toggle(!isWingLvlActive, "Hdg control", GeneralUI.Style(UIStyle.ToggleButton));
            bool tmpToggle2 = GUILayout.Toggle(isWingLvlActive, "Wing lvl", GeneralUI.Style(UIStyle.ToggleButton));
            // tmpToggle1 and tmpToggle2 are true when the user clicks the non-active mode, i.e. the mode changes. 
            if (tmpToggle1 && tmpToggle2)
                PilotAssistant.Instance.ToggleWingLvl();

            GUILayout.EndHorizontal();
            if (!isWingLvlActive)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target Hdg: ");
                GeneralUI.TextFieldNext(windowId);
                string targetHeadingText = GUILayout.TextField(targetHeading.ToString("F2"), GUILayout.Width(60));
                try
                {
                    targetHeading = Functions.Clamp(double.Parse(targetHeadingText), 0, 360);
                }
                catch {}
                if (GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
                {
                    ScreenMessages.PostScreenMessage("Target Heading updated");
                    PilotAssistant.Instance.SetHdgActive(targetHeading);
                }
                GUILayout.EndHorizontal();
            }
            if (!isWingLvlActive)
            {
                DrawPIDValues(windowId, PIDList.HdgBank, "Heading", "\u00B0", flightData.Heading, 2, "Bank", "\u00B0", false, true, false);
                DrawPIDValues(windowId, PIDList.HdgYaw, "Bank => Yaw", "\u00B0", flightData.Yaw, 2, "Yaw", "\u00B0", true, false, false);
            }
            if (showControlSurfaces)
            {
                DrawPIDValues(windowId, PIDList.Aileron, "Bank", "\u00B0", flightData.Roll, 3, "Deflect", "\u00B0", false, true, false);
                DrawPIDValues(windowId, PIDList.Rudder, "Yaw", "\u00B0", flightData.Yaw, 3, "Deflect", "\u00B0", false, true, false);
            }
            GUILayout.EndVertical();
        }

        private void DrawVerticalControls(int windowId)
        {
            bool isVertActive = PilotAssistant.Instance.IsVertActive;
            bool isAltitudeHoldActive = PilotAssistant.Instance.IsAltitudeHoldActive;
            FlightData flightData = PilotAssistant.Instance.FlightData;

            // Vertical speed
            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isVertActive, isVertActive ? "On" : "Off", GeneralUI.Style(UIStyle.ToggleButton),
                                 GUILayout.ExpandWidth(false)) != isVertActive)
            {
                PilotAssistant.Instance.ToggleVert();
            }
            GUILayout.Label("Vertical Control", GeneralUI.Style(UIStyle.BoldLabel), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode:");
            bool tmpToggle1 = GUILayout.Toggle(isAltitudeHoldActive, "Altitude",
                                               GeneralUI.Style(UIStyle.ToggleButton));
            bool tmpToggle2 = GUILayout.Toggle(!isAltitudeHoldActive, "Vertical Speed",
                                               GeneralUI.Style(UIStyle.ToggleButton));
            // tmpToggle1 and tmpToggle2 are true when the user clicks the non-active mode, i.e. the mode changes.
            if (tmpToggle1 && tmpToggle2)
                PilotAssistant.Instance.ToggleAltitudeHold();

            GUILayout.EndHorizontal();

            if (isAltitudeHoldActive)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target Altitude: ");
                GeneralUI.TextFieldNext(windowId);
                string targetAltText = GUILayout.TextField(targetAlt.ToString("F1"), GUILayout.Width(60));
                try
                {
                    targetAlt = double.Parse(targetAltText);
                }
                catch {}
                if (GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
                {
                    ScreenMessages.PostScreenMessage("Target Altitude updated");
                    PilotAssistant.Instance.SetAltitudeHoldActive(targetAlt);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Target Speed: ");
                GeneralUI.TextFieldNext(windowId);
                string targetVertText = GUILayout.TextField(targetVert.ToString("F3"), GUILayout.Width(60));
                try
                {
                    targetVert = double.Parse(targetVertText);
                }
                catch {}
                if (GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
                {
                    ScreenMessages.PostScreenMessage("Target Speed updated");
                    PilotAssistant.Instance.SetVertSpeedActive(targetVert);
                }
                GUILayout.EndHorizontal();
            }
            if (isAltitudeHoldActive)
                DrawPIDValues(windowId, PIDList.Altitude, "Altitude", "m", flightData.Vessel.altitude, 2, "Speed ", "m/s", true, true, false);
            DrawPIDValues(windowId, PIDList.VertSpeed, "Vertical Speed", "m/s", flightData.Vessel.verticalSpeed, 2, "AoA", "\u00B0", true);
            if (showControlSurfaces)
                DrawPIDValues(windowId, PIDList.Elevator, "Angle of Attack", "\u00B0", flightData.AoA, 3, "Deflect", "\u00B0", true, true, false);
            GUILayout.EndVertical();
        }

        private void DrawThrottleControls(int windowId)
        {
            bool isThrottleActive = PilotAssistant.Instance.IsThrottleActive;
            FlightData flightData = PilotAssistant.Instance.FlightData;

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection),
                                    GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isThrottleActive, isThrottleActive ? "On" : "Off", GeneralUI.Style(UIStyle.ToggleButton),
                                 GUILayout.ExpandWidth(false)) != isThrottleActive)
            {
                PilotAssistant.Instance.ToggleThrottleControl();
            }
            GUILayout.Label("Throttle Control", GeneralUI.Style(UIStyle.BoldLabel), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Speed: ");
            GeneralUI.TextFieldNext(windowId);
            string targetSrfSpeedText = GUILayout.TextField(targetSrfSpeed.ToString("F1"), GUILayout.Width(60));
            try
            {
                targetSrfSpeed = double.Parse(targetSrfSpeedText);
            }
            catch {}
            if (GUILayout.Button("Set", GUILayout.ExpandWidth(false)))
            {
                ScreenMessages.PostScreenMessage("Target surface speed updated");
                PilotAssistant.Instance.SetThrottleActive(targetSrfSpeed);
            }
            GUILayout.EndHorizontal();
            if (isThrottleActive)
                DrawPIDValues(windowId, PIDList.Throttle, "Speed", "m/s", flightData.Vessel.srfSpeed, 2, "Throttle", "", true);
            GUILayout.EndVertical();
        }

        private void DrawPresetWindow(int windowId)
        {
            if (PilotAssistant.Instance.ActivePAPreset != null)
            {
                PAPreset p = PilotAssistant.Instance.ActivePAPreset;
                GUILayout.Label(string.Format("Active Preset: {0}", p.Name), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != PilotAssistant.Instance.DefaultPAPreset)
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        p.Update(PilotAssistant.Instance.Controllers);
                        PresetManager.Instance.SavePresetsToFile();
                        GeneralUI.PostMessage("Preset \"" + p.Name + "\" updated");
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GeneralUI.TextFieldNext(windowId);
            newPresetName = GUILayout.TextField(newPresetName);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                PAPreset p = null;
                // Disallow these names to reduce confusion
                if (newPresetName.ToLower() != "default" &&
                    newPresetName.ToLower() != "stock")
                    p = PresetManager.Instance.RegisterPAPreset(newPresetName, PilotAssistant.Instance.Controllers);
                else
                    GeneralUI.PostMessage("The preset name \"" + newPresetName + "\" is not allowed");
                if (p != null)
                {
                    PilotAssistant.Instance.ActivePAPreset = p;
                    newPresetName = "";
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" added");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Default"))
            {
                PAPreset p = PilotAssistant.Instance.DefaultPAPreset;
                PilotAssistant.Instance.ActivePAPreset = p;
                p.LoadPreset(PilotAssistant.Instance.Controllers);
                GeneralUI.PostMessage("Default Pilot Assistant preset loaded");
            }

            List<PAPreset> allPresets = PresetManager.Instance.GetAllPAPresets();
            foreach (PAPreset p in allPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.Name))
                {
                    PilotAssistant.Instance.ActivePAPreset = p;
                    p.LoadPreset(PilotAssistant.Instance.Controllers);
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" loaded");
                }
                if (GUILayout.Button("x", GUILayout.Width(25)))
                {
                    PresetManager.Instance.RemovePreset(p);
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" deleted");
                }
                GUILayout.EndHorizontal();
            }

            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFields(windowId, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);

            GUILayout.EndVertical();
        }

        private void DrawPIDValues(
            int windowId,
            PIDList controllerID,
            string inputName,
            string inputUnits,
            double inputValue,
            int displayPrecision,
            string outputName,
            string outputUnits,
            bool invertOutput = false,
            bool showTarget = true,
            bool doublesided = true)
        {
            PID.PID_Controller controller = PilotAssistant.Instance.GetController(controllerID);
            string buttonText = string.Format("{0}: {1}{2}",
                                              inputName,
                                              inputValue.ToString("F" + displayPrecision),
                                              inputUnits);
            if (GUILayout.Button(buttonText, GUILayout.ExpandWidth(true)))
                pidDisplay[(int)controllerID] = !pidDisplay[(int)controllerID];


            if (pidDisplay[(int)controllerID])
            {
                if (showTarget)
                    GUILayout.Label(string.Format("Target: ", inputName) + controller.SetPoint.ToString("F" + displayPrecision) + inputUnits);

                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();

                controller.Tuning.PGain = GeneralUI.LabPlusNumBox(windowId, "Kp:", controller.Tuning.PGain, "F3", 45);
                controller.Tuning.IGain = GeneralUI.LabPlusNumBox(windowId, "Ki:", controller.Tuning.IGain, "F3", 45);
                controller.Tuning.DGain = GeneralUI.LabPlusNumBox(windowId, "Kd:", controller.Tuning.DGain, "F3", 45);
                controller.Tuning.Scale = GeneralUI.LabPlusNumBox(windowId, "Scalar:", controller.Tuning.Scale, "F3", 45);

                if (showPIDLimits)
                {
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    string tmpMinText = string.Format("Max {0}{1}:", outputName, outputUnits);
                    string tmpMaxText = string.Format("Min {0}{1}:", outputName, outputUnits);

                    if (!invertOutput)
                    {
                        controller.Tuning.OutMax = GeneralUI.LabPlusNumBox(windowId, tmpMaxText, controller.Tuning.OutMax, "F3");
                        if (doublesided)
                            controller.Tuning.OutMin = GeneralUI.LabPlusNumBox(windowId, tmpMinText, controller.Tuning.OutMin, "F3");
                        else
                            controller.Tuning.OutMin = -controller.Tuning.OutMax;
                        controller.Tuning.ClampLower = GeneralUI.LabPlusNumBox(windowId, "I Clamp Lower:", controller.Tuning.ClampLower, "F3");
                        controller.Tuning.ClampUpper = GeneralUI.LabPlusNumBox(windowId, "I Clamp Upper:", controller.Tuning.ClampUpper, "F3");
                    }
                    else
                    { // used when response * -1 is used to get the correct output
                        controller.Tuning.OutMax = -GeneralUI.LabPlusNumBox(windowId, tmpMinText, -controller.Tuning.OutMax, "F3");
                        if (doublesided)
                            controller.Tuning.OutMin = -GeneralUI.LabPlusNumBox(windowId, tmpMaxText, -controller.Tuning.OutMin, "F3");
                        else
                            controller.Tuning.OutMin = -controller.Tuning.OutMax;
                        controller.Tuning.ClampUpper = -GeneralUI.LabPlusNumBox(windowId, "I Clamp Lower:", -controller.Tuning.ClampUpper, "F3");
                        controller.Tuning.ClampLower = -GeneralUI.LabPlusNumBox(windowId, "I Clamp Upper:", -controller.Tuning.ClampLower, "F3");
                    }
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; }
        }
    }
}
