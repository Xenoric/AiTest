using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathBerserker2d
{
    internal class ColliderConverter
    {
        public Polygon[] Convert(Collider2D collider)
        {
            Polygon[] cachedPolys;
            bool hasRigidbody = collider.GetComponentInParent<Rigidbody2D>();

            if (collider is PolygonCollider2D && PathBerserker2dSettings.UsePolygonCollider2dPathsForBaking)
            {
                cachedPolys = PolygonColliderToPolygon((PolygonCollider2D)collider);
            }
            else
            {
                var mesh = collider.CreateMesh(hasRigidbody, hasRigidbody);
                if (mesh == null)
                {
                    if (collider.GetType() == typeof(EdgeCollider2D))
                    {
                        //special handling for an open edge collider
                        cachedPolys = EdgeColliderToPolygon(collider as EdgeCollider2D);
                    }
                    else
                    {
                        Debug.LogWarning(string.Format("Unity's 'CreateMesh' returned null for Collider {0}({1}). Will ignore this collider.", collider.name, collider.GetType()));

                        cachedPolys = new Polygon[0];
                    }
                }
                else
                {
                    cachedPolys = MeshToPolygons(mesh);
                }
            }
            return cachedPolys;
        }

        private static Polygon[] EdgeColliderToPolygon(EdgeCollider2D edgeCollider2D)
        {
            Matrix4x4 localToWorld = edgeCollider2D.transform.localToWorldMatrix * Matrix4x4.Translate(edgeCollider2D.offset);
            return new Polygon[1] {
                new Polygon(
                    new Contour(
                        Array.ConvertAll(edgeCollider2D.points, (v) => (Vector2)localToWorld.MultiplyPoint3x4(v)),
                        false))
            };
        }

        private static Polygon[] MeshToPolygons(Mesh mesh)
        {
            // 1. find connected regions of mesh
            var verts = mesh.vertices;
            var triangles = mesh.triangles;

            MonoBehaviour.DestroyImmediate(mesh);

            List<int>[] graph = new List<int>[verts.Length];
            for (int i = 0; i < graph.Length; i++)
                graph[i] = new List<int>(3);

            for (int iTris = 0; iTris < triangles.Length; iTris += 3)
            {
                int a = triangles[iTris];
                int b = triangles[iTris + 1];
                int c = triangles[iTris + 2];

                AddEdgeToGraph(graph, a, b);
                AddEdgeToGraph(graph, b, c);
                AddEdgeToGraph(graph, c, a);
            }

            int nextRegion = 1;
            int[] regionMap = new int[verts.Length];
            for (int iVert = 0; iVert < verts.Length; iVert++)
            {
                if (regionMap[iVert] == 0)
                {
                    Flood(nextRegion++, regionMap, graph, iVert);
                }
            }

            StableConnector<int>[] connectors = new StableConnector<int>[nextRegion - 1];
            for (int i = 0; i < connectors.Length; i++)
                connectors[i] = new StableConnector<int>();

            for (int iVert = 0; iVert < verts.Length; iVert++)
            {
                var connections = graph[iVert];
                foreach (var conn in connections)
                {
                    var other = graph[conn];
                    if (!other.Contains(iVert))
                    {
                        connectors[regionMap[iVert] - 1].Add(iVert, conn);
                    }
                }
            }

            List<Polygon> polygons = new List<Polygon>(connectors.Length);
            for (int i = 0; i < connectors.Length; i++)
            {
                ConnectorToContours(connectors[i], verts, ref polygons);
            }
            // some polygons are contained by other polygons, make a check
            for (int iPoly = 0; iPoly < polygons.Count; iPoly++)
            {
                var poly = polygons[iPoly];
                for (int iOtherPoly = iPoly + 1; iOtherPoly < polygons.Count; iOtherPoly++)
                {
                    if (poly.Contains(polygons[iOtherPoly]))
                    {
                        poly.AddAsChild(polygons[iOtherPoly]);
                        polygons.RemoveAt(iOtherPoly);
                        iOtherPoly--;
                    }
                }
            }
            return polygons.ToArray();
        }

        private static Polygon[] PolygonColliderToPolygon(PolygonCollider2D collider)
        {
            if (collider.pathCount == 0)
                return new Polygon[0];

            var path = collider.GetPath(0);
            if (path.Length < 3)
                return new Polygon[0];

            var worldPath = path.Select(vert => (Vector2)collider.transform.TransformPoint(vert)).Reverse();
            return  new Polygon[] {
                new Polygon(new Contour(worldPath))
            };
        }

        private static void AddEdgeToGraph(List<int>[] graph, int a, int b)
        {
            graph[a].Add(b);
        }

        private static void Flood(int id, int[] regionMap, List<int>[] graph, int start)
        {
            regionMap[start] = id;

            Stack<int> indiciesToProcess = new Stack<int>(10);

            indiciesToProcess.Push(start);

            while (indiciesToProcess.Count > 0)
            {
                int i = indiciesToProcess.Pop();

                var connections = graph[i];
                foreach (var conn in connections)
                {
                    if (regionMap[conn] != id)
                    {
                        regionMap[conn] = id;
                        indiciesToProcess.Push(conn);
                    }
                }
            }
        }

        private static void ConnectorToContours(StableConnector<int> connector, Vector3[] verts, ref List<Polygon> polygons)
        {
            List<Contour> holes = new List<Contour>(1);
            List<Tuple<double, Polygon>> hulls = new List<Tuple<double, Polygon>>(1);
            foreach (var chain in connector.closedPolygons)
            {
                List<Vector2> points = new List<Vector2>(chain.points.Count);
                foreach (var p in chain.points)
                {
                    points.Add(verts[p]);
                }

                // simplification moved to later step in pipeline
                //points = ExtendedGeometry.SimplifyContour(points, 0.1f);
                /*
                for (int i = 0; i < points.Count; i++)
                {
                    points[i] = new Vector2((float)Math.Round(points[i].x, 3),
                        (float)Math.Round(points[i].y, 3)); ;
                }*/

                Contour c = new Contour(points);
                double area = c.SignedArea();
                if (area > 0)
                    hulls.Add(new Tuple<double, Polygon>(area, new Polygon(c)));
                else
                    holes.Add(c);
            }
            if (hulls.Count == 1)
            {
                hulls[0].Item2.Holes = holes;
                polygons.Add(hulls[0].Item2);
            }
            else
            {
                hulls.Sort((a, b) => b.Item1.CompareTo(a.Item1));

                for (int iHull = 0; iHull < hulls.Count; iHull++)
                {
                    for (int iHull2 = iHull + 1; iHull2 < hulls.Count; iHull2++)
                    {
                        if (hulls[iHull].Item2.Contains(hulls[iHull2].Item2))
                        {
                            hulls[iHull].Item2.AddAsChild(hulls[iHull2].Item2);
                            hulls.RemoveAt(iHull2);
                            iHull2--;
                        }
                    }
                }
                foreach (var hull in hulls)
                {
                    for (int iHole = 0; iHole < holes.Count; iHole++)
                    {
                        if (hull.Item2.Contains(holes[iHole]))
                        {
                            hull.Item2.Holes.Add(holes[iHole]);
                            holes.RemoveAt(iHole);
                            iHole--;
                        }
                    }
                }
                polygons.AddRange(hulls.ConvertAll(t => t.Item2));
            }
        }
    }
}
