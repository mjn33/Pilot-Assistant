using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.UI
{
    using Presets;
    using Utility;

    public class SASMainWindow
    {
        private SurfSAS surfSAS;

        private Rect windowRect = new Rect(350, 50, 200, 30);
        private Rect presetWindowRect = new Rect(550, 50, 50, 50);

        private bool isVisible = false;
        // Determines if the preset GUI should be shown
        private bool showPresets = false;

        // Array describing the visibility of the various stock SAS PID controller values
        private bool[] stockPidDisplay = new bool[Enum.GetNames(typeof(SASList)).Length];
        // Similar to "stockPidDisplay" but for surface SAS controllers
        private bool[] ssasPidDisplay = new bool[Enum.GetNames(typeof(SASList)).Length];
        private string newPresetName = "";

        private const int WINDOW_ID = 78934856;
        private const int PRESET_WINDOW_ID = 78934857;

        public SASMainWindow(SurfSAS surfSAS)
        {
            this.surfSAS = surfSAS;
        }

        public void OnGUI()
        {
            GUI.skin = GeneralUI.Skin;
            if (surfSAS.IsSSASMode)
            {
                Color oldColor = GUI.backgroundColor;
                if (surfSAS.IsSSASOperational)
                    GUI.backgroundColor = GeneralUI.SSASActiveBGColor;
                else
                    GUI.backgroundColor = GeneralUI.SSASInactiveBGColor;

                if (GUI.Button(new Rect(Screen.width / 2 + 50, Screen.height - 200, 50, 30), "SSAS"))
                {
                    surfSAS.ToggleOperational();
                }
                GUI.backgroundColor = oldColor;
            }

            if (AppLauncherFlight.ShowSAS)
            {
                windowRect = GUILayout.Window(WINDOW_ID, windowRect, DrawSASWindow, "SAS Module", GUILayout.Width(0),
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

        private void DrawSASWindow(int windowId)
        {
            bool isOperational = surfSAS.IsSSASOperational || surfSAS.IsStockSASOperational;
            bool isSSASMode = surfSAS.IsSSASMode;
            GUILayout.BeginHorizontal();
            showPresets = GUILayout.Toggle(showPresets, "Presets", GeneralUI.Style(UIStyle.ToggleButton));
            GUILayout.EndHorizontal();

            // SSAS/SAS
            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isOperational, isOperational ? "On" : "Off", GeneralUI.Style(UIStyle.ToggleButton),
                                 GUILayout.ExpandWidth(false)) != isOperational)
            {
                surfSAS.ToggleOperational();
            }
            GUILayout.Label("SAS", GeneralUI.Style(UIStyle.BoldLabel), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode:");
            bool tmpToggle1 = GUILayout.Toggle(!isSSASMode, "Stock SAS", GeneralUI.Style(UIStyle.ToggleButton));
            bool tmpToggle2 = GUILayout.Toggle(isSSASMode, "SSAS", GeneralUI.Style(UIStyle.ToggleButton));
            // tmpToggle1 and tmpToggle2 are true when the user clicks the non-active mode, i.e. the mode changes.
            if (tmpToggle1 && tmpToggle2)
                surfSAS.ToggleSSASMode();

            GUILayout.EndHorizontal();

            if (isSSASMode)
            {
                double pitch = surfSAS.GetController(SASList.Pitch).SetPoint;
                double roll = surfSAS.GetController(SASList.Roll).SetPoint;
                double hdg = surfSAS.GetController(SASList.Yaw).SetPoint;

                bool tmp1 = surfSAS.IsSSASAxisEnabled(SASList.Pitch);
                bool tmp2 = surfSAS.IsSSASAxisEnabled(SASList.Roll);
                bool tmp3 = surfSAS.IsSSASAxisEnabled(SASList.Yaw);
                surfSAS.GetController(SASList.Pitch).SetPoint
                    = GeneralUI.TogPlusNumBox(windowId, "Pitch:", ref tmp1, pitch, 80, 60, 80, -80);
                surfSAS.GetController(SASList.Roll).SetPoint
                    = GeneralUI.TogPlusNumBox(windowId, "Roll:", ref tmp2, roll, 80, 60, 180, -180);
                surfSAS.GetController(SASList.Yaw).SetPoint
                    = GeneralUI.TogPlusNumBox(windowId, "Heading:", ref tmp3, hdg, 80, 60, 360, 0);
                surfSAS.SetSSASAxisEnabled(SASList.Pitch, tmp1);
                surfSAS.SetSSASAxisEnabled(SASList.Roll, tmp2);
                surfSAS.SetSSASAxisEnabled(SASList.Yaw, tmp3);

                DrawPIDValues(windowId, SASList.Pitch, "Pitch");
                DrawPIDValues(windowId, SASList.Roll, "Roll");
                DrawPIDValues(windowId, SASList.Yaw, "Yaw");
            }
            else
            {
                FlightData flightData = surfSAS.FlightData;
                VesselAutopilot.VesselSAS sas = flightData.Vessel.Autopilot.SAS;

                DrawPIDValues(windowId, sas.pidLockedPitch, "Pitch", SASList.Pitch);
                DrawPIDValues(windowId, sas.pidLockedRoll, "Roll", SASList.Roll);
                DrawPIDValues(windowId, sas.pidLockedYaw, "Yaw", SASList.Yaw);
            }

            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFields(windowId, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawPresetWindow(int windowId)
        {
            if (surfSAS.IsSSASMode)
                DrawSSASPreset(windowId);
            else
                DrawStockPreset(windowId);
        }

        private void DrawSSASPreset(int windowId)
        {
            if (surfSAS.ActiveSSASPreset != null)
            {
                SASPreset p = surfSAS.ActiveSSASPreset;
                GUILayout.Label(string.Format("Active Preset: {0}", p.Name), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != surfSAS.DefaultSSASPreset)
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        p.Update(surfSAS.Controllers);
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
                SASPreset p = null;
                // Disallow these names to reduce confusion
                if (newPresetName.ToLower() != "default" &&
                    newPresetName.ToLower() != "stock")
                    p = PresetManager.Instance.RegisterSSASPreset(newPresetName, surfSAS.Controllers);
                else
                    GeneralUI.PostMessage("The preset name \"" + newPresetName + "\" is not allowed");
                if (p != null)
                {
                    surfSAS.ActiveSSASPreset = p;
                    newPresetName = "";
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" added");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Default"))
            {
                SASPreset p = surfSAS.DefaultSSASPreset;
                surfSAS.ActiveSSASPreset = p;
                p.LoadPreset(surfSAS.Controllers);
                GeneralUI.PostMessage("Default SSAS preset loaded");
            }

            List<SASPreset> allPresets = PresetManager.Instance.GetAllSASPresets();
            foreach (SASPreset p in allPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.Name))
                {
                    surfSAS.ActiveSSASPreset = p;
                    p.LoadPreset(surfSAS.Controllers);
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

        private void DrawStockPreset(int windowId)
        {
            if (surfSAS.ActiveStockPreset != null)
            {
                SASPreset p = surfSAS.ActiveStockPreset;
                GUILayout.Label(string.Format("Active Preset: {0}", p.Name), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != surfSAS.DefaultStockPreset)
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        p.UpdateStock(surfSAS.FlightData.Vessel.Autopilot.SAS);
                        PresetManager.Instance.SavePresetsToFile();
                        GeneralUI.PostMessage("Preset \"" + p.Name + "\" updated");
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GeneralUI.TextFieldNext(windowId);
            newPresetName = GUILayout.TextField(newPresetName);
            if (GUILayout.Button("+",  GUILayout.Width(25)))
            {
                SASPreset p = null;
                // Disallow these names to reduce confusion
                if (newPresetName.ToLower() != "default" &&
                    newPresetName.ToLower() != "stock")
                    p = PresetManager.Instance.RegisterStockSASPreset(
                        newPresetName, surfSAS.FlightData.Vessel.Autopilot.SAS);
                else
                    GeneralUI.PostMessage("The preset name \"" + newPresetName + "\" is not allowed");
                if (p != null)
                {
                    surfSAS.ActiveStockPreset = p;
                    newPresetName = "";
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" added");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Stock"))
            {
                SASPreset p = surfSAS.DefaultStockPreset;
                surfSAS.ActiveStockPreset = p;
                p.LoadStockPreset(surfSAS.FlightData.Vessel.Autopilot.SAS);
                GeneralUI.PostMessage("Default stock preset loaded");
            }

            List<SASPreset> allStockPresets = PresetManager.Instance.GetAllStockSASPresets();
            foreach (SASPreset p in allStockPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.Name))
                {
                    surfSAS.ActiveStockPreset = p;
                    p.LoadStockPreset(surfSAS.FlightData.Vessel.Autopilot.SAS);
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

        private void DrawPIDValues(int windowId, SASList controllerID, string inputName)
        {
            PID.PID_Controller controller = surfSAS.GetController(controllerID);
            if (GUILayout.Button(inputName, GUILayout.ExpandWidth(true)))
                ssasPidDisplay[(int)controllerID] = !ssasPidDisplay[(int)controllerID];

            if (ssasPidDisplay[(int)controllerID])
            {
                controller.Tuning.PGain = GeneralUI.LabPlusNumBox(windowId, "Kp:", controller.Tuning.PGain, "F3", 45);
                controller.Tuning.IGain = GeneralUI.LabPlusNumBox(windowId, "Ki:", controller.Tuning.IGain, "F3", 45);
                controller.Tuning.DGain = GeneralUI.LabPlusNumBox(windowId, "Kd:", controller.Tuning.DGain, "F3", 45);
                controller.Tuning.Scale = GeneralUI.LabPlusNumBox(windowId, "Scalar:", controller.Tuning.Scale, "F3", 45);
            }
        }

        private void DrawPIDValues(int windowId, PIDclamp controller, string inputName, SASList id)
        {
            if (GUILayout.Button(inputName, GUILayout.ExpandWidth(true)))
            {
                stockPidDisplay[(int)id] = !stockPidDisplay[(int)id];
            }

            if (stockPidDisplay[(int)id])
            {
                controller.kp = GeneralUI.LabPlusNumBox(windowId, "Kp:", controller.kp, "F3", 45);
                controller.ki = GeneralUI.LabPlusNumBox(windowId, "Ki:", controller.ki, "F3", 45);
                controller.kd = GeneralUI.LabPlusNumBox(windowId, "Kd:", controller.kd, "F3", 45);
                controller.clamp = GeneralUI.LabPlusNumBox(windowId, "Scalar:", controller.clamp, "F3", 45);
            }
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set { isVisible = value; }
        }
    }
}
