using UnityEngine;
using UnityEditor;
using System;

namespace PathBerserker2d
{
    internal class BaseNavLinkInspector : Editor
    {
        protected SerializedProperty spCostOverride;
        protected SerializedProperty spClearance;
        protected SerializedProperty spAvgWaitTime;
        protected SerializedProperty spMaxTraversableDistance;
        SerializedProperty spLinkType;
        protected SerializedProperty spNavTag;
        protected SerializedProperty spAutoMap;
        string[] filteredLinkTypes;
        private static bool advancedOpen;

        public virtual void OnEnable()
        {
            spCostOverride = serializedObject.FindProperty("costOverride");
            spLinkType = serializedObject.FindProperty("linkType");
            spClearance = serializedObject.FindProperty("clearance");
            spNavTag = serializedObject.FindProperty("navTag");
            spAvgWaitTime = serializedObject.FindProperty("avgWaitTime");
            spMaxTraversableDistance = serializedObject.FindProperty("maxTraversableDistance");
            spAutoMap = serializedObject.FindProperty("autoMap");

            filteredLinkTypes = new string[PathBerserker2dSettings.NavLinkTypeNames.Length - 1];
            Array.Copy(PathBerserker2dSettings.NavLinkTypeNames, 1, filteredLinkTypes, 0, filteredLinkTypes.Length);
        }

        protected void DrawLinkTypeField()
        {
            EditorGUILayout.BeginHorizontal();

            if (filteredLinkTypes.Length != PathBerserker2dSettings.NavLinkTypeNames.Length - 1)
            {
                filteredLinkTypes = new string[PathBerserker2dSettings.NavLinkTypeNames.Length - 1];
                Array.Copy(PathBerserker2dSettings.NavLinkTypeNames, 1, filteredLinkTypes, 0, filteredLinkTypes.Length);
            }
            spLinkType.intValue = EditorGUILayout.Popup("Link Type", spLinkType.intValue - 1, filteredLinkTypes) + 1;
            if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(17)))
            {
                SettingsService.OpenProjectSettings(PathBerserker2dSettingsProvider.WindowPath);
            }
            EditorGUILayout.EndHorizontal();
        }

        protected void DrawAdvancedSection()
        {
            advancedOpen = EditorGUILayout.Foldout(advancedOpen, "Advanced");
            if (advancedOpen)
            {
                DrawAdvancedOptions();
            }
        }

        protected virtual void DrawAdvancedOptions()
        {
            EditorGUILayout.PropertyField(spCostOverride);
            EditorGUILayout.PropertyField(spAvgWaitTime);
            EditorGUILayout.PropertyField(spMaxTraversableDistance);
        }
    }
}