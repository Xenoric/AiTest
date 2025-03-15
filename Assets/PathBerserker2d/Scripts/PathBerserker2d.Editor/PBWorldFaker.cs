using UnityEngine;
using UnityEditor;

namespace PathBerserker2d
{
    [InitializeOnLoad]
    class PBWorldFaker
    {
        // register an event handler when the class is initialized
        static PBWorldFaker()
        {
            EditorApplication.playModeStateChanged += LogPlayModeState;
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
                PBWorld.NavGraph = new NavGraph(1);
        }

        private static void LogPlayModeState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
                PBWorld.NavGraph = new NavGraph(1);
        }
    }
}
