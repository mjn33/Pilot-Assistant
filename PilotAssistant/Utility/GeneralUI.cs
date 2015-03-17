using System;
using System.Collections.Generic;
using UnityEngine;

namespace PilotAssistant.Utility
{
    public enum UIStyle
    {
        AlertLabel,
        BoldLabel,
        NumBoxText,
        GUISection,
        ToggleButton,
        SpinnerLabel,
        SpinnerPlus,
        SpinnerMinus,
        OptionsWindow
    }

    public static class GeneralUI
    {
        private static GUISkin skin = null;

        public static GUISkin Skin
        {
            get { EnsureSkinInitialized(); return skin; }
        }

        // Not using properties due it requiring this hack:
        // http://stackoverflow.com/questions/3547739/properties-exposing-array-elements-in-c-sharp
        public static GUIStyle Style(UIStyle style)
        {
            EnsureSkinInitialized(); return skin.customStyles[(int)style];
        }

        public static Color SSASActiveBGColor
        {
            get { return XKCDColors.BrightOrange; }
        }
        public static Color SSASInactiveBGColor
        {
            get { return XKCDColors.BrightSkyBlue; }
        }

        // Used to track the state of a text field group.
        private class TextFieldGroupState
        {
            public TextFieldGroupState() { counter = 0; locked = false; }
            public int counter;
            public bool locked;
        }

        // A map from text field group name to text field group state. 
        private static Dictionary<string, TextFieldGroupState> textFieldGroups = new Dictionary<string, TextFieldGroupState>();

        // Begin a new text field group, sets the counter to zero
        public static void StartTextFieldGroup(string groupName)
        {
            TextFieldGroupState st = null;
            if (textFieldGroups.TryGetValue(groupName, out st))
                st.counter = 0;
            else
                textFieldGroups[groupName] = new TextFieldGroupState();
        }

        // Mark the next control as a text field. Actually any control which we want to lock input for.
        public static void TextFieldNext(string groupName)
        {
            TextFieldGroupState st = null;
            if (textFieldGroups.TryGetValue(groupName, out st))
            {
                // st.counter used so names are unique.
                GUI.SetNextControlName("IMPORTANT_TEXTFIELD_" + groupName + st.counter);
                ++st.counter;
            }
        }

        // Mark the end of the text field group, automatically lock if any control has focus. 
        public static bool AutolockTextFieldGroup(string groupName, ControlTypes mask)
        {
            TextFieldGroupState st = null;
            if (textFieldGroups.TryGetValue(groupName, out st))
            {
                string name = GUI.GetNameOfFocusedControl();
                bool focus = name.StartsWith("IMPORTANT_TEXTFIELD_" + groupName);
                if (focus && !st.locked)
                {
                    st.locked = true;
                    InputLockManager.SetControlLock(mask, groupName + "_ControlLock");
                }
                else if (!focus && st.locked)
                {
                    st.locked = false;
                    InputLockManager.RemoveControlLock(groupName + "_ControlLock");
                }
                return st.locked;
            }
            return false;
        }

        // Clear the lock for a specific text field group
        public static void ClearLocks(string groupName)
        {
            TextFieldGroupState st = null;
            if (textFieldGroups.TryGetValue(groupName, out st))
            {
                st.counter = 0;
                if (st.locked)
                {
                    st.locked = false;
                    InputLockManager.RemoveControlLock(groupName + "_ControlLock");
                }
            }
        }

        private static void EnsureSkinInitialized()
        {
            if (skin != null)
                return;

            skin = (GUISkin)GUISkin.Instantiate(HighLogic.Skin);
            skin.customStyles = new GUIStyle[Enum.GetNames(typeof(UIStyle)).Length];

            // Style for the paused message
            GUIStyle alertLabelStyle = new GUIStyle(skin.label);
            alertLabelStyle.alignment = TextAnchor.MiddleCenter;
            alertLabelStyle.normal.textColor = XKCDColors.Red;
            alertLabelStyle.fontSize = 21;
            alertLabelStyle.fontStyle = FontStyle.Bold;
            skin.customStyles[(int)UIStyle.AlertLabel] = alertLabelStyle;

            // Style for text box to align with increment buttons better
            GUIStyle numBoxTextStyle = new GUIStyle(skin.textField);
            numBoxTextStyle.alignment = TextAnchor.MiddleLeft;
            numBoxTextStyle.margin = new RectOffset(4, 0, 5, 3);
            skin.customStyles[(int)UIStyle.NumBoxText] = numBoxTextStyle;

            // Style for a box for grouping related GUI elements
            GUIStyle guiSectionStyle = new GUIStyle(skin.box);
            guiSectionStyle.normal.textColor
                = guiSectionStyle.focused.textColor
                = Color.white;
            guiSectionStyle.hover.textColor
                = guiSectionStyle.active.textColor
                = Color.yellow;
            guiSectionStyle.onNormal.textColor
                = guiSectionStyle.onFocused.textColor
                = guiSectionStyle.onHover.textColor
                = guiSectionStyle.onActive.textColor
                = Color.green;
            guiSectionStyle.padding = new RectOffset(4, 4, 4, 4);
            skin.customStyles[(int)UIStyle.GUISection] = guiSectionStyle;

            // Style for a toggle control that looks like a button
            GUIStyle toggleButtonStyle = new GUIStyle(skin.button);
            toggleButtonStyle.normal.textColor
                = toggleButtonStyle.focused.textColor
                = Color.white;
            toggleButtonStyle.hover.textColor
                = toggleButtonStyle.active.textColor
                = toggleButtonStyle.onActive.textColor
                = Color.yellow;
            toggleButtonStyle.onNormal.textColor
                = toggleButtonStyle.onFocused.textColor
                = toggleButtonStyle.onHover.textColor
                = Color.green;
            toggleButtonStyle.padding = new RectOffset(4, 4, 4, 4);
            skin.customStyles[(int)UIStyle.ToggleButton] = toggleButtonStyle;

            // Style for regular buttons
            GUIStyle buttonStyle = new GUIStyle(skin.button);
            buttonStyle.padding = new RectOffset(4, 4, 4, 4);
            skin.button = buttonStyle;

            // Style for increment button
            GUIStyle spinnerPlusBtnStyle = new GUIStyle(skin.button);
            spinnerPlusBtnStyle.margin = new RectOffset(2, 2, 0, 0);
            skin.customStyles[(int)UIStyle.SpinnerPlus] = spinnerPlusBtnStyle;

            // Style for derement button
            GUIStyle spinnerMinusBtnStyle = new GUIStyle(skin.button);
            spinnerMinusBtnStyle.margin = new RectOffset(2, 2, 0, 0);
            skin.customStyles[(int)UIStyle.SpinnerMinus] = spinnerMinusBtnStyle;

            // Style for label to align with increment buttons
            GUIStyle spinnerLabelStyle = new GUIStyle(skin.label);
            spinnerLabelStyle.alignment = TextAnchor.MiddleLeft;
            spinnerLabelStyle.margin = new RectOffset(4, 4, 5, 3);
            skin.customStyles[(int)UIStyle.SpinnerLabel] = spinnerLabelStyle;

            // Style for bold labels
            GUIStyle boldLabelStyle = new GUIStyle(skin.label);
            boldLabelStyle.fontStyle = FontStyle.Bold;
            boldLabelStyle.alignment = TextAnchor.MiddleLeft;
            skin.customStyles[(int)UIStyle.BoldLabel] = boldLabelStyle;

            GUIStyle optionsWindowStyle = new GUIStyle(skin.window);
            optionsWindowStyle.padding = new RectOffset(0, 0, 0, 0);
            optionsWindowStyle.margin = new RectOffset(0, 0, 0, 0);
            skin.customStyles[(int)UIStyle.OptionsWindow] = optionsWindowStyle;
        }

