using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rowlan.Fullscreen
{
    /// <summary>
    /// Persistent editor preferences for the Fullscreen Game View feature.
    /// Stores keybind assignments and the auto-fullscreen toggle via EditorPrefs.
    /// Provides a SettingsProvider for the Unity Preferences window under "Rowlan/Fullscreen".
    /// </summary>
    public static class FullscreenSettings
    {
        #region Pref Keys

        private const string Prefix = "Rowlan.Fullscreen.";
        private const string ToggleKeyPref = Prefix + "ToggleKey";
        private const string ExitKeyPref = Prefix + "ExitKey";
        private const string AutoFullscreenPref = Prefix + "AutoFullscreen";
        private const string VSyncPref = Prefix + "VSync";
        private const string HideCursorPref = Prefix + "HideCursor";

        #endregion

        #region Defaults

        private const Key DefaultToggleKey = Key.F11;
        private const Key DefaultExitKey = Key.Escape;
        private const bool DefaultAutoFullscreen = false;
        private const bool DefaultVSync = true;
        private const bool DefaultHideCursor = true;

        #endregion

        #region Properties

        /// <summary>
        /// The key used to toggle fullscreen on and off during Play Mode.
        /// </summary>
        public static Key ToggleKey
        {
            get => (Key)EditorPrefs.GetInt(ToggleKeyPref, (int)DefaultToggleKey);
            set => EditorPrefs.SetInt(ToggleKeyPref, (int)value);
        }

        /// <summary>
        /// The key used to exit fullscreen without stopping Play Mode.
        /// </summary>
        public static Key ExitKey
        {
            get => (Key)EditorPrefs.GetInt(ExitKeyPref, (int)DefaultExitKey);
            set => EditorPrefs.SetInt(ExitKeyPref, (int)value);
        }

        /// <summary>
        /// Whether fullscreen should activate automatically when entering Play Mode.
        /// </summary>
        public static bool AutoFullscreen
        {
            get => EditorPrefs.GetBool(AutoFullscreenPref, DefaultAutoFullscreen);
            set => EditorPrefs.SetBool(AutoFullscreenPref, value);
        }

        /// <summary>
        /// Whether VSync should be enabled during fullscreen Play Mode.
        /// When disabled, the frame rate is uncapped (useful for performance testing).
        /// The original VSync setting is restored when exiting fullscreen.
        /// </summary>
        public static bool VSync
        {
            get => EditorPrefs.GetBool(VSyncPref, DefaultVSync);
            set => EditorPrefs.SetBool(VSyncPref, value);
        }

        /// <summary>
        /// Whether the mouse cursor should be hidden during fullscreen Play Mode.
        /// The original cursor visibility is restored when exiting fullscreen.
        /// </summary>
        public static bool HideCursor
        {
            get => EditorPrefs.GetBool(HideCursorPref, DefaultHideCursor);
            set => EditorPrefs.SetBool(HideCursorPref, value);
        }

        #endregion

        #region Settings Provider

        /// <summary>
        /// Registers the preferences panel under Edit → Preferences → Rowlan/Fullscreen.
        /// </summary>
        /// <returns>A SettingsProvider instance for the Unity Preferences window.</returns>
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Rowlan/Fullscreen", SettingsScope.User)
            {
                label = "Fullscreen",
                keywords = new HashSet<string> { "fullscreen", "game view", "play mode" },
                guiHandler = OnGUI
            };
        }

        /// <summary>
        /// Draws the preferences GUI with keybind fields, auto-fullscreen toggle,
        /// and a reset button.
        /// </summary>
        /// <param name="searchContext">The current search filter from the Preferences window.</param>
        private static void OnGUI(string searchContext)
        {
            EditorGUIUtility.labelWidth = 200;
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Keybinds", EditorStyles.boldLabel);

            ToggleKey = (Key)EditorGUILayout.EnumPopup("Toggle Fullscreen", ToggleKey);
            ExitKey = (Key)EditorGUILayout.EnumPopup("Exit Fullscreen", ExitKey);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Behavior", EditorStyles.boldLabel);

            AutoFullscreen = EditorGUILayout.Toggle("Fullscreen On Play", AutoFullscreen);
            VSync = EditorGUILayout.Toggle(
                new GUIContent("VSync", "Enable VSync during fullscreen. Disable to uncap frame rate for performance testing."),
                VSync);
            HideCursor = EditorGUILayout.Toggle(
                new GUIContent("Hide Cursor", "Hide the mouse cursor during fullscreen. Useful for gamepad-only or presentation scenarios."),
                HideCursor);

            EditorGUILayout.Space(20);

            if (UnityEngine.GUILayout.Button("Reset to Defaults", UnityEngine.GUILayout.Width(150)))
            {
                ResetToDefaults();
            }
        }

        /// <summary>
        /// Resets all preferences to their default values.
        /// </summary>
        private static void ResetToDefaults()
        {
            ToggleKey = DefaultToggleKey;
            ExitKey = DefaultExitKey;
            AutoFullscreen = DefaultAutoFullscreen;
            VSync = DefaultVSync;
            HideCursor = DefaultHideCursor;
        }

        #endregion
    }
}