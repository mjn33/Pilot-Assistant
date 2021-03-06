﻿using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant
{
    using UI;
    using Utility;
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AppLauncherFlight : MonoBehaviour
    {
        private ApplicationLauncherButton btnLauncher;
        private Rect windowRect = new Rect(Screen.width, 38, 200, 30);

        private const int WINDOW_ID = 0984653;

        private bool showOptionsWindow = false;

        private void Awake()
        {
            //GameEvents.onGUIApplicationLauncherReady.Add(this.OnAppLauncherReady);
            OnAppLauncherReady();
        }

        private void OnDestroy()
        {
            //GameEvents.onGUIApplicationLauncherReady.Remove(this.OnAppLauncherReady);
            if (btnLauncher != null)
                ApplicationLauncher.Instance.RemoveModApplication(btnLauncher);
        }

        private void OnAppLauncherReady()
        {
            btnLauncher = ApplicationLauncher.Instance.AddModApplication(
                OnToggleTrue, OnToggleFalse,
                null, null, null, null,
                ApplicationLauncher.AppScenes.ALWAYS,
                GameDatabase.Instance.GetTexture("Pilot Assistant/Icons/AppLauncherIcon", false));
        }

        private void OnGameSceneChange(GameScenes scene)
        {
            ApplicationLauncher.Instance.RemoveModApplication(btnLauncher);
        }

        private void OnToggleTrue()
        {
            showOptionsWindow = true;
        }

        private void OnToggleFalse()
        {
            showOptionsWindow = false;
        }

        private void OnGUI()
        {
            GUI.skin = GeneralUI.Skin;
            if (showOptionsWindow)
            {
                windowRect.x = Mathf.Clamp(Screen.width * 0.5f + btnLauncher.transform.position.x - 19.0f,
                                           Screen.width * 0.5f,
                                           Screen.width - windowRect.width);
                windowRect = GUILayout.Window(WINDOW_ID, windowRect, DrawOptionsWindow, "",
                                              GeneralUI.Style(UIStyle.OptionsWindow),
                                              GUILayout.Width(200), GUILayout.Height(0));
            }
        }

        private void DrawOptionsWindow(int id)
        {
            bool tmpToggle = GUILayout.Toggle(ShowPA, "Pilot Assistant", GeneralUI.Style(UIStyle.ToggleButton));
            if (tmpToggle != ShowPA)
            {
                ShowPA = !ShowPA;
                btnLauncher.toggleButton.Value = false;
            }

            tmpToggle = GUILayout.Toggle(ShowSAS, "SAS Systems", GeneralUI.Style(UIStyle.ToggleButton));
            if (tmpToggle != ShowSAS)
            {
                ShowSAS = !ShowSAS;
                btnLauncher.toggleButton.Value = false;
            }
        }

        public static bool ShowPA { get; private set; }
        public static bool ShowSAS { get; private set; }
    }
}