        /// <summary>
        /// Draws a label and text box of specified widths with +/- 10% increment buttons. Returns the numeric value of
        /// the text box.
        /// </summary>
        /// <param name="textFieldGroup">The text field group the input box should have.</param>
        /// <param name="labelText">text for the label</param>
        /// <param name="boxVal">number to display in text box</param>
        /// <param name="labelWidth"></param>
        /// <param name="boxWidth"></param>
        /// <returns>edited value of the text box</returns>
        public static double LabPlusNumBox(
            string textFieldGroup,
            string labelText,
            double boxVal,
            string format,
            float labelWidth = 100,
            float boxWidth = 60)
        {
            string boxText = (format != null) ? boxVal.ToString(format) : boxVal.ToString();
            GUILayout.BeginHorizontal();

            GUILayout.Label(labelText, Style(UIStyle.SpinnerLabel), GUILayout.Width(labelWidth));
            GeneralUI.TextFieldNext(textFieldGroup);
            string text = GUILayout.TextField(boxText, Style(UIStyle.NumBoxText), GUILayout.Width(boxWidth));
            try
            {
                boxVal = double.Parse(text);
            }
            catch {}
            GUILayout.BeginVertical();
            if (GUILayout.Button("+", Style(UIStyle.SpinnerPlus), GUILayout.Width(20), GUILayout.Height(13)))
            {
                if (boxVal != 0)
                    boxVal *= 1.1;
                else
                    boxVal = 0.01;
            }
            if (GUILayout.Button("-", Style(UIStyle.SpinnerMinus), GUILayout.Width(20), GUILayout.Height(13)))
            {
                boxVal /= 1.1;
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            return boxVal;
        }

        /// <summary>
        /// Draws a label and text box of specified widths with +/- 1 increment buttons. Returns the numeric value of
        /// the text box
        /// </summary>
        /// <param name="textFieldGroup">The text field group the input box should have.</param>
        /// <param name="toggleText"></param>
        /// <param name="boxVal"></param>
        /// <param name="toggleWidth"></param>
        /// <param name="boxWidth"></param>
        /// <param name="upper">upper value to which input will be clamped, attempting to increase will roll value down
        /// to lower</param>
        /// <param name="lower">lower value to which input will be clamped, attempting to decrease will roll value up to
        /// upper</param>
        /// <returns></returns>
        public static double TogPlusNumBox(
            string textFieldGroup,
            string toggleText,
            ref bool toggleState,
            double boxVal,
            float toggleWidth = 100,
            float boxWidth = 60,
            float upper = 360,
            float lower = -360)
        {
            GUILayout.BeginHorizontal();
            // state is returned by reference
            toggleState = GUILayout.Toggle(toggleState, toggleText, Style(UIStyle.ToggleButton), GUILayout.Width(toggleWidth));
            GeneralUI.TextFieldNext(textFieldGroup);
            string text = GUILayout.TextField(boxVal.ToString("F2"), Style(UIStyle.NumBoxText), GUILayout.Width(boxWidth));
            try
            {
                boxVal = double.Parse(text);
            }
            catch {}
            GUILayout.BeginVertical();
            if (GUILayout.Button("+", Style(UIStyle.SpinnerPlus), GUILayout.Width(20), GUILayout.Height(13)))
            {
                boxVal += 1;
                if (boxVal >= upper)
                    boxVal = lower;
            }
            if (GUILayout.Button("-", Style(UIStyle.SpinnerMinus), GUILayout.Width(20), GUILayout.Height(13)))
            {
                boxVal -= 1;
                if (boxVal < lower)
                    boxVal = upper - 1;
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            return Functions.Clamp(boxVal, lower, upper);
        }
    }
}
