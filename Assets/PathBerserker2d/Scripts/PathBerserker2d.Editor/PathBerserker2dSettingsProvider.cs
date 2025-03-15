using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace PathBerserker2d
{
    [InitializeOnLoad]
    internal class PathBerserker2dSettingsProvider : SettingsProvider
    {
        static int settingsLoadTryCount = 0;

        static PathBerserker2dSettingsProvider()
        {
            TryLoadSettings();
            if (PathBerserker2dSettings.instance == null)
            {
                EditorApplication.update += RetryLoadSettings;
            }
        }

        static void RetryLoadSettings()
        {
            TryLoadSettings();
            if (PathBerserker2dSettings.instance != null)
            {
                EditorApplication.update -= RetryLoadSettings;
                settingsLoadTryCount = 0;
            }
        }

        static void TryLoadSettings()
        {
            // ensure, that a settings object exists
            // otherwise create one
            PathBerserker2dSettings.instance = Resources.Load<PathBerserker2dSettings>(PathBerserker2dSettings.GlobalSettingsFile);
            if (PathBerserker2dSettings.instance == null)
            {
                // security check
                if (System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "PathBerserker2d/Resources/", PathBerserker2dSettings.GlobalSettingsFile + ".asset")))
                {
#if DEBUG
                    Debug.Log("Couldn't load settings file, but it does exist.");
#endif
                    settingsLoadTryCount++;
                    if (settingsLoadTryCount < 3)
                        return;

                    string settingsPath = System.IO.Path.Combine("PathBerserker2d/Resources/", PathBerserker2dSettings.GlobalSettingsFile + ".asset");
                    if (!EditorUtility.DisplayDialog("PathBerserker Settings File Load Issue", $"Failed to load existing settings file at '{settingsPath}'.", "Retry", "Replace")) {
                        CreateNewSettingsFile();
                    }
                }
                else
                {
                    CreateNewSettingsFile();
                }
            }
        }

        static void CreateNewSettingsFile() {
            Debug.Log("Found no existing settings file. Creating a new one.");
            // couldn't load settings file
            // need to create a new one

            PathBerserker2dSettings.instance = ScriptableObject.CreateInstance<PathBerserker2dSettings>();
            PathBerserker2dSettings.instance.OnValidate();

            AssetDatabase.CreateAsset(PathBerserker2dSettings.instance, System.IO.Path.Combine(PathBerserker2dSettings.GlobalSettingsFolder, PathBerserker2dSettings.GlobalSettingsFile) + ".asset");
            AssetDatabase.SaveAssets();
        }

        public const string WindowPath = "Project/PathBerserker2d";


        private SerializedObject globalSettings;
        private SerializedProperty spNavLinkTypeNames;
        private SerializedProperty spDrawUnselectedLinks;
        private SerializedProperty spDrawUnselectedSurfaces;
        private SerializedProperty spDrawUnselectedSubstractors;
        private SerializedProperty spPointMappingDistance;
        private SerializedProperty spNavSegmentTags;
        private SerializedProperty spDrawGraphWhilePlaying;
        private SerializedProperty spClosestToSegmentMaxDistance;
        private SerializedProperty spPathfinderThreadCount;
        private SerializedProperty spInitiateUpdateInterval;
        private SerializedProperty spNavSurfaceLineWidth;
        private SerializedProperty spNavAreaMarkerLineWidth;
        private SerializedProperty spDrawUnselectedAreaMarkers;
        private SerializedProperty spUsePolygonCollider2dPathsForBaking;
        private ReorderableList linkTypeList;
        private ReorderableList navSegmentTags;

        public PathBerserker2dSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }



        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            EnsureSettingsFileIsPresentOnDisk();

            globalSettings = new SerializedObject(PathBerserker2dSettings.instance);
            spNavLinkTypeNames = globalSettings.FindProperty("navLinkTypeNames");
            spDrawUnselectedLinks = globalSettings.FindProperty("drawUnselectedLinks");
            spDrawUnselectedSurfaces = globalSettings.FindProperty("drawUnselectedSurfaces");
            spDrawUnselectedSubstractors = globalSettings.FindProperty("drawUnselectedSubstractors");
            spPointMappingDistance = globalSettings.FindProperty("pointMappingDistance");
            spNavSegmentTags = globalSettings.FindProperty("navTags");
            spDrawGraphWhilePlaying = globalSettings.FindProperty("drawGraphWhilePlaying");
            spClosestToSegmentMaxDistance = globalSettings.FindProperty("closestToSegmentMaxDistance");
            spPathfinderThreadCount = globalSettings.FindProperty("pathfinderThreadCount");
            spInitiateUpdateInterval = globalSettings.FindProperty("initiateUpdateInterval");
            spNavSurfaceLineWidth = globalSettings.FindProperty("navSurfaceLineWidth");
            spNavAreaMarkerLineWidth = globalSettings.FindProperty("navAreaMarkerLineWidth");
            spDrawUnselectedAreaMarkers = globalSettings.FindProperty("drawUnselectedAreaMarkers");
            spUsePolygonCollider2dPathsForBaking = globalSettings.FindProperty("usePolygonCollider2dPathsForBaking");

            linkTypeList = new ReorderableList(globalSettings, spNavLinkTypeNames, true, true, true, true);
            linkTypeList.drawHeaderCallback = DrawLinkTypeListHeader;
            linkTypeList.drawElementCallback = DrawLinkTypeListItems;
            linkTypeList.onCanRemoveCallback = CanRemoveLinkTypeListItem;

            navSegmentTags = new ReorderableList(globalSettings, spNavSegmentTags, true, true, true, true);
            navSegmentTags.drawHeaderCallback = DrawSegmentTagListHeader;
            navSegmentTags.drawElementCallback = DrawSegmentTagListItems;
            navSegmentTags.onRemoveCallback = OnRemoveNavTag;
            navSegmentTags.onCanRemoveCallback = CanRemoveTagTypeListItem;
        }

        private void EnsureSettingsFileIsPresentOnDisk()
        {
            if (PathBerserker2dSettings.instance != null && AssetDatabase.Contains(PathBerserker2dSettings.instance))
                return;

            var instance = Resources.Load<PathBerserker2dSettings>(PathBerserker2dSettings.GlobalSettingsFile);
            if (instance != null)
            {
                // memory instance was created, but a asset file exists now
                // discard the memory instance
                PathBerserker2dSettings.instance = instance;
            }
            else
            {
                var path = System.IO.Path.Combine(PathBerserker2dSettings.GlobalSettingsFolder, PathBerserker2dSettings.GlobalSettingsFile) + ".asset";
                AssetDatabase.CreateAsset(PathBerserker2dSettings.instance, path);
            }
        }

        public override void OnGUI(string searchContext)
        {
            // Use IMGUI to display UI:
            EditorGUI.BeginChangeCheck();

            // pathfinding
            EditorGUILayout.PropertyField(spPointMappingDistance);
            EditorGUILayout.PropertyField(spClosestToSegmentMaxDistance);

            GUIContent threadCountLabel = new GUIContent("Pathfinder Thread Count", "Amount of threads used for pathfinding. NOTE: WebGL doesn't support threads.");

            GUI.enabled = EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL;
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
                EditorGUILayout.IntField(threadCountLabel, 1);
            else
                EditorGUILayout.PropertyField(spPathfinderThreadCount, threadCountLabel);

            GUI.enabled = true;
            EditorGUILayout.PropertyField(spInitiateUpdateInterval);
            EditorGUILayout.PropertyField(spUsePolygonCollider2dPathsForBaking);

            // nav links
            linkTypeList.DoLayoutList();


            // nav segments
            navSegmentTags.DoLayoutList();

            // visualization
            EditorGUILayout.PropertyField(spDrawUnselectedLinks);
            EditorGUILayout.PropertyField(spDrawUnselectedSurfaces);
            EditorGUILayout.PropertyField(spDrawUnselectedSubstractors);
            EditorGUILayout.PropertyField(spDrawUnselectedAreaMarkers);
            EditorGUILayout.PropertyField(spDrawGraphWhilePlaying);
            EditorGUILayout.PropertyField(spNavSurfaceLineWidth);
            EditorGUILayout.PropertyField(spNavAreaMarkerLineWidth);

            if (EditorGUI.EndChangeCheck())
                globalSettings.ApplyModifiedProperties();
        }

        // Register the SettingsProvider
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            var provider = new PathBerserker2dSettingsProvider(WindowPath, SettingsScope.Project);

            provider.keywords = new string[] {
                "NavLinkTypeNames"

            };
            return provider;
        }

        private void DrawLinkTypeListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Link Types");
        }

        private void DrawLinkTypeListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty linkType = linkTypeList.serializedProperty.GetArrayElementAtIndex(index);
            GUI.enabled = index >= PathBerserker2dSettings.buildinNavLinkTypeNames.Length;

            float orgWidth = rect.width;
            rect.width *= 0.7f;

            EditorGUI.PropertyField(rect, linkType, new GUIContent("Type " + index));
            GUI.enabled = true;

            rect.x += rect.width + 5;
            rect.width = orgWidth - rect.width - 5;
            PathBerserker2dSettings.NavLinkTypeColors[index] = EditorGUI.ColorField(rect, PathBerserker2dSettings.NavLinkTypeColors[index]);
        }

        private bool CanRemoveLinkTypeListItem(ReorderableList list)
        {
            return list.index >= PathBerserker2dSettings.buildinNavLinkTypeNames.Length;
        }

        private bool CanRemoveTagTypeListItem(ReorderableList list)
        {
            return list.index > 0;
        }

        private void DrawSegmentTagListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Nav Tags");
        }

        private void DrawSegmentTagListItems(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty linkType = navSegmentTags.serializedProperty.GetArrayElementAtIndex(index);

            float orgWidth = rect.width;
            rect.width *= 0.7f;
            EditorGUI.PropertyField(rect, linkType, new GUIContent("Tag " + index));

            rect.x += rect.width + 5;
            rect.width = orgWidth - rect.width - 5;
            PathBerserker2dSettings.NavTagColors[index] = EditorGUI.ColorField(rect, PathBerserker2dSettings.NavTagColors[index]);
        }

        private void OnRemoveNavTag(ReorderableList list)
        {
            list.serializedProperty.DeleteArrayElementAtIndex(list.index);
        }
    }
}