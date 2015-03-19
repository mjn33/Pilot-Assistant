﻿using System;
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
        private string newPresetName = "";

        private const int WINDOW_ID = 34244;
        private const int PRESET_WINDOW_ID = 34245;
        private const string TEXT_FIELD_GROUP = "PAMainWindow";

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

        public bool IsVisible()
        {
            return isVisible;
        }

        public void ToggleVisibility()
        {
            isVisible = !isVisible;
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
            }
            else
            {
                GeneralUI.ClearLocks(TEXT_FIELD_GROUP);
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

        private void DrawMainWindow(int id)
        {
            // Start a text field group.
            GeneralUI.StartTextFieldGroup(TEXT_FIELD_GROUP);

            GUILayout.BeginVertical(GUILayout.Height(0), GUILayout.Width(0), GUILayout.ExpandHeight(true));
            if (PilotAssistant.Instance.IsPaused() && (PilotAssistant.Instance.IsHdgActive() || PilotAssistant.Instance.IsVertActive()))
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

            DrawHeadingControls();

            DrawVerticalControls();

            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFieldGroup(TEXT_FIELD_GROUP, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawHeadingControls()
        {
            bool isHdgActive = PilotAssistant.Instance.IsHdgActive();
            bool isWingLvlActive = PilotAssistant.Instance.IsWingLvlActive();
            FlightData flightData = PilotAssistant.Instance.GetFlightData();

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
                GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
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
                DrawPIDValues(PIDList.HdgBank, "Heading", "\u00B0", flightData.Heading, 2, "Bank", "\u00B0", false, true, false);
                DrawPIDValues(PIDList.HdgYaw, "Bank => Yaw", "\u00B0", flightData.Yaw, 2, "Yaw", "\u00B0", true, false, false);
            }
            if (showControlSurfaces)
            {
                DrawPIDValues(PIDList.Aileron, "Bank", "\u00B0", flightData.Roll, 3, "Deflect", "\u00B0", false, true, false);
                DrawPIDValues(PIDList.Rudder, "Yaw", "\u00B0", flightData.Yaw, 3, "Deflect", "\u00B0", false, true, false);
            }
            GUILayout.EndVertical();
        }

        private void DrawVerticalControls()
        {
            bool isVertActive = PilotAssistant.Instance.IsVertActive();
            bool isAltitudeHoldActive = PilotAssistant.Instance.IsAltitudeHoldActive();
            FlightData flightData = PilotAssistant.Instance.GetFlightData();

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
                GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
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
                GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
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
                DrawPIDValues(PIDList.Altitude, "Altitude", "m", flightData.Vessel.altitude, 2, "Speed ", "m/s", true, true, false);
            DrawPIDValues(PIDList.VertSpeed, "Vertical Speed", "m/s", flightData.Vessel.verticalSpeed, 2, "AoA", "\u00B0", true);
            if (showControlSurfaces)
                DrawPIDValues(PIDList.Elevator, "Angle of Attack", "\u00B0", flightData.AoA, 3, "Deflect", "\u00B0", true, true, false);
            GUILayout.EndVertical();
        }

        private void DrawPresetWindow(int id)
        {
            // Start a text field group.
            GeneralUI.StartTextFieldGroup(TEXT_FIELD_GROUP);

            if (PresetManager.Instance.GetActivePAPreset() != null)
            {
                PAPreset p = PresetManager.Instance.GetActivePAPreset();
                GUILayout.Label(string.Format("Active Preset: {0}", p.GetName()), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != PresetManager.Instance.GetDefaultPATuning())
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        PilotAssistant.Instance.UpdatePreset();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
            newPresetName = GUILayout.TextField(newPresetName);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                PilotAssistant.Instance.RegisterNewPreset(newPresetName);
                newPresetName = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Default"))
            {
                PilotAssistant.Instance.LoadPreset(PresetManager.Instance.GetDefaultPATuning());
            }

            List<PAPreset> allPresets = PresetManager.Instance.GetAllPAPresets();
            foreach (PAPreset p in allPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.GetName()))
                {
                    PilotAssistant.Instance.LoadPreset(p);
                }
                if (GUILayout.Button("x", GUILayout.Width(25)))
                {
                    PresetManager.Instance.RemovePreset(p);
                }
                GUILayout.EndHorizontal();
            }

            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFieldGroup(TEXT_FIELD_GROUP, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);

            GUILayout.EndVertical();
        }

        private void DrawPIDValues(
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

                controller.PGain  = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kp:", controller.PGain, "F3", 45);
                controller.IGain  = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Ki:", controller.IGain, "F3", 45);
                controller.DGain  = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kd:", controller.DGain, "F3", 45);
                controller.Scalar = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Scalar:", controller.Scalar, "F3", 45);

                if (showPIDLimits)
                {
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical();
                    string tmpMinText = string.Format("Max {0}{1}:", outputName, outputUnits);
                    string tmpMaxText = string.Format("Min {0}{1}:", outputName, outputUnits);

                    if (!invertOutput)
                    {
                        controller.OutMax = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, tmpMaxText, controller.OutMax, "F3");
                        if (doublesided)
                            controller.OutMin = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, tmpMinText, controller.OutMin, "F3");
                        else
                            controller.OutMin = -controller.OutMax;
                        controller.ClampLower = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "I Clamp Lower:", controller.ClampLower, "F3");
                        controller.ClampUpper = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "I Clamp Upper:", controller.ClampUpper, "F3");
                    }
                    else
                    { // used when response * -1 is used to get the correct output
                        controller.OutMax = -1 * GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, tmpMinText, -controller.OutMax, "F3");
                        if (doublesided)
                            controller.OutMin = -1 * GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, tmpMaxText, -controller.OutMin, "F3");
                        else
                            controller.OutMin = -controller.OutMax;
                        controller.ClampUpper = -1 * GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "I Clamp Lower:", -controller.ClampUpper, "F3");
                        controller.ClampLower = -1 * GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "I Clamp Upper:", -controller.ClampLower, "F3");
                    }
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }
    }
}
