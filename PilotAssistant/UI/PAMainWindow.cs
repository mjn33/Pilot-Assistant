﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.UI
{
    using Utility;

    static class PAMainWindow
    {
        private static Rect windowRect = new Rect(10, 50, 10, 10);

        private static bool showPresets = false;

        private static bool[] pidDisplay = { false, false, false, false, false, false, false };

        private static bool showPIDLimits = false;
        private static bool showControlSurfaces = false;

        private static double targetVert = 0.0;
        private static double targetAlt = 0.0;
        private static double targetHeading = 0.0;

        private const int WINDOW_ID = 34244;
        private const string TEXT_FIELD_GROUP = "PAMainWindow";

        public static void Draw(bool show)
        {
            if (show)
            {
                GUI.skin = HighLogic.Skin;
                windowRect = GUILayout.Window(WINDOW_ID, windowRect, DrawWindow, "Pilot Assistant", GUILayout.Width(0), GUILayout.Height(0));
                
                PAPresetWindow.Reposition(windowRect.x + windowRect.width, windowRect.y);
                PAPresetWindow.Draw(showPresets);
            }
            else
            {
                GeneralUI.ClearLocks(TEXT_FIELD_GROUP);
                PAPresetWindow.Draw(false);
            }
        }

        public static void SetTargetHeading(double heading)
        {
            targetHeading = heading;
        }

        public static double GetTargetHeading()
        {
            return targetHeading;
        }

        public static void SetTargetVerticalSpeed(double speed)
        {
            targetVert = speed;
        }

        public static double GetTargetVerticalSpeed()
        {
            return targetVert;
        }

        public static void SetTargetAltitude(double altitude)
        {
            targetAlt = altitude;
        }

        public static double GetTargetAltitude()
        {
            return targetAlt;
        }

        private static void DrawHeadingControls()
        {
            bool isHdgActive = PilotAssistant.Instance.IsHdgActive();
            bool isWingLvlActive = PilotAssistant.Instance.IsWingLvlActive();
            FlightData flightData = PilotAssistant.Instance.GetFlightData();
            
            // Heading
            GUILayout.BeginVertical(GeneralUI.GUISectionStyle, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isHdgActive, isHdgActive ? "On" : "Off", GeneralUI.ToggleButtonStyle, GUILayout.ExpandWidth(false)) != isHdgActive)
            {
                PilotAssistant.Instance.ToggleHdg();
            }
            GUILayout.Label("Roll and Yaw Control", GeneralUI.BoldLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode:");
            bool tmpToggle1 = GUILayout.Toggle(!isWingLvlActive, "Hdg control", GeneralUI.ToggleButtonStyle);
            bool tmpToggle2 = GUILayout.Toggle(isWingLvlActive, "Wing lvl", GeneralUI.ToggleButtonStyle);
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
                if (GUILayout.Button("Set", GeneralUI.ButtonStyle, GUILayout.ExpandWidth(false)))
                {
                    ScreenMessages.PostScreenMessage("Target Heading updated");
                    PilotAssistant.Instance.SetHdgActive();
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

        private static void DrawVerticalControls()
        {
            bool isVertActive = PilotAssistant.Instance.IsVertActive();
            bool isAltitudeHoldActive = PilotAssistant.Instance.IsAltitudeHoldActive();
            FlightData flightData = PilotAssistant.Instance.GetFlightData();
            
            // Vertical speed
            GUILayout.BeginVertical(GeneralUI.GUISectionStyle, GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isVertActive, isVertActive ? "On" : "Off", GeneralUI.ToggleButtonStyle, GUILayout.ExpandWidth(false)) != isVertActive)
            {
                PilotAssistant.Instance.ToggleVert();
            }
            GUILayout.Label("Vertical Control", GeneralUI.BoldLabelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode:");
            bool tmpToggle1 = GUILayout.Toggle(isAltitudeHoldActive, "Altitude", GeneralUI.ToggleButtonStyle);
            bool tmpToggle2 = GUILayout.Toggle(!isAltitudeHoldActive, "Vertical Speed", GeneralUI.ToggleButtonStyle);
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
                if (GUILayout.Button("Set", GeneralUI.ButtonStyle, GUILayout.ExpandWidth(false)))
                {
                    ScreenMessages.PostScreenMessage("Target Altitude updated");
                    PilotAssistant.Instance.SetAltitudeHoldActive();
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
                if (GUILayout.Button("Set", GeneralUI.ButtonStyle, GUILayout.ExpandWidth(false)))
                {
                    ScreenMessages.PostScreenMessage("Target Speed updated");
                    PilotAssistant.Instance.SetVertSpeedActive();
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

        private static void DrawWindow(int id)
        {
            // Start a text field group.
            GeneralUI.StartTextFieldGroup(TEXT_FIELD_GROUP);

            GUILayout.BeginVertical(GUILayout.Height(0), GUILayout.Width(0), GUILayout.ExpandHeight(true));
            if (PilotAssistant.Instance.IsPaused() && (PilotAssistant.Instance.IsHdgActive() || PilotAssistant.Instance.IsVertActive()))
            {
                GUILayout.Label("CONTROL PAUSED", GeneralUI.LabelAlertStyle, GUILayout.ExpandWidth(true));
            }
            GUILayout.BeginHorizontal();
            showPresets = GUILayout.Toggle(showPresets, "Presets", GeneralUI.ToggleButtonStyle, GUILayout.ExpandWidth(true));
            showPIDLimits = GUILayout.Toggle(showPIDLimits, "PID Limits", GeneralUI.ToggleButtonStyle, GUILayout.ExpandWidth(true));
            showControlSurfaces = GUILayout.Toggle(showControlSurfaces, "Ctrl Surfaces", GeneralUI.ToggleButtonStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            DrawHeadingControls();

            DrawVerticalControls();

            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFieldGroup(TEXT_FIELD_GROUP, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);
            
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private static void DrawPIDValues(
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
            PID.PID_Controller controller = PilotAssistant.Instance.GetController(controllerID); // controllers[(int)controllerID];
            string buttonText = string.Format("{0}: {1}{2}",
                                              inputName,
                                              inputValue.ToString("F" + displayPrecision),
                                              inputUnits);
            if (GUILayout.Button(buttonText, GeneralUI.ButtonStyle, GUILayout.ExpandWidth(true)))
                pidDisplay[(int)controllerID] = !pidDisplay[(int)controllerID];


            if (pidDisplay[(int)controllerID])
            {
                if (showTarget)
                    GUILayout.Label(string.Format("Target: ", inputName) + controller.SetPoint.ToString("F" + displayPrecision) + inputUnits);
                
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                
                controller.PGain = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kp:", controller.PGain, "F3", 45);
                controller.IGain = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Ki:", controller.IGain, "F3", 45);
                controller.DGain = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kd:", controller.DGain, "F3", 45);
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
