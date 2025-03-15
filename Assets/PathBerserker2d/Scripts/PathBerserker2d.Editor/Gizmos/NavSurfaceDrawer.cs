using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    internal static class NavSurfaceDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        public static void DrawGizmos(NavSurface surface, GizmoType gizmoType)
        {
            if (surface.NavSegments != null && !Application.IsPlaying(surface) && (PathBerserker2dSettings.DrawUnselectedSurfaces || (gizmoType & GizmoType.Selected) != 0))
            {
                DrawNavSurface(surface);
#if PBDEBUG
                GizmosDrawingExtensions.DrawRect(surface.WorldBounds);
#endif
            }
        }

        private static bool[] visited;
        private static Dictionary<NavSurface, List<Mesh>> miterLinesMap;
        public static void DrawNavSurface(NavSurface surface)
        {
            if (miterLinesMap == null)
                miterLinesMap = new Dictionary<NavSurface, List<Mesh>>();

            List<Mesh> miterLines = null;
            miterLinesMap.TryGetValue(surface, out miterLines);

            if (surface.hasDataChanged || miterLines == null)
            {
                if (visited == null || visited.Length < surface.NavSegments.Count)
                {
                    visited = new bool[surface.NavSegments.Count];
                }
                else
                {
                    for (int i = 0; i < visited.Length; i++)
                    {
                        visited[i] = false;
                    }
                }

                var miterCreator = new MiterLineMeshCreator();
                bool newMiterLines = miterLines == null;
                if (newMiterLines)
                    miterLines = new List<Mesh>();

                int lineCount = 0;
                for (int i = 0; i < surface.NavSegments.Count; i++)
                {
                    if (!visited[i])
                    {
                        var points = GatherContourPoints(surface, surface.NavSegments[i], ref visited);

                        if (lineCount >= miterLines.Count)
                        {
                            miterLines.Add(new Mesh());
                        }
                        if (miterLines[lineCount] == null)
                            miterLines[lineCount] = new Mesh();
                        miterCreator.CreateLine(miterLines[lineCount], points, PathBerserker2dSettings.NavSurfaceLineWidth, new Color32(204, 65, 255, 255), new Color32(255, 178, 10, 255), new Color32(98, 81, 255, 255));
                        lineCount++;
                    }
                }

                // clean up unused meshs
                for (int i = miterLines.Count - 1; i >= lineCount; i--)
                {
                    GameObject.DestroyImmediate(miterLines[i]);
                    miterLines.RemoveAt(i);
                }

                if (newMiterLines)
                    miterLinesMap.Add(surface, miterLines);
                surface.hasDataChanged = false;
            }

            SharedMaterials.UnlitVertexColorSolid.SetPass(0);
            if (miterLines.Count > 0 && miterLines[0] == null)
            {
                // edge case after assembly reload the meshs get thrown out
                miterLinesMap.Clear();
                return;
            }
            foreach (var ml in miterLines)
            {
                Graphics.DrawMeshNow(ml, surface.LocalToWorldMatrixEditor);
            }
        }

        private static List<Vector2> GatherContourPoints(NavSurface surface, NavSegment initialSeg, ref bool[] visited)
        {
            List<Vector2> contourPoints = new List<Vector2>();
            if (!initialSeg.HasPrev)
            {
                contourPoints.Add(initialSeg.Start);
            }

            contourPoints.Add(initialSeg.End);

            NavSegment seg = initialSeg;
            while (seg.HasNext && !visited[seg.NextSegmentIndex])
            {
                visited[seg.NextSegmentIndex] = true;
                seg = surface.NavSegments[seg.NextSegmentIndex];

                contourPoints.Add(seg.End);
            }


            if (initialSeg.HasPrev && !visited[initialSeg.PrevSegmentIndex])
            {
                // we might have missed segments to the left get em
                List<Vector2> leftPoints = new List<Vector2>();
                seg = initialSeg;
                while (seg.HasPrev)
                {
                    visited[seg.PrevSegmentIndex] = true;
                    seg = surface.NavSegments[seg.PrevSegmentIndex];

                    leftPoints.Add(seg.End);
                }
                leftPoints.Add(seg.Start);

                leftPoints.Reverse();
                leftPoints.AddRange(contourPoints);
                contourPoints = leftPoints;
            }

            return contourPoints;
        }
    }
}
