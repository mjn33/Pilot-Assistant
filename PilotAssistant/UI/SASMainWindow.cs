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
        private const string TEXT_FIELD_GROUP = "SASMainWindow";

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
            }
            else
            {
                GeneralUI.ClearLocks(TEXT_FIELD_GROUP);
            }
        }

        private void DrawSASWindow(int id)
        {
            // Start a text field group.
            GeneralUI.StartTextFieldGroup(TEXT_FIELD_GROUP);

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
                    = GeneralUI.TogPlusNumBox(TEXT_FIELD_GROUP, "Pitch:", ref tmp1, pitch, 80, 60, 80, -80);
                SurfSAS.Instance.GetController(SASList.Roll).SetPoint
                    = GeneralUI.TogPlusNumBox(TEXT_FIELD_GROUP, "Roll:", ref tmp2, roll, 80, 60, 180, -180);
                SurfSAS.Instance.GetController(SASList.Yaw).SetPoint
                    = GeneralUI.TogPlusNumBox(TEXT_FIELD_GROUP, "Heading:", ref tmp3, hdg, 80, 60, 360, 0);
                SurfSAS.Instance.SetSSASAxisEnabled(SASList.Pitch, tmp1);
                SurfSAS.Instance.SetSSASAxisEnabled(SASList.Roll, tmp2);
                SurfSAS.Instance.SetSSASAxisEnabled(SASList.Yaw, tmp3);

                DrawPIDValues(SASList.Pitch, "Pitch");
                DrawPIDValues(SASList.Roll, "Roll");
                DrawPIDValues(SASList.Yaw, "Yaw");
            }
            else
            {
                FlightData flightData = SurfSAS.Instance.GetFlightData();
                VesselAutopilot.VesselSAS sas = flightData.Vessel.Autopilot.SAS;

                DrawPIDValues(sas.pidLockedPitch, "Pitch", SASList.Pitch);
                DrawPIDValues(sas.pidLockedRoll, "Roll", SASList.Roll);
                DrawPIDValues(sas.pidLockedYaw, "Yaw", SASList.Yaw);
            }

            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFieldGroup(TEXT_FIELD_GROUP, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawPresetWindow(int id)
        {
            // Start a text field group.
            GeneralUI.StartTextFieldGroup(TEXT_FIELD_GROUP);
            if (SurfSAS.Instance.IsSSASMode())
                DrawSSASPreset();
            else
                DrawStockPreset();
        }

        private void DrawSSASPreset()
        {
            if (PresetManager.Instance.GetActiveSASPreset() != null)
            {
                SASPreset p = PresetManager.Instance.GetActiveSASPreset();
                GUILayout.Label(string.Format("Active Preset: {0}", p.GetName()), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != PresetManager.Instance.GetDefaultSASTuning())
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        SurfSAS.Instance.UpdatePreset();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
            newPresetName = GUILayout.TextField(newPresetName);
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                SurfSAS.Instance.RegisterNewPreset(newPresetName);
                newPresetName = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Default"))
            {
                SurfSAS.Instance.LoadPreset(PresetManager.Instance.GetDefaultSASTuning());
            }

            List<SASPreset> allPresets = PresetManager.Instance.GetAllSASPresets();
            foreach (SASPreset p in allPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.GetName()))
                {
                    SurfSAS.Instance.LoadPreset(p);
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

        private void DrawStockPreset()
        {
            if (PresetManager.Instance.GetActiveStockSASPreset() != null)
            {
                SASPreset p = PresetManager.Instance.GetActiveStockSASPreset();
                GUILayout.Label(string.Format("Active Preset: {0}", p.GetName()), GeneralUI.Style(UIStyle.BoldLabel));
                if (p != PresetManager.Instance.GetDefaultStockSASTuning())
                {
                    if (GUILayout.Button("Update Preset"))
                    {
                        SurfSAS.Instance.UpdateStockPreset();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
            newPresetName = GUILayout.TextField(newPresetName);
            if (GUILayout.Button("+",  GUILayout.Width(25)))
            {
                SurfSAS.Instance.RegisterNewStockPreset(newPresetName);
                newPresetName = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.Style(UIStyle.GUISection));
            GUILayout.Label("Available presets: ", GeneralUI.Style(UIStyle.BoldLabel));

            if (GUILayout.Button("Default"))
            {
                SurfSAS.Instance.LoadStockPreset(PresetManager.Instance.GetDefaultStockSASTuning());
            }

            List<SASPreset> allStockPresets = PresetManager.Instance.GetAllStockSASPresets();
            foreach (SASPreset p in allStockPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.GetName()))
                {
                    SurfSAS.Instance.LoadStockPreset(p);
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

        private void DrawPIDValues(SASList controllerID, string inputName)
        {
            PID.PID_Controller controller = SurfSAS.Instance.GetController(controllerID);
            if (GUILayout.Button(inputName, GUILayout.ExpandWidth(true)))
                ssasPidDisplay[(int)controllerID] = !ssasPidDisplay[(int)controllerID];

            if (ssasPidDisplay[(int)controllerID])
            {
                controller.PGain = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kp:", controller.PGain, "F3", 45);
                controller.IGain = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Ki:", controller.IGain, "F3", 45);
                controller.DGain = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kd:", controller.DGain, "F3", 45);
                controller.Scalar = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Scalar:", controller.Scalar, "F3", 45);
            }
        }

        private void DrawPIDValues(PIDclamp controller, string inputName, SASList id)
        {
            if (GUILayout.Button(inputName, GUILayout.ExpandWidth(true)))
            {
                stockPidDisplay[(int)id] = !stockPidDisplay[(int)id];
            }

            if (stockPidDisplay[(int)id])
            {
                controller.kp = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kp:", controller.kp, "F3", 45);
                controller.ki = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Ki:", controller.ki, "F3", 45);
                controller.kd = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Kd:", controller.kd, "F3", 45);
                controller.clamp = GeneralUI.LabPlusNumBox(TEXT_FIELD_GROUP, "Scalar:", controller.clamp, "F3", 45);
            }
        }
    }
}
