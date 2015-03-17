using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.AppLauncher
{
    using Utility;
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AppLauncherFlight : MonoBehaviour
    {
        private ApplicationLauncherButton btnLauncher;
        private Rect windowRect = new Rect(Screen.width, 38, 200, 30);

        private const int WINDOW_ID = 0984653;

        public static bool bDisplayOptions = false;
        public static bool bDisplayAssistant = false;
        public static bool bDisplaySAS = false;

        private void Awake()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(this.OnAppLauncherReady);

            RenderingManager.AddToPostDrawQueue(5, DrawGUI);
        }

        private void OnDestroy()
        {
            RenderingManager.RemoveFromPostDrawQueue(5, DrawGUI);

            GameEvents.onGUIApplicationLauncherReady.Remove(this.OnAppLauncherReady);
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
            bDisplayOptions = true;
        }

        private void OnToggleFalse()
        {
            bDisplayOptions = false;
        }

        private void DrawGUI()
        {
            GUI.skin = GeneralUI.Skin;
            if (bDisplayOptions)
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
            bool tmpToggle = GUILayout.Toggle(bDisplayAssistant, "Pilot Assistant", GeneralUI.Style(UIStyle.ToggleButton));
            if (tmpToggle != bDisplayAssistant)
            {
                bDisplayAssistant = !bDisplayAssistant;
                btnLauncher.toggleButton.SetFalse();
            }

            tmpToggle = GUILayout.Toggle(bDisplaySAS, "SAS Systems", GeneralUI.Style(UIStyle.ToggleButton));
            if (tmpToggle != bDisplaySAS)
            {
                bDisplaySAS = !bDisplaySAS;
                btnLauncher.toggleButton.SetFalse();
            }
        }
    }
}
