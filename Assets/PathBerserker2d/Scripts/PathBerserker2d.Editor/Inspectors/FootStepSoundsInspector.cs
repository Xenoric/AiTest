using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PathBerserker2d.Examples
{
    [CustomEditor(typeof(FootStepSounds))]
    public class FootStepSoundsInspector : Editor
    {
        SerializedProperty spAudioSource;
        SerializedProperty spAgent;
        SerializedProperty spFootStepDelay;
        SerializedProperty spDefaultFootstep;
        SerializedProperty spFootstepSounds;
        ReorderableList footstepList;

        public void OnEnable()
        {
            spAudioSource = serializedObject.FindProperty("audioSource");
            spAgent = serializedObject.FindProperty("agent");
            spFootStepDelay = serializedObject.FindProperty("footStepDelay");
            spDefaultFootstep = serializedObject.FindProperty("defaultFootstep");
            spFootstepSounds = serializedObject.FindProperty("footstepSounds");

            footstepList = new ReorderableList(serializedObject, spFootstepSounds, true, true, false, false);
            footstepList.drawHeaderCallback = HeaderCallback;
            footstepList.drawElementCallback = DrawElementCallback;
        }

        private void DrawElementCallback(Rect rect, int index, bool isActive, bool isFocused)
        {
            float width = rect.width;
            rect.width = 150;
            EditorGUI.LabelField(rect, PathBerserker2dSettings.NavTags[index]);
            rect.x = 150;
            rect.width = width - 150;
            EditorGUI.PropertyField(rect, spFootstepSounds.GetArrayElementAtIndex(index), new GUIContent(""));
        }

        private void HeaderCallback(Rect rect)
        {
            EditorGUI.LabelField(rect, "Footsteps");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(spAudioSource);
            EditorGUILayout.PropertyField(spAgent);
            EditorGUILayout.PropertyField(spFootStepDelay);
            EditorGUILayout.PropertyField(spDefaultFootstep);

            footstepList.DoLayoutList();

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}