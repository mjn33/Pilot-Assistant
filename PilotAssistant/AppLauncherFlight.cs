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
            RenderingManager.AddToPostDrawQueue(5, DrawGUI);
        }

        private void OnDestroy()
        {
            RenderingManager.RemoveFromPostDrawQueue(5, DrawGUI);

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

        private void DrawGUI()
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
            bool tmpToggle = GUILayout.Toggle(PAMainWindow.Instance.IsVisible, "Pilot Assistant", GeneralUI.Style(UIStyle.ToggleButton));
            if (tmpToggle != PAMainWindow.Instance.IsVisible)
            {
                PAMainWindow.Instance.IsVisible = !PAMainWindow.Instance.IsVisible;
                btnLauncher.toggleButton.SetFalse();
            }

            tmpToggle = GUILayout.Toggle(SASMainWindow.Instance.IsVisible, "SAS Systems", GeneralUI.Style(UIStyle.ToggleButton));
            if (tmpToggle != SASMainWindow.Instance.IsVisible)
            {
                SASMainWindow.Instance.IsVisible = !SASMainWindow.Instance.IsVisible;
                btnLauncher.toggleButton.SetFalse();
            }
        }
    }
}
