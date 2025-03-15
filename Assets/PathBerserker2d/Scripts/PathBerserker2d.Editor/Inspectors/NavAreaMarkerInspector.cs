using UnityEditor;

namespace PathBerserker2d
{
    [CustomEditor(typeof(NavAreaMarker)), CanEditMultipleObjects]
    internal class NavAreaMarkerInspector : Editor
    {
        SerializedProperty spNavTag;
        SerializedProperty spMaxAngle;
        SerializedProperty spMinAngle;
        SerializedProperty spUpdateAfterTimeOfNoMovement;

        public void OnEnable()
        {
            spNavTag = serializedObject.FindProperty("navTag");
            spMinAngle = serializedObject.FindProperty("minAngle");
            spMaxAngle = serializedObject.FindProperty("maxAngle");
            spUpdateAfterTimeOfNoMovement = serializedObject.FindProperty("updateAfterTimeOfNoMovement");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(spMinAngle);
            EditorGUILayout.PropertyField(spMaxAngle);
            EditorGUILayout.PropertyField(spUpdateAfterTimeOfNoMovement);
            MyGUI.DrawNavTagLayout(spNavTag);
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginChangeCheck();
            MyGUI.DrawNavTagColorPickerLayout(spNavTag);
            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();
        }
    }
}
