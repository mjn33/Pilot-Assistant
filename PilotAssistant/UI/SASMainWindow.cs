using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.UI
{
    using Presets;
    using Utility;

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SASMainWindow : MonoBehaviour
    {
        // Singleton pattern, as opposed to using semi-static classes
        private static SASMainWindow instance;
        public static SASMainWindow Instance
        {
            get { return instance; }
        }

        private Rect windowRect = new Rect(350, 50, 200, 30);
        private Rect presetWindowRect = new Rect(550, 50, 50, 50);

        private bool isVisible = false;
        // Determines if the preset GUI should be shown
        private bool showPresets = false;

        // Array describing the visibility of the various stock SAS PID controller values
        private bool[] stockPidDisplay;
        // Similar to "stockPidDisplay" but for surface SAS controllers
        private bool[] ssasPidDisplay;
        private string newPresetName = "";

        private const int WINDOW_ID = 78934856;
        private const int PRESET_WINDOW_ID = 78934857;

        private void Awake()
        {
            instance = this;
            stockPidDisplay = new bool[Enum.GetNames(typeof(SASList)).Length];
            ssasPidDisplay = new bool[Enum.GetNames(typeof(SASList)).Length];
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
            if (SurfSAS.Instance.IsSSASMode())
            {
                Color oldColor = GUI.backgroundColor;
                if (SurfSAS.Instance.IsSSASOperational())
                    GUI.backgroundColor = GeneralUI.SSASActiveBGColor;
                else
                    GUI.backgroundColor = GeneralUI.SSASInactiveBGColor;

                if (GUI.Button(new Rect(Screen.width / 2 + 50, Screen.height - 200, 50, 30), "SSAS"))
                {
                    SurfSAS.Instance.ToggleOperational();
                }
                GUI.backgroundColor = oldColor;
            }

            if (isVisible)
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
            bool isOperational = SurfSAS.Instance.IsSSASOperational() || SurfSAS.Instance.IsStockSASOperational();
            bool isSSASMode = SurfSAS.Instance.IsSSASMode();
            GUILayout.BeginHorizontal();
            showPresets = GUILayout.Toggle(showPresets, "Presets", GeneralUI.Style(UIStyle.ToggleButton));
            GUILayout.EndHorizontal();

            // SSAS/SAS
            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection), GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(isOperational, isOperational ? "On" : "Off", GeneralUI.Style(UIStyle.ToggleButton),
                                 GUILayout.ExpandWidth(false)) != isOperational)
            {
                SurfSAS.Instance.ToggleOperational();
            }
            GUILayout.Label("SAS", GeneralUI.Style(UIStyle.BoldLabel), GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mode:");
            bool tmpToggle1 = GUILayout.Toggle(!isSSASMode, "Stock SAS", GeneralUI.Style(UIStyle.ToggleButton));
            bool tmpToggle2 = GUILayout.Toggle(isSSASMode, "SSAS", GeneralUI.Style(UIStyle.ToggleButton));
            // tmpToggle1 and tmpToggle2 are true when the user clicks the non-active mode, i.e. the mode changes.
            if (tmpToggle1 && tmpToggle2)
                SurfSAS.Instance.ToggleSSASMode();

            GUILayout.EndHorizontal();

            if (isSSASMode)
            {
                double pitch = SurfSAS.Instance.GetController(SASList.Pitch).SetPoint;
                double roll = SurfSAS.Instance.GetController(SASList.Roll).SetPoint;
                double hdg = SurfSAS.Instance.GetController(SASList.Yaw).SetPoint;

                bool tmp1 = SurfSAS.Instance.IsSSASAxisEnabled(SASList.Pitch);
                bool tmp2 = SurfSAS.Instance.IsSSASAxisEnabled(SASList.Roll);
                bool tmp3 = SurfSAS.Instance.IsSSASAxisEnabled(SASList.Yaw);
                SurfSAS.Instance.GetController(SASList.Pitch).SetPoint
                    = GeneralUI.TogPlusNumBox(windowId, "Pitch:", ref tmp1, pitch, 80, 60, 80, -80);
                SurfSAS.Instance.GetController(SASList.Roll).SetPoint
                    = GeneralUI.TogPlusNumBox(windowId, "Roll:", ref tmp2, roll, 80, 60, 180, -180);
                SurfSAS.Instance.GetController(SASList.Yaw).SetPoint
                    = GeneralUI.TogPlusNumBox(windowId, "Heading:", ref tmp3, hdg, 80, 60, 360, 0);
                SurfSAS.Instance.SetSSASAxisEnabled(SASList.Pitch, tmp1);
                SurfSAS.Instance.SetSSASAxisEnabled(SASList.Roll, tmp2);
                SurfSAS.Instance.SetSSASAxisEnabled(SASList.Yaw, tmp3);

                DrawPIDValues(windowId, SASList.Pitch, "Pitch");
                DrawPIDValues(windowId, SASList.Roll, "Roll");
                DrawPIDValues(windowId, SASList.Yaw, "Yaw");
            }
            else
            {
                FlightData flightData = SurfSAS.Instance.GetFlightData();
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
            if (SurfSAS.Instance.IsSSASMode())
                DrawSSASPreset(windowId);
            else
                DrawStockPreset(windowId);
        }

        private void DrawSSASPreset(int windowId)
        {
            if (SurfSAS.Instance.ActiveSSASPreset != null)
            {
                SASPreset p = SurfSAS.Instance.ActiveSSASPreset;
                GUILayout.Label(string.Format("Active Preset: {0}", p.Name), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != SurfSAS.Instance.DefaultSSASPreset)
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        // TODO: Reformat this (too much indentation)
                        // TODO: Print out message?
                        p.Update(SurfSAS.Instance.Controllers);
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
                // TODO: Print out message?
                // TODO: Null check
                SASPreset p = null;
                // Disallow these names to reduce confusion
                if (newPresetName.ToLower() != "default" &&
                    newPresetName.ToLower() != "stock")
                    p = PresetManager.Instance.RegisterSSASPreset(newPresetName, SurfSAS.Instance.Controllers);
                else
                    GeneralUI.PostMessage("The preset name \"" + newPresetName + "\" is not allowed");
                if (p != null)
                {
                    SurfSAS.Instance.ActiveSSASPreset = p;
                    newPresetName = "";
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" added");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Default"))
            {
                // TODO: Print out message?
                SASPreset p = SurfSAS.Instance.DefaultSSASPreset;
                SurfSAS.Instance.ActiveSSASPreset = p;
                p.LoadPreset(SurfSAS.Instance.Controllers);
                GeneralUI.PostMessage("Default SSAS preset loaded");
            }

            List<SASPreset> allPresets = PresetManager.Instance.GetAllSASPresets();
            foreach (SASPreset p in allPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.Name))
                {
                    // TODO: Print out message?
                    SurfSAS.Instance.ActiveSSASPreset = p;
                    p.LoadPreset(SurfSAS.Instance.Controllers);
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" loaded");
                }
                if (GUILayout.Button("x", GUILayout.Width(25)))
                {
                    // TODO: Print out message?
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
            if (SurfSAS.Instance.ActiveStockPreset != null)
            {
                SASPreset p = SurfSAS.Instance.ActiveStockPreset;
                GUILayout.Label(string.Format("Active Preset: {0}", p.Name), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != SurfSAS.Instance.DefaultStockPreset)
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        // TODO: Reformat this (too much indentation)
                        // TODO: Print out message?
                        p.UpdateStock(SurfSAS.Instance.FlightData.Vessel.Autopilot.SAS);
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
                // TODO: Print out message?
                // TODO: Null check
                SASPreset p = null;
                // Disallow these names to reduce confusion
                if (newPresetName.ToLower() != "default" &&
                    newPresetName.ToLower() != "stock")
                    p = PresetManager.Instance.RegisterStockSASPreset(
                        newPresetName, SurfSAS.Instance.FlightData.Vessel.Autopilot.SAS);
                else
                    GeneralUI.PostMessage("The preset name \"" + newPresetName + "\" is not allowed");
                if (p != null)
                {
                    SurfSAS.Instance.ActiveStockPreset = p;
                    newPresetName = "";
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" added");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Stock"))
            {
                // TODO: Print out message?
                SASPreset p = SurfSAS.Instance.DefaultStockPreset;
                SurfSAS.Instance.ActiveStockPreset = p;
                p.LoadStockPreset(SurfSAS.Instance.FlightData.Vessel.Autopilot.SAS);
                GeneralUI.PostMessage("Default stock preset loaded");
            }

            List<SASPreset> allStockPresets = PresetManager.Instance.GetAllStockSASPresets();
            foreach (SASPreset p in allStockPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.Name))
                {
                    // TODO: Print out message?
                    SurfSAS.Instance.ActiveStockPreset = p;
                    p.LoadStockPreset(SurfSAS.Instance.FlightData.Vessel.Autopilot.SAS);
                    GeneralUI.PostMessage("Preset \"" + p.Name + "\" loaded");
                }
                if (GUILayout.Button("x", GUILayout.Width(25)))
                {
                    // TODO: Print out message?
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
            PID.PID_Controller controller = SurfSAS.Instance.GetController(controllerID);
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
    }
}
