using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    internal class ReadOnlyAttributeDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool oldValue = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = oldValue;
        }
    }
}
