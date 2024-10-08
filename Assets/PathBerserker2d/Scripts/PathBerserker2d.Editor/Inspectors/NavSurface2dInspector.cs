using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace PathBerserker2d
{
    [CustomEditor(typeof(NavSurface))]
    internal class NavSurface2dInspector : Editor
    {
        private SerializedProperty spNavSegments;
        private SerializedProperty spMaxClearance;
        private SerializedProperty spMinClearance;
        private SerializedProperty spCellSize;
        private SerializedProperty spIncludedColliders;
        private SerializedProperty spMaxSlopeAngle;
        private SerializedProperty spSmallestDistanceYouCareAbout;
        private SerializedProperty spMinSegmentLength;
        private SerializedProperty spOnlyStaticColliders;

        private NavSurface navSurface;
        bool advancedOpen;

        private void OnEnable()
        {
            spNavSegments = serializedObject.FindProperty("navSegments");
            spMaxClearance = serializedObject.FindProperty("maxClearance");
            spMinClearance = serializedObject.FindProperty("minClearance");
            spCellSize = serializedObject.FindProperty("cellSize");
            spIncludedColliders = serializedObject.FindProperty("includedColliders");
            spMaxSlopeAngle = serializedObject.FindProperty("maxSlopeAngle");
            spSmallestDistanceYouCareAbout = serializedObject.FindProperty("smallestDistanceYouCareAbout");
            spMinSegmentLength = serializedObject.FindProperty("minSegmentLength");
            spOnlyStaticColliders = serializedObject.FindProperty("onlyStaticColliders");

            navSurface = (NavSurface)target;

            if (!BakedDataSanityCheck())
            {
                Debug.LogError("Baked data of this NavSurface did not pass sanity check. Please rebake it!");
            }
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(spMaxClearance);
            HeightFromSelectionButton(spMaxClearance, 1, Event.current);

            EditorGUILayout.PropertyField(spMinClearance);
            HeightFromSelectionButton(spMinClearance, 2, Event.current);

            EditorGUILayout.PropertyField(spIncludedColliders);
            EditorGUILayout.PropertyField(spMaxSlopeAngle);
            EditorGUILayout.PropertyField(spOnlyStaticColliders);

            advancedOpen = EditorGUILayout.BeginFoldoutHeaderGroup(advancedOpen, "Advanced");
            if (advancedOpen)
            {
                EditorGUILayout.PropertyField(spCellSize);
                EditorGUILayout.PropertyField(spSmallestDistanceYouCareAbout);
                EditorGUILayout.PropertyField(spMinSegmentLength);
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Bake"))
            {
                if (Application.IsPlaying(navSurface))
                {
                    navSurface.StartCoroutine(navSurface.Bake());
                }
                else
                {
                    navSurface.StartBakeJob();
                }
                EditorApplication.update -= WaitForBakeJobToFinish;
                EditorApplication.update += WaitForBakeJobToFinish;
                Repaint();
            }
            if (navSurface.BakeJob?.IsRunning ?? false)
            {
                Rect r = EditorGUILayout.BeginVertical();
                EditorGUI.ProgressBar(r, navSurface.BakeJob.Progress, "Baking");
                GUILayout.Space(18);
                EditorGUILayout.EndVertical();
                Repaint();
            }
            if (navSurface.NavSegments?.Count > 0 && navSurface.BakeVersion < NavSurface.CurrentBakeVersion)
            {
                EditorGUILayout.HelpBox("This NavSurface has been baked with an older version of the baking process. (Rebake to hide this message)", MessageType.Warning);
            }

            MyGUI.Header("Info");
            GUI.enabled = false;
            EditorGUILayout.LabelField("Segments", spNavSegments.arraySize.ToString());

            GUI.enabled = true;
        }

        private void HeightFromSelectionButton(SerializedProperty prop, int controlId, Event ev)
        {
            if (ev.type == EventType.ExecuteCommand && EditorGUIUtility.GetObjectPickerControlID() == controlId)
            {
                string commandName = ev.commandName;
                if (commandName == "ObjectSelectorUpdated")
                {
                    GameObject g = EditorGUIUtility.GetObjectPickerObject() as GameObject;
                    if (g == null)
                        return;
                    Renderer r = g.GetComponent<Renderer>();
                    if (r == null)
                        return;

                    prop.floatValue = r.bounds.size.y;
                    GUI.changed = true;
                }
            }
            if (GUILayout.Button("From object"))
            {
                EditorGUIUtility.ShowObjectPicker<Renderer>(null, true, "", controlId);
            }
        }

        private void WaitForBakeJobToFinish()
        {
            if (navSurface.BakeJob.IsFinished)
            {
#if DEBUG
                Debug.Log("Bake completed in " + navSurface.BakeJob.TotalBakeTime + "ms");
#endif
                EditorApplication.update -= WaitForBakeJobToFinish;

                EditorUtility.SetDirty(navSurface);

                if (!Application.IsPlaying(navSurface))
                {
                    navSurface.UpdateInternalData(navSurface.BakeJob.navSegments, navSurface.BakeJob.bounds);
                }

                serializedObject.Update();
                SceneView.RepaintAll();
            }
        }

        private bool BakedDataSanityCheck()
        {
            for (int i = 0; i < spNavSegments.arraySize; i++)
            {
                var seg = navSurface.GetSegment(i);
                if (seg.Owner != navSurface)
                    return false;
            }
            return true;
        }
    }
}
