using UnityEngine;
using UnityEditor;
using System;

namespace PathBerserker2d
{
    [CustomEditor(typeof(NavLink)), CanEditMultipleObjects]
    internal class NavLinkInspector : BaseNavLinkInspector
    {
        SerializedProperty spStart;
        SerializedProperty spGoal;
        SerializedProperty spIsBidirectional;
        SerializedProperty spVisualizationType;
        SerializedProperty spTraversalAngle;
        SerializedProperty spBezierControlPoint;
        SerializedProperty spHorizontalSpeed;


        bool visualizationOpen;
        bool infoOpen;

        GUIStyle distanceLabelStyle;
        NavLink link;

        public override void OnEnable()
        {
            base.OnEnable();

            spStart = serializedObject.FindProperty("start");
            spGoal = serializedObject.FindProperty("goal");
            spIsBidirectional = serializedObject.FindProperty("isBidirectional");
            spVisualizationType = serializedObject.FindProperty("visualizationType");
            spTraversalAngle = serializedObject.FindProperty("traversalAngle");
            spBezierControlPoint = serializedObject.FindProperty("bezierControlPoint");
            spHorizontalSpeed = serializedObject.FindProperty("horizontalSpeed");

            if (distanceLabelStyle == null)
            {
                try
                {
                    // editorStyles.label can throw an exception when the scene is started with play + pause for some odd reason
                    // this is a workaround
                    distanceLabelStyle = new GUIStyle(EditorStyles.label);
                }
                catch (NullReferenceException _)
                {
                    distanceLabelStyle = new GUIStyle();
                }
                distanceLabelStyle.alignment = TextAnchor.MiddleCenter;
                distanceLabelStyle.normal.textColor = Color.white;
            }

            startHandle = new PositionHandle2D(Color.white, new Color(1, 1, 160f / 255f), Color.yellow);
            goalHandle = new PositionHandle2D(Color.white, new Color(1, 1, 160f / 255f), Color.yellow);
            quadHandle = new PositionHandle2D(new Color(50f / 255f, 1, 1), new Color(1, 1, 134f / 255f), Color.yellow);

            link = target as NavLink;
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(spStart);
            EditorGUILayout.PropertyField(spGoal);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reverse"))
            {
                foreach (var t in targets)
                {
                    var link = t as NavLink;
                    var swap = link.StartLocalPosition;
                    link.StartLocalPosition = link.GoalLocalPosition;
                    link.GoalLocalPosition = swap;
                }
                SceneView.RepaintAll();
            }
            if (GUILayout.Button("Center Pivot"))
            {
                foreach (var t in targets)
                {
                    var link = t as NavLink;

                    Vector2 start = link.transform.TransformPoint(link.StartLocalPosition);
                    Vector2 goal = link.transform.TransformPoint(link.GoalLocalPosition);
                    Vector2 worldCP = link.transform.TransformPoint(link.BezierControlPoint);

                    Vector2 newPivot = start + (goal - start) * 0.5f;
                    link.transform.position = new Vector3(newPivot.x, newPivot.y, link.transform.position.z);
                    link.StartLocalPosition = link.transform.InverseTransformPoint(start);
                    link.GoalLocalPosition = link.transform.InverseTransformPoint(goal);

                    link.BezierControlPoint = link.transform.InverseTransformPoint(worldCP);
                }
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(spIsBidirectional);

            MyGUI.Header("Properties");
            DrawLinkTypeField();
            MyGUI.DrawNavTagLayout(spNavTag);
            EditorGUILayout.PropertyField(spClearance);
            EditorGUILayout.PropertyField(spAutoMap);

            DrawAdvancedSection();

            visualizationOpen = EditorGUILayout.Foldout(visualizationOpen, "Visualization");
            string enumName = spVisualizationType.enumNames[spVisualizationType.enumValueIndex];
            if (visualizationOpen)
            {
                EditorGUILayout.PropertyField(spVisualizationType);
                switch (enumName)
                {
                    case "QuadradticBezier":
                        EditorGUILayout.PropertyField(spTraversalAngle);
                        EditorGUILayout.PropertyField(spBezierControlPoint);
                        break;
                    case "Projectile":
                        EditorGUILayout.PropertyField(spTraversalAngle);
                        spHorizontalSpeed.floatValue = EditorGUILayout.Slider("Horizontal Speed", spHorizontalSpeed.floatValue, 0.1f, 20);
                        break;
                }
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (targets.Length == 1)
            {
                infoOpen = EditorGUILayout.Foldout(infoOpen, "Info");

                if (infoOpen)
                {

                    Vector2 g = link.GoalWorldPosition;
                    Vector2 s = link.StartWorldPosition;

                    EditorGUILayout.LabelField("Traversal Costs", link.TravelCosts(s, g).ToString("N2"));
                    EditorGUILayout.LabelField("Distance", (g - s).magnitude.ToString("N2"));
                    EditorGUILayout.LabelField("Horizontal Distance", Mathf.Abs(g.x - s.x).ToString("N2"));
                    EditorGUILayout.LabelField("Vertical Distance", Mathf.Abs(g.y - s.y).ToString("N2"));

                    if (enumName == "Projectile")
                    {
                        float t = Mathf.Abs(g.x - s.x) / spHorizontalSpeed.floatValue;
                        float grav = 9.81f * t * 0.5f;
                        float heightDelta = (s.y - g.y) / t;
                        EditorGUILayout.LabelField("JumpAcceleration(start->goal)", (grav - heightDelta).ToString("N2"));
                        if (spIsBidirectional.boolValue)
                            EditorGUILayout.LabelField("JumpAcceleration(goal->start)", (grav - (g.y - s.y) / t).ToString("N2"));
                    }
                }
            }

            if (Application.IsPlaying(link) && !link.IsAddedToWorld)
            {
                EditorGUILayout.HelpBox("Link is not added to the pathfinder. It will not be considered for pathfinding.", MessageType.Warning);
            }
        }

        private PositionHandle2D startHandle;
        private PositionHandle2D goalHandle;
        private PositionHandle2D quadHandle;

        private void OnSceneGUI()
        {
            // when starting a scene in the editor with play + pause, OnEnable might not be called yet


            NavLink link = target as NavLink;
            Handles.matrix = Matrix4x4.Translate(new Vector3(0, 0, link.transform.position.z));

            EditorGUI.BeginChangeCheck();
            Vector2 start = startHandle.DrawHandle(link.StartWorldPosition);
            //Vector2 start = Handles.PositionHandle(link.StartWorldPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "NavLink change start position");
                link.StartWorldPosition = start;
                if (Application.IsPlaying(link) && link.autoMap)
                    link.UpdateMapping();
            }

            EditorGUI.BeginChangeCheck();
            Vector2 goal = goalHandle.DrawHandle(link.GoalWorldPosition);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "NavLink change goal position");
                link.GoalWorldPosition = goal;
                if (Application.IsPlaying(link) && link.autoMap)
                    link.
                        UpdateMapping();
            }

