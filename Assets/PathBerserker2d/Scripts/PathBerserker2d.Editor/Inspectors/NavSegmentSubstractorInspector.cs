using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    [CustomEditor(typeof(NavSegmentSubstractor))]
    internal class NavSegmentSubstractorInspector : Editor
    {
        SerializedProperty spfromAngle;
        SerializedProperty sptoAngle;

        private void OnEnable()
        {
            spfromAngle = serializedObject.FindProperty("fromAngle");
            sptoAngle = serializedObject.FindProperty("toAngle");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(spfromAngle);
            EditorGUILayout.PropertyField(sptoAngle);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            var t = (target as NavSegmentSubstractor).GetComponent<Transform>();
            if (t.localRotation != Quaternion.identity)
            {
                EditorGUILayout.HelpBox("Rotation will not affect the rect.", MessageType.Warning);
            }
        }
    }
}
