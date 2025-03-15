using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    internal static class NavAgentDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Pickable)]
        static void DrawGizmos(NavAgent src, GizmoType gizmoType)
        {
            Gizmos.color = Color.green;
            if (!Application.IsPlaying(src))
            {
                Vector2 adjustedPosition = src.transform.position;
                Gizmos.DrawRay(adjustedPosition, src.transform.up * src.Height);
                Gizmos.DrawLine(adjustedPosition + -(Vector2)src.transform.right * 0.2f, adjustedPosition + (Vector2)src.transform.right * 0.2f);
            }
            else if(!src.currentMappedPosition.IsInvalid())
            {
                Gizmos.color = Color.magenta;
                GizmosDrawingExtensions.DrawCircle( src.currentMappedPosition.Position);
            }

            if (src.IsFollowingAPath)
            {
                int hash = Mathf.Abs(src.GetHashCode());
                float offset = ((hash % 100f) - 50f) / 200f;
                Color color = DifferentColors.GetColor(hash);

                if (src.IsOnLink)
                {
                    DrawPath(src.Path, src.Path.Current.LinkStart, src.Height / 2f + offset, color);
                }
                else
                {
                    DrawPath(src.Path, src.transform.position, src.Height / 2f + offset, color);
                }
            }
        }

        static void DrawPath(Path path, Vector2 startPoint, float lineHeight, Color color)
        {
            Gizmos.color = color;
            var seg = path.Current;
            Vector2 lineA = startPoint + seg.Normal * lineHeight;

            while (seg != null)
            {
                Vector2 lineB = seg.LinkStart + seg.Normal * lineHeight;
                if (seg.Next == null)
                {
                    Gizmos.DrawLine(lineA, lineB);
                    lineA = lineB;

                    GizmosDrawingExtensions.DrawCircle(lineB, 0.2f);
                    GizmosDrawingExtensions.DrawCircle(lineB, 0.3f);
                }
                else
                {
                    if (seg.link.LinkType == -1)
                    {
                        Vector2 oLineA = seg.LinkEnd + seg.Next.Normal * lineHeight;
                        Vector2 oLineB = seg.Next.LinkStart + seg.Next.Normal * lineHeight;

                        // calc intersection
                        Vector2 inter;
                        if (ExtendedGeometry.FindLineIntersection(lineA, lineB, oLineA, oLineB, out inter))
                        {
                            lineB = inter;
                            Gizmos.DrawLine(lineA, lineB);
                            lineA = lineB;
                        }
                        else
                        {
                            Gizmos.DrawLine(lineB, oLineA);
                            Gizmos.DrawLine(lineA, lineB);
                            lineA = oLineA;
                        }
                    }
                    else
                    {
                        Gizmos.DrawLine(lineA, lineB);
                        lineA = lineB;

                        lineB = seg.LinkEnd + seg.Next.Normal * lineHeight;
                        Gizmos.DrawLine(lineA, lineB);
                        lineA = lineB;
                    }
                }
                seg = seg.Next;
            }
        }
    }
}
