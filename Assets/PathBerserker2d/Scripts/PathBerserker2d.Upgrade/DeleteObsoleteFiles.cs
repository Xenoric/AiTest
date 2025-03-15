using System.IO;
using UnityEditor;
using UnityEngine;

namespace Assets.PathBerserker2d.Scripts.PathBerserker2d.Upgrade
{
    static class DeleteObsoleteFiles
    {
        static string[] obsoleteFiles = new string[] {
            "Corgi.zip",
            "Editor/PathBerserker2d-Editor.dll",
            "Editor/ShowAssetIds.cs",
            "Editor/FootStepSoundsInspector.cs",
            "Scripts/PathBerserker2d.dll",
            "Scripts/PathBerserker2d.xml",
            "Scripts/TransformBasedMovement.cs",
            "Scripts/Examples/AdjustRotation.cs",
            "Scripts/Examples/CornerRotationSkipper.cs",
            "Scripts/Examples/Elevator.cs",
            "Scripts/Examples/Follower.cs",
            "Scripts/Examples/FootStepSounds.cs",
            "Scripts/Examples/GoalWalker.cs",
            "Scripts/Examples/KeepGrounded.cs",
            "Scripts/Examples/MouseWalker.cs",
            "Scripts/Examples/MovingPlatform.cs",
            "Scripts/Examples/MultiGoalWalker.cs",
            "Scripts/Examples/PatrolWalker.cs",
            "Scripts/Examples/RandomWalker.cs",
        };

        static string[] obsoleteDirs = new string[] {
            "Editor",
            "Scripts/Examples"
        };

        public static void RemoveObsoleteFiles()
        {
            string basePath = Path.Combine(Application.dataPath, "PathBerserker2d");

            foreach (var file in obsoleteFiles)
            {
                string path = Path.Combine(basePath, file);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Debug.Log("Deleted " + path);
                }
            }

            foreach (var dir in obsoleteDirs)
            {
                string path = Path.Combine(basePath, dir);
                if (Directory.Exists(path) && !Directory.EnumerateFiles(path).GetEnumerator().MoveNext())
                {
                    Directory.Delete(path);
                }
            }

            AssetDatabase.Refresh();
        }
    }
}
