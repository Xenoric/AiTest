using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    class MiterLineMeshCreator
    {
        private List<Vector3> verts = new List<Vector3>();
        private List<Color32> colors = new List<Color32>();
        private List<int> tris = new List<int>();

        public void CreateLine(Mesh mesh, IList<Vector2> linePoints, float thickness, Color line, Color32 corner, Color endpoint)
        {
            verts.Clear();
            colors.Clear();
            tris.Clear();

            verts.Capacity = Math.Max(verts.Capacity, linePoints.Count * 2);
            colors.Capacity = Math.Max(colors.Capacity, verts.Count);
            tris.Capacity = Math.Max(tris.Capacity, linePoints.Count * 4);

            Vector2 curTangent = (linePoints[0] - linePoints[1]).normalized;
            Vector2 curNormal = new Vector2(-curTangent.y, curTangent.x);

            Vector2 prevOffset;
            int prevMult = 1;
            bool looped = linePoints[0] == linePoints[linePoints.Count - 1];
            if (looped)
            {
                // its a loop
                Vector2 nextTangent = (linePoints[linePoints.Count - 2] - linePoints[linePoints.Count - 1]).normalized;
                Vector2 nextNormal = new Vector2(-nextTangent.y, nextTangent.x);
                var tangent2 = (nextTangent + curTangent).normalized;
                var miter2 = new Vector2(-tangent2.y, tangent2.x);
                float length2 = thickness / Vector2.Dot(nextNormal, miter2);

                prevOffset = length2 * miter2;
                prevMult = Math.Sign(Vector2.SignedAngle(nextTangent, curTangent)) * -1;
            }
            else
            {
                prevOffset = curNormal * thickness;
            }

            int end = looped ? linePoints.Count - 1 : linePoints.Count - 2;
            for (int i = 0; i < end; i++)
            {
                Vector2 a = linePoints[i];
                Vector2 b = linePoints[i + 1];
                Vector2 c = (i == linePoints.Count - 2) ? linePoints[1] : linePoints[i + 2];

                Vector2 nextTangent = (b - c).normalized;
                Vector2 nextNormal = new Vector2(-nextTangent.y, nextTangent.x);

                var tangent2 = (nextTangent + curTangent).normalized;
                var miter2 = new Vector2(-tangent2.y, tangent2.x);
                float length2 = thickness / Vector2.Dot(curNormal, miter2);

                Vector2 offset2 = length2 * miter2;

                verts.Add(a + prevOffset * prevMult);
                verts.Add((a + prevOffset * prevMult) - curNormal * thickness * 2 * prevMult);

                int nextMult;
                if (Vector2.SignedAngle(curTangent, nextTangent) > 0)
                {
                    if (prevMult == -1)
                    {
                        verts.Add(b - offset2);
                        verts.Add((b - offset2) + curNormal * thickness * 2);
                    }
                    else
                    {
                        verts.Add((b - offset2) + curNormal * thickness * 2);
                        verts.Add(b - offset2);
                    }

                    nextMult = -1;
                }
                else
                {
                    if (prevMult == -1)
                    {
                        verts.Add((b + offset2) - curNormal * thickness * 2);
                        verts.Add(b + offset2);
                    }
                    else
                    {
                        verts.Add(b + offset2);
                        verts.Add((b + offset2) - curNormal * thickness * 2);
                    }

                    nextMult = 1;
                }

                AddLineTriangles(prevMult, line);
                AddTriangleConnector(curNormal, nextNormal, thickness, b, offset2, corner);

                prevOffset = offset2;
                curTangent = nextTangent;
                curNormal = nextNormal;
                prevMult = nextMult;
            }

            if (!looped)
            {
                verts.Add(linePoints[linePoints.Count - 2] + prevOffset * prevMult);
                verts.Add((linePoints[linePoints.Count - 2] + prevOffset * prevMult) - curNormal * thickness * 2 * prevMult);

                if (prevMult == -1)
                {
                    verts.Add(linePoints[linePoints.Count - 1] - curNormal * thickness);
                    verts.Add(linePoints[linePoints.Count - 1] + curNormal * thickness);
                }
                else
                {
                    verts.Add(linePoints[linePoints.Count - 1] + curNormal * thickness);
                    verts.Add(linePoints[linePoints.Count - 1] - curNormal * thickness);
                }
                AddLineTriangles(prevMult, line);

                AddLineEndMarker(linePoints[0], (linePoints[1] - linePoints[0]).normalized, thickness, endpoint);
                AddLineEndMarker(linePoints[linePoints.Count -1], (linePoints[linePoints.Count - 2] - linePoints[linePoints.Count - 1]).normalized, thickness, endpoint);
            }

            mesh.triangles = new int[] { };
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
            mesh.colors32 = colors.ToArray();
        }

        private void AddTriangleConnector(Vector3 curNormal, Vector3 nextNormal, float thickness, Vector3 point, Vector3 miterOffset, Color32 corner)
        {
            colors.Add(corner);
            colors.Add(corner);
            colors.Add(corner);

            if (Vector2.SignedAngle(curNormal, nextNormal) > 0)
            {
                verts.Add((point - miterOffset) + nextNormal * thickness * 2);
                verts.Add(point - miterOffset);
                verts.Add((point - miterOffset) + curNormal * thickness * 2);

                tris.Add(verts.Count - 3);
                tris.Add(verts.Count - 1);
                tris.Add(verts.Count - 2);
            }
            else
            {
                verts.Add((point + miterOffset) - nextNormal * thickness * 2);
                verts.Add(point + miterOffset);
                verts.Add((point + miterOffset) - curNormal * thickness * 2);

                tris.Add(verts.Count - 2);
                tris.Add(verts.Count - 1);
                tris.Add(verts.Count - 3);
            }
        }

        private void AddLineTriangles(int prevMult, Color32 line)
        {
            if (prevMult == -1)
            {
                tris.Add(colors.Count + 3);
                tris.Add(colors.Count + 1);
                tris.Add(colors.Count + 0);

                tris.Add(colors.Count + 0);
                tris.Add(colors.Count + 2);
                tris.Add(colors.Count + 3);
            }
            else
            {
                tris.Add(colors.Count + 0);
                tris.Add(colors.Count + 1);
                tris.Add(colors.Count + 3);

                tris.Add(colors.Count + 3);
                tris.Add(colors.Count + 2);
                tris.Add(colors.Count + 0);
            }

            colors.Add(line);
            colors.Add(line);
            colors.Add(line);
            colors.Add(line);
        }

        private void AddLineEndMarker(Vector3 lineEnd, Vector3 tangent, float lineThickness, Color lineEndpointColor)
        {
            Vector3 zOffset = new Vector3(0, 0, -0.01f);
            Vector3 normal = new Vector3(-tangent.y, tangent.x);
            float increasedThickness = lineThickness * 1.25f * 2;
            int index = verts.Count;
            verts.Add(lineEnd + normal * increasedThickness + zOffset);
            verts.Add(lineEnd - normal * increasedThickness + zOffset);

            Vector3 inset = tangent * increasedThickness;
            verts.Add(lineEnd + inset + normal * increasedThickness + zOffset);
            verts.Add(lineEnd + inset - normal * increasedThickness + zOffset);

            colors.Add(lineEndpointColor);
            colors.Add(lineEndpointColor);
            colors.Add(lineEndpointColor);
            colors.Add(lineEndpointColor);

            tris.Add(index + 1);
            tris.Add(index);
            tris.Add(index + 2);

            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 1);
        }
    }
}
