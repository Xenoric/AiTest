using UnityEditor;
using UnityEngine;
using static PathBerserker2d.NavLink;

namespace PathBerserker2d
{
    [InitializeOnLoad]
    internal static class NavLinkGizmosDrawer
    {
        static string linkFileName = "Assets/PathBerserker2d/Icons/link_icon.png";
        static Texture2D linkTexture;

        static NavLinkGizmosDrawer()
        {
            linkTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(linkFileName);
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        static void DrawGizmos(NavLink src, GizmoType gizmoType)
        {
            /*
            if ((gizmoType & GizmoType.Selected) != 0 || PathBerserker2dSettings.DrawUnselectedLinks)
            {
                if (src.CurrentVisualizationType == NavLink.VisualizationType.Teleport)
                {
                    Gizmos.DrawIcon(src.StartWorldPosition, "PathBerserker2d/Gizmos/portal.png");
                    Gizmos.DrawIcon(src.GoalWorldPosition, "PathBerserker2D/portal.png");
                }
                else
                {
                    //Gizmos.DrawIcon((src.GoalWorldPosition - src.StartWorldPosition) * 0.5f + src.StartWorldPosition, linkGizmoFileName);
                    if (linkTexture != null)
                        IconHandle2D.DrawHandle((src.GoalWorldPosition - src.StartWorldPosition) * 0.5f + src.StartWorldPosition, linkTexture, 0.5f, src);
                }
            }
            */
            bool isSelected = (gizmoType & GizmoType.Selected) != 0;
            if (isSelected || (PathBerserker2dSettings.DrawUnselectedLinks &&
                !Application.IsPlaying(src)))
                Draw(src, isSelected);
        }

        public static void Draw(NavLink link, bool isSelected)
        {
            var m = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.Translate(new Vector3(0, 0, link.transform.position.z));
            Gizmos.color = PathBerserker2dSettings.GetLinkTypeColor(link.LinkType);
            switch (link.CurrentVisualizationType)
            {
                case VisualizationType.Linear:
                    Vector2 dir = (link.GoalWorldPosition - link.StartWorldPosition).normalized;
                    if (link.IsBidirectional)
                    {
                        GizmosDrawingExtensions.DrawArrowHead(link.StartWorldPosition + dir * 0.2f, -dir, 0.2f);
                    }
                    else
                    {
                        Vector2 normal = new Vector2(-dir.y, dir.x) * 0.3f;
                        Gizmos.DrawLine(link.StartWorldPosition + normal, link.StartWorldPosition - normal);
                    }
                    Gizmos.DrawLine(link.StartWorldPosition, link.GoalWorldPosition);
                    GizmosDrawingExtensions.DrawArrowHead(link.GoalWorldPosition - dir * 0.2f, dir, 0.2f);

                    if (isSelected)
                    {
                        Vector2 offset = Quaternion.Euler(0, 0, link.TraversalAngle) * Vector3.up * link.Clearance;

                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(link.StartWorldPosition + offset, link.GoalWorldPosition + offset);

                        float length = (link.GoalWorldPosition - link.StartWorldPosition).magnitude;
                        Gizmos.DrawLine(link.StartWorldPosition, link.StartWorldPosition + offset);
                        for (float t = 2; t <= length - 2; t += 2)
                        {
                            Gizmos.DrawLine(link.StartWorldPosition + dir * t, link.StartWorldPosition + offset + dir * t);
                        }
                        Gizmos.DrawLine(link.GoalWorldPosition, link.GoalWorldPosition + offset);
                    }

                    break;
                case VisualizationType.QuadradticBezier:
                    GizmosDrawingExtensions.DrawBezierConnection(
                        link.StartWorldPosition,
                        link.GoalWorldPosition,
                        link.transform.TransformPoint(link.BezierControlPoint),
                        link.IsBidirectional);

                    if (isSelected)
                    {
                        Vector2 offset = Quaternion.Euler(0, 0, link.TraversalAngle) * Vector3.up * link.Clearance;

                        Gizmos.color = Color.green;
                        GizmosDrawingExtensions.DrawBezierConnectionWithOffset(
                            link.StartWorldPosition,
                            link.GoalWorldPosition,
                            (Vector2)link.transform.TransformPoint(link.BezierControlPoint),
                            offset);
                    }
                    break;
                case VisualizationType.Projectile:
                    GizmosDrawingExtensions.DrawProjectileArc(link.StartWorldPosition, link.GoalWorldPosition, link.HorizontalSpeed, link.IsBidirectional);
                    if (isSelected)
                    {
                        Vector2 offset = Quaternion.Euler(0, 0, link.TraversalAngle) * Vector3.up * link.Clearance;

                        Gizmos.color = Color.green;
                        GizmosDrawingExtensions.DrawProjectileArcWithOffset(
                            link.StartWorldPosition, link.GoalWorldPosition, link.HorizontalSpeed,
                            offset);
                    }
                    break;
                case VisualizationType.Teleport:
                    break;
                case VisualizationType.TransformBasedMovement:
                    GizmosDrawingExtensions.DrawJumpArc(link.StartWorldPosition, link.GoalWorldPosition, link.HorizontalSpeed, link.IsBidirectional);
                    break;
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
