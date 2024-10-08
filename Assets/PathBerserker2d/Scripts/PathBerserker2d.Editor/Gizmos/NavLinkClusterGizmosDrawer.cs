using UnityEditor;
using UnityEngine;
using static PathBerserker2d.NavLinkCluster;

namespace PathBerserker2d
{
    internal class NavLinkClusterGizmosDrawer
    {
        private static Color[] lineTraversalColors = new Color[] { Color.red, Color.green, Color.blue };

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmos(NavLinkCluster src, GizmoType gizmoType)
        {
            if ((!PathBerserker2dSettings.DrawUnselectedLinks || (gizmoType & GizmoType.Selected) != 0))
            {
                Gizmos.DrawIcon(src.transform.position, "PathBerserker2D/link_icon.png");
            }
            if ((gizmoType & GizmoType.Selected) != 0 || (PathBerserker2dSettings.DrawUnselectedLinks &&
                !Application.IsPlaying(src)))
                Draw(src);
        }

        public static void Draw(NavLinkCluster link)
        {
            var m = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.Translate(new Vector3(0, 0, link.transform.position.z));

            Gizmos.color = Color.green;
            GizmosDrawingExtensions.DrawCircle(link.transform.position);
            Gizmos.color = Color.white;

            foreach (var points in link.LinkPoints)
            {
                Vector2 worldPoint = link.transform.TransformPoint(points.point);
                Gizmos.color = PathBerserker2dSettings.NavLinkTypeColors[link.LinkType];
                Gizmos.DrawLine((Vector2)link.transform.position, worldPoint);
                Vector2 dir = ((Vector2)link.transform.position - worldPoint).normalized;

                Gizmos.color = lineTraversalColors[(int)points.traversalType];
                if (points.traversalType == PointTraversalType.Entry || points.traversalType == PointTraversalType.Both)
                {
                    GizmosDrawingExtensions.DrawArrowHead(worldPoint, dir, 0.2f);
                    if (points.traversalType == PointTraversalType.Exit || points.traversalType == PointTraversalType.Both)
                        GizmosDrawingExtensions.DrawArrowHead(worldPoint + dir * 0.2f, -dir, 0.2f);
                }
                else if (points.traversalType == PointTraversalType.Exit || points.traversalType == PointTraversalType.Both)
                    GizmosDrawingExtensions.DrawArrowHead(worldPoint, -dir, 0.2f);
            }

            Gizmos.matrix = m;
            if (link.LinkTypeName == "climb")
            {
                Vector3 pos = link.gameObject.transform.position;
                Vector3 dir = link.gameObject.transform.up;
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(pos - dir * 0.5f * 2, pos + dir * 0.5f * 2);
            }
        }
    }
}