            string enumName = spVisualizationType.enumNames[spVisualizationType.enumValueIndex];
            switch (enumName)
            {
                case "QuadradticBezier":
                    EditorGUI.BeginChangeCheck();
                    Vector2 cp = quadHandle.DrawHandle(link.transform.TransformPoint(link.BezierControlPoint));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "NavLink change bezier point");
                        link.BezierControlPoint = link.transform.InverseTransformPoint(cp);
                    }
                    break;
            }

            Handles.color = Color.red;
            Handles.DrawWireDisc(start, Vector3.forward, PathBerserker2dSettings.PointMappingDistance);
            Handles.DrawWireDisc(goal, Vector3.forward, PathBerserker2dSettings.PointMappingDistance);

            Vector2 dir = goal - start;
            Handles.BeginGUI();
            Vector2 pos = dir * 0.5f + start;
            Vector2 pos2D = HandleUtility.WorldToGUIPoint(pos);
            var oldMatrix = GUI.matrix;
            float angle = Vector2.SignedAngle(dir, Vector2.up) - 90f;

            angle = angle < -90 ? 180 + angle : angle;

            GUI.matrix = Matrix4x4.TRS(pos2D, Quaternion.Euler(0, 0, angle), Vector3.one) * Matrix4x4.Translate(new Vector2(-35, -20));
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(0, 0, 70, 40), dir.magnitude.ToString("N2"), distanceLabelStyle);
            GUI.matrix = oldMatrix;
            Handles.EndGUI();

            Handles.color = Color.white;
            Camera cam = Camera.current;

            float textLengthWorld = 10;
            if (cam)
            {
                Vector2 startLineEnd = cam.ScreenToWorldPoint(pos2D + new Vector2(-35, 0));
                Vector2 goalLineStart = cam.ScreenToWorldPoint(pos2D + new Vector2(35, 0));

                textLengthWorld = (goalLineStart - startLineEnd).magnitude / 2f;
            }
            Handles.DrawLine(start, pos - dir.normalized * textLengthWorld);
            Handles.DrawLine(pos + dir.normalized * textLengthWorld, goal);
        }
    }
}