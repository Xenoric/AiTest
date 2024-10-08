using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    internal class PBWorldDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmos(PBWorld src, GizmoType gizmoType)
        {
            if (PathBerserker2dSettings.DrawGraphWhilePlaying && PBWorld.NavGraph != null)
            {
                NavGraphDrawer.Draw(PBWorld.NavGraph);
            }
        }
    }
}
