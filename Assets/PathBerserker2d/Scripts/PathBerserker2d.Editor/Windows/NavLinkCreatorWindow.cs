using System;
using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    internal class NavLinkCreatorWindow : EditorWindow
    {
        [MenuItem("Window/AI/NavLinkCreator")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            NavLinkCreatorWindow window = (NavLinkCreatorWindow)EditorWindow.GetWindow(typeof(NavLinkCreatorWindow));
            window.Show();
        }

        NavLink navLinkToCopy;
        Transform parent;

        Vector2 firstPoint;
        bool firstPointPlaced;
        bool isActive = true;

        private void OnEnable()
        {
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
            SceneView.duringSceneGui += SceneView_duringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= SceneView_duringSceneGui;
        }

        void OnGUI()
        {
            MyGUI.Header("Link Settings");
            parent = EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true) as Transform;
            navLinkToCopy = EditorGUILayout.ObjectField("Link to instantiate", navLinkToCopy, typeof(NavLink), true) as NavLink;

            EditorGUILayout.HelpBox("Left mouse button to place link node. Right mouse button to abort.", MessageType.Info);

            isActive = EditorGUILayout.Toggle("Is Active", isActive);
        }

        private void SceneView_duringSceneGui(SceneView obj)
        {
            if (!isActive || navLinkToCopy == null)
                return;

            Event current = Event.current;

            if (current.type == EventType.MouseDown)
            {
                if (current.button == 0)
                {
                    if (!firstPointPlaced)
                    { 
                        firstPoint = HandleUtility.GUIPointToWorldRay(current.mousePosition).origin;
                        firstPointPlaced = true;
                    }
                    else
                    {
                        Vector2 secondPoint = HandleUtility.GUIPointToWorldRay(current.mousePosition).origin;
                        var link = Instantiate(navLinkToCopy);
                        link.transform.parent = parent;
                        link.transform.position = firstPoint + (secondPoint - firstPoint) * 0.5f;
                        
                        var ser = new SerializedObject(link);
                        ser.FindProperty("start").vector2Value = link.transform.InverseTransformPoint(firstPoint);
                        ser.FindProperty("goal").vector2Value = link.transform.InverseTransformPoint(secondPoint);
                        ser.FindProperty("bezierControlPoint").vector2Value = link.transform.InverseTransformPoint(link.transform.position + Vector3.up * 2);
                        ser.ApplyModifiedPropertiesWithoutUndo();

                        firstPointPlaced = false;
                    }
                    current.Use();
                }
                else if (current.button == 1)
                {
                    firstPointPlaced = false;
                    current.Use();
                }
            }

            if (firstPointPlaced)
            {
                Handles.DrawLine(firstPoint, HandleUtility.GUIPointToWorldRay(current.mousePosition).origin);
                SceneView.RepaintAll();
            }
        }
    }
}
