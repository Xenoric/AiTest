using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    internal static class MyGUI
    {
        public static string nameSeed = "aWJhcmFraQ==";
        public static string nameSeed2 = "aWJhcl9ha2k=";
        static GUIStyle horizontalLine;

        static MyGUI()
        {
            horizontalLine = new GUIStyle();
            horizontalLine.normal.background = EditorGUIUtility.whiteTexture;
            horizontalLine.margin = new RectOffset(0, 0, 4, 4);
            horizontalLine.fixedHeight = 1;
        }

        // utility method
        public static void HorizontalLine(Color color)
        {
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, horizontalLine);
            GUI.color = c;
        }

        public static void Header(string text)
        {
            EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
        }

        public static void DrawNavTagLayout(SerializedProperty spNavTag)
        {
            EditorGUILayout.BeginHorizontal();
            spNavTag.intValue = EditorGUILayout.Popup("NavTag", spNavTag.intValue, PathBerserker2dSettings.NavTags);
            if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(17)))
            {
                SettingsService.OpenProjectSettings(PathBerserker2dSettingsProvider.WindowPath);
            }
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawNavTagColorPickerLayout(SerializedProperty spNavTag)
        {
            int tag = spNavTag.intValue;
            if (tag == 0)
                GUI.enabled = false;

            PathBerserker2dSettings.SetNavTagColor(tag, EditorGUILayout.ColorField("NavTag Color", PathBerserker2dSettings.GetNavTagColor(tag)));

            GUI.enabled = true;
        }

        public static void ProVersionOnlyLabelLayout()
        {
            EditorGUILayout.LabelField(ProVersionOnly);
        }

        public static void ProVersionLinkTypeLabelLayout()
        {
            EditorGUILayout.LabelField("Custom link types are limited to the pro-version.");
        }

        public static GUIContent AddProVersionOnlyToolTipp(string label)
        {
            return new GUIContent(label, ProVersionOnly);
        }

        public const string ProVersionOnly = "This is a pro-version only feature";
    }
}
