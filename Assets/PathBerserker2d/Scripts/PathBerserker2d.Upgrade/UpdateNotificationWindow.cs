using System;
using UnityEditor;
using UnityEngine;

namespace Assets.PathBerserker2d.Scripts.PathBerserker2d.Upgrade
{
    [InitializeOnLoad]
    class UpdateNotificationWindow : EditorWindow
    {
        [MenuItem("Window/General/PathBerserker Update Log")]
        static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            UpdateNotificationWindow window = (UpdateNotificationWindow)EditorWindow.GetWindow(typeof(UpdateNotificationWindow));
            window.Show();
        }

        static UpdateNotificationWindow()
        {
            EditorApplication.update += EditorUpdate;
        }

        static void EditorUpdate()
        {
            if (!EditorPrefs.GetBool("pathberserker.showedupgrader") && EditorPrefs.GetBool("pathberserker.showedupdate.1_4"))
            {
                ShowWindow();

                EditorPrefs.SetBool("pathberserker.showedupgrader", true);
            }
            EditorPrefs.SetString("pathberserker.installed_version", AssemblyInfo.Version);
            EditorApplication.update -= EditorUpdate;
        }

        GUIStyle labelStyle;
        GUIStyle headingStyle;

        private void OnEnable()
        {
            try
            {
                labelStyle = new GUIStyle(EditorStyles.label);
            }
            catch (NullReferenceException)
            {
                labelStyle = new GUIStyle();
            }

            labelStyle.normal.textColor = Color.white;
            labelStyle.richText = true;
            labelStyle.wordWrap = true;

            headingStyle = new GUIStyle(labelStyle);
            headingStyle.fontSize += 20;
        }

        public void OnGUI()
        {
            if (labelStyle == null || headingStyle == null)
                OnEnable();
            if (labelStyle == null || headingStyle == null)
                return;

            EditorGUILayout.LabelField("<color=orange>Pathberserker 2d - Upgrader</color>", headingStyle);
            EditorGUILayout.LabelField(AssemblyInfo.Version, labelStyle);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("<i>This window can be reopened at 'Window/General/PathBerserker Update Log'</i>", labelStyle);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("<size=15><color=red>Important</color></size>", labelStyle);
            EditorGUILayout.LabelField("All Dll files have been replaced with AssemblyDefinitions. <b>All references to MonoBehaviors that where previously located in a DLL are now broken!</b> This will lead to a lot of missing scripts in your project.", labelStyle);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fear not. By clicking the button below an upgrade script runs, that should restore all those references. <b>It will directly modify your projects .meta files.</b>", labelStyle);

            EditorGUILayout.Space();


            EditorGUILayout.LabelField("<b>BE WARNED, this might cause damage</b>. You should backup your project before starting this process. This will not work, if you moved the plugin-files from its default directory.", labelStyle);
            EditorGUILayout.Space();

            if (GUILayout.Button("Perform Upgrade") && EditorUtility.DisplayDialog("Have you made a backup?", "This process may fail or do unexpected things. Don't blame me if it ends up destroying something. Make a backup.", "Yes, I have a backup."))
            {
                MissingScriptResolver.UpdateReferences();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("<b>If this is your first time installing this plugin, you don't have to do anything.</b>", labelStyle);
        }
    }
}
