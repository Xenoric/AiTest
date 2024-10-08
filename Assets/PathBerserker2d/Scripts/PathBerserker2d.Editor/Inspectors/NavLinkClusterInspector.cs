using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;

namespace PathBerserker2d
{
    [CustomEditor(typeof(NavLinkCluster)), CanEditMultipleObjects]
    internal class NavLinkClusterInspector : BaseNavLinkInspector
    {
        private static bool linkPointsOpen;

        SerializedProperty spLinkPoints;

        bool lockPoints;
        Vector3 lastPosition;
        NavLinkCluster link;

        ReorderableList linkPointList;

        public override void OnEnable()
        {
            base.OnEnable();
            spLinkPoints = serializedObject.FindProperty("linkPoints");

            link = target as NavLinkCluster;
            lastPosition = link.transform.position;

            linkPointList = new ReorderableList(serializedObject, spLinkPoints, true, true, true, true);
            linkPointList.drawHeaderCallback = DrawLinkPointListHeader;
            linkPointList.drawElementCallback = DrawLinkPointListElement;

            posHandles = new PositionHandle2D[link.linkPoints.Length];
            for (int i = 0; i < posHandles.Length; i++)
            {
                posHandles[i] = new PositionHandle2D(Color.white, new Color(1, 1, 160f / 255f), Color.yellow);
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            MyGUI.Header("Location");
            lockPoints = EditorGUILayout.Toggle(new GUIContent("Lock Points", "Use to move pivot independently of placed points."), lockPoints);

            MyGUI.Header("Properties");
            linkPointsOpen = EditorGUILayout.Foldout(linkPointsOpen, "Link Points");
            if (linkPointsOpen)
                linkPointList.DoLayoutList();

            DrawLinkTypeField();
            MyGUI.DrawNavTagLayout(spNavTag);
            EditorGUILayout.PropertyField(spClearance);
            EditorGUILayout.PropertyField(spAutoMap);

            DrawAdvancedSection();

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

        }

        PositionHandle2D[] posHandles;

        private void OnSceneGUI()
        {
            Handles.matrix = Matrix4x4.Translate(new Vector3(0, 0, link.transform.position.z));
            if (lockPoints && lastPosition != link.transform.position)
            {
                // update point pos
                Vector2 delta = link.transform.position - lastPosition;
                for (int i = 0; i < link.linkPoints.Length; i++)
                {
                    link.linkPoints[i].point -= delta;
                }
            }

            if (posHandles.Length != link.linkPoints.Length)
            {
                Array.Resize<PositionHandle2D>(ref posHandles, link.linkPoints.Length);
                for (int i = 0; i < posHandles.Length; i++)
                {
                    if (posHandles[i] == null)
                        posHandles[i] = new PositionHandle2D(Color.white, new Color(1, 1, 160f / 255f), Color.yellow);
                }
            }

            for (int i = 0; i < link.linkPoints.Length; i++)
            {
                EditorGUI.BeginChangeCheck();
                Vector2 v = link.transform.TransformPoint(link.linkPoints[i].point);

                v = link.transform.InverseTransformPoint(posHandles[i].DrawHandle(v));

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "NavLinkCluster changed link position");
                    link.linkPoints[i].point = v;
                    if (Application.IsPlaying(link) && link.autoMap)
                        link.UpdateMapping();
                }
            }
            lastPosition = link.transform.position;
        }

        private void DrawLinkPointListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Link Points");
        }

        private void DrawLinkPointListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var prop = spLinkPoints.GetArrayElementAtIndex(index);

            const float enumSize = 50;

            var tt = prop.FindPropertyRelative("traversalType");
            var p = prop.FindPropertyRelative("point");

            rect.width -= enumSize;

            p.vector2Value = EditorGUI.Vector2Field(rect, "", p.vector2Value);

            rect.x += rect.width;
            rect.width = enumSize;
            EditorGUI.indentLevel = 0;
            EditorGUI.PropertyField(rect, tt, GUIContent.none);
        }
    }
}