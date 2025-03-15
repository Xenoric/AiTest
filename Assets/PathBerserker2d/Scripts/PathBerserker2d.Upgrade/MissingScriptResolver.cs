using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;

namespace Assets.PathBerserker2d.Scripts.PathBerserker2d.Upgrade
{
    class MissingScriptResolver
    {
        string navAgentFI;
        string navSurfaceFI;
        string navSegmentSubstractorFI;
        string navAreaMarkerFI;
        string dynamicObstacleFI;

        public static void UpdateReferences()
        {
            string dllGuid = "45d3c5b18a3fb854b94b339e477774af";

            int navAgentFI = -1018851484;
            string navAgentGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/NavAgent/NavAgent.cs");

            int navSurfaceFI = -567900050;
            string navSurfaceGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/NavSurface/NavSurface.cs");

            int navLinkFI = -546232842;
            string navLinkGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/NavObjects/NavLink.cs");

            int navLinkClusterFI = 1837436107;
            string navLinkClusterGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/NavObjects/NavLinkCluster.cs");

            int navSegmentSubstractorFI = -274983532;
            string navSegmentSubstractorGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/NavObjects/NavSegmentSubstractor.cs");

            int navAreaMarkerFI = 709968320;
            string navAreaMarkerGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/NavObjects/NavAreaMarker.cs");

            int dynamicObstacleFI = -721922897;
            string dynamicObstacleGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/NavObjects/DynamicObstacle.cs");

            int pathBerserker2dSettingsFI = -1515731982;
            string pathBerserker2dSettingsGUID = AssetDatabase.AssetPathToGUID("Assets/PathBerserker2d/Scripts/PathBerserker2d/PathBerserker2dSettings.cs");

            int[] fis = new int[] {
                navAgentFI,
                navSurfaceFI,
                navSegmentSubstractorFI,
                navAreaMarkerFI,
                dynamicObstacleFI,
                pathBerserker2dSettingsFI,
                navLinkFI,
                navLinkClusterFI
            };

            string[] guids = new string[] {
                navAgentGUID,
                navSurfaceGUID,
                navSegmentSubstractorGUID,
                navAreaMarkerGUID,
                dynamicObstacleGUID,
                pathBerserker2dSettingsGUID,
                navLinkGUID,
                navLinkClusterGUID
            };

            for (int i = 0; i < guids.Length; i++)
            {
                if (guids[i] == null)
                {
                    Debug.LogError("One or multiple cs files could not be found. Aborting upgrade. Please make sure that the Plugin files are in Assets/PathBerserker2d");
                    return;
                }
            }
            // first patch settings
            foreach (var metaFile in Directory.EnumerateFiles(System.IO.Path.Combine(Application.dataPath, "PathBerserker2d/Resources/"), "*.asset", SearchOption.AllDirectories))
            {
                FixFile(metaFile, fis, dllGuid, guids);
            }

            // patch everything else
            foreach (var metaFile in Directory.EnumerateFiles(Application.dataPath, "*", SearchOption.AllDirectories).Where(f => f.EndsWith(".unity") || f.EndsWith(".prefab")))
            {
                FixFile(metaFile, fis, dllGuid, guids);
            }

            Debug.Log("Finished!");
        }

        private static void FixFile(string path, int[] fis, string dllGuid, string[] guids)
        {
            try
            {
                FileInfo file = new FileInfo(path);
                bool isHidden = (file.Attributes & FileAttributes.Hidden) != 0;
                file.Attributes &= ~FileAttributes.Hidden;

                string prevText = File.ReadAllText(path);
                string text = prevText;

                for (int i = 0; i < fis.Length; i++)
                {
                    text = text.Replace($"fileID: {fis[i]}, guid: {dllGuid}",
                        $"fileID: 11500000, guid: {guids[i]}");
                }

                File.WriteAllText(path, text);
                if (isHidden)
                    file.Attributes |= FileAttributes.Hidden;

                if (prevText != text)
                {
                    Debug.Log("Updated " + path);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.LogError(e);
            }
            catch (IOException e)
            {
                Debug.LogError(e);
            }
        }
    }
}

