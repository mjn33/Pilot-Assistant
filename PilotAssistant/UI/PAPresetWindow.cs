using System;
using System.Collections.Generic;
using UnityEngine;
using PilotAssistant.Presets;

namespace PilotAssistant.UI
{
    using Utility;

    static class PAPresetWindow
    {
        private static string newPresetName = "";
        private static Rect windowRect = new Rect(0, 0, 200, 10);

        private const int WINDOW_ID = 34245;
        private const string TEXT_FIELD_GROUP = "PAPresetWindow";

        public static void Draw(bool show)
        {
            if (show)
            {
                GUI.skin = GeneralUI.Skin;
                windowRect = GUILayout.Window(WINDOW_ID, windowRect, DrawPresetWindow, "Presets", GUILayout.Width(200), GUILayout.Height(0));
            }
            else
            {
                GeneralUI.ClearLocks(TEXT_FIELD_GROUP);
            }
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
    }
}
