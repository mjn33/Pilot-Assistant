using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.UI
{
    using Presets;
    using Utility;

    public static class SASPresetWindow
    {
        private static string newPresetName = "";
        private static Rect windowRect = new Rect(550, 50, 50, 50);

        private const int WINDOW_ID = 78934857;
        private const string TEXT_FIELD_GROUP = "SASPresetWindow";

        public static void Draw(bool show)
        {
            if (show)
            {
                GUI.skin = HighLogic.Skin;
                windowRect = GUILayout.Window(WINDOW_ID, windowRect, DrawPresetWindow, "Presets", GUILayout.Width(200), GUILayout.Height(0));
            }
            else
                GeneralUI.ClearLocks(TEXT_FIELD_GROUP);
        }

        public static void Reposition(float x, float y)
        {
            windowRect.x = x;
            windowRect.y = y;
        }

        private static void DrawPresetWindow(int id)
        {
            // Start a text field group.
            GeneralUI.StartTextFieldGroup(TEXT_FIELD_GROUP);
            if (SurfSAS.Instance.IsSSASMode())
                DrawSurfPreset();
            else
                DrawStockPreset();
        }

        private static void DrawSurfPreset()
        {
            if (PresetManager.Instance.GetActiveSASPreset() != null)
            {
                SASPreset p = PresetManager.Instance.GetActiveSASPreset();
                GUILayout.Label(string.Format("Active Preset: {0}", p.GetName()), GeneralUI.BoldLabelStyle);
                if (p != PresetManager.Instance.GetDefaultSASTuning())
                {
                    if (GUILayout.Button("Update Preset", GeneralUI.ButtonStyle))
                    {
                        SurfSAS.Instance.UpdatePreset();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
            newPresetName = GUILayout.TextField(newPresetName);
            if (GUILayout.Button("+", GeneralUI.ButtonStyle, GUILayout.Width(25)))
            {
                SurfSAS.Instance.RegisterNewPreset(newPresetName);
                newPresetName = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.GUISectionStyle);
            GUILayout.Label("Available presets: ", GeneralUI.BoldLabelStyle);

            if (GUILayout.Button("Default", GeneralUI.ButtonStyle))
            {
                SurfSAS.Instance.LoadPreset(PresetManager.Instance.GetDefaultSASTuning());
            }

            List<SASPreset> allPresets = PresetManager.Instance.GetAllSASPresets();
            foreach (SASPreset p in allPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.GetName(), GeneralUI.ButtonStyle))
                {
                    SurfSAS.Instance.LoadPreset(p);
                }
                if (GUILayout.Button("x", GeneralUI.ButtonStyle, GUILayout.Width(25)))
                {
                    PresetManager.Instance.RemovePreset(p);
                }
                GUILayout.EndHorizontal();
            }
            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFieldGroup(TEXT_FIELD_GROUP, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);
            
            GUILayout.EndVertical();
        }

        private static void DrawStockPreset()
        {
            if (PresetManager.Instance.GetActiveStockSASPreset() != null)
            {
                SASPreset p = PresetManager.Instance.GetActiveStockSASPreset();
                GUILayout.Label(string.Format("Active Preset: {0}", p.GetName()), GeneralUI.BoldLabelStyle);
                if (p != PresetManager.Instance.GetDefaultStockSASTuning())
                {
                    if (GUILayout.Button("Update Preset", GeneralUI.ButtonStyle))
                    {
                        SurfSAS.Instance.UpdateStockPreset();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GeneralUI.TextFieldNext(TEXT_FIELD_GROUP);
            newPresetName = GUILayout.TextField(newPresetName);
            if (GUILayout.Button("+", GeneralUI.ButtonStyle, GUILayout.Width(25)))
            {
                SurfSAS.Instance.RegisterNewStockPreset(newPresetName);
                newPresetName = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GeneralUI.GUISectionStyle);
            GUILayout.Label("Available presets: ", GeneralUI.BoldLabelStyle);

            if (GUILayout.Button("Default", GeneralUI.ButtonStyle))
            {
                SurfSAS.Instance.LoadStockPreset(PresetManager.Instance.GetDefaultStockSASTuning());
            }

            List<SASPreset> allStockPresets = PresetManager.Instance.GetAllStockSASPresets();
            foreach (SASPreset p in allStockPresets)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(p.GetName(), GeneralUI.ButtonStyle))
                {
                    SurfSAS.Instance.LoadStockPreset(p);
                }
                if (GUILayout.Button("x", GeneralUI.ButtonStyle, GUILayout.Width(25)))
                {
                    PresetManager.Instance.RemovePreset(p);
                }
                GUILayout.EndHorizontal();
            }
            // Autolock vessel controls on focus.
            GeneralUI.AutolockTextFieldGroup(TEXT_FIELD_GROUP, ControlTypes.ALL_SHIP_CONTROLS | ControlTypes.TIMEWARP);

            GUILayout.EndVertical();
        }
    }
}
