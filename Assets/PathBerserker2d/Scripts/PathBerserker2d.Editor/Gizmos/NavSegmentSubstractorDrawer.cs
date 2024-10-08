using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    internal static class NavSegmentSubstractorDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawGizmos(NavSegmentSubstractor src, GizmoType gizmoType)
        {
            if ((gizmoType & GizmoType.Selected) != 0 || PathBerserker2dSettings.DrawUnselectedSubstractors)
            {
                Gizmos.color = Color.red;

                var rT = src.GetComponent<RectTransform>();
                var r = rT.rect;
                Vector2 scaleFactor = rT.lossyScale * r.size * 0.5f;
                Vector2 center = r.center;

                r.min = center - scaleFactor + (Vector2)rT.position;
                r.max = center + scaleFactor + (Vector2)rT.position;

                GizmosDrawingExtensions.DrawRect(r);
                Gizmos.DrawLine(r.max, r.min);
            }
        }
    }
}
