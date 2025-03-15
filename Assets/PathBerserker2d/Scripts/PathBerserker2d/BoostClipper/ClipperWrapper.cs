using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PathBerserker2d
{
    class ClipperWrapper : IClipper
    {
        const int FloatToIntMult = 10000;
        const float IntToFloatDiv = 10000;

        Clipper clipper = new Clipper();

        public ResultType Compute(Polygon sp, Polygon cp, BoolOpType op, out List<Polygon> result, bool includeOpenPolygons = false)
        {
            result = new List<Polygon>();

            if (!sp.BoundsOverlap(cp))
            {
                return ResultType.NoOverlap;
            }

            AddPolygonToClipper(sp, PolyType.ptSubject);
            AddPolygonToClipper(cp, PolyType.ptClip);

            double prevArea = 0;
            ClipType clipType;
            switch (op)
            {
                case BoolOpType.INTERSECTION:
                    clipType = ClipType.ctIntersection;
                    break;
                case BoolOpType.UNION:
                    clipType = ClipType.ctUnion;
                    break;
                case BoolOpType.DIFFERENCE:
                    clipType = ClipType.ctDifference;
                    prevArea = sp.SignedArea();
                    break;
                default:
                    throw new ArgumentException("Unknown op type " + op);
            }

            PolyTree resultTree = new PolyTree();
            bool succeeded = clipper.Execute(clipType, resultTree);

            GetResultsFromNode(resultTree, result, includeOpenPolygons);
            foreach (var poly in result)
            {
                poly.EnsureCWOrdering();
            }

            clipper.Clear();

            bool intersectionHappened = false;
            double afterArea = 0;
            switch (op)
            {
                case BoolOpType.INTERSECTION:
                    intersectionHappened = result.Count > 0;
                    break;
                case BoolOpType.UNION:
                    if (result.Count == 1)
                        intersectionHappened = true;
                    break;
                case BoolOpType.DIFFERENCE:
                    if (result.Count > 1)
                        intersectionHappened = true;
                    else
                    {
                        foreach (var poly in result)
                            afterArea += poly.SignedArea();

                        intersectionHappened = !(Math.Abs(afterArea - prevArea) < 0.001);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown op type " + op);
            }


            return intersectionHappened ? ResultType.Clipped : ResultType.NoOverlap;
        }

        private void AddPolygonToClipper(Polygon polygon, PolyType polyType)
        {
            var points = ConvertContour(polygon.Hull);
            clipper.AddPath(points, polyType, polygon.Hull.IsClosed);

            foreach (var hole in polygon.Holes)
            {
                points = ConvertContour(hole);
                clipper.AddPath(points, polyType, hole.IsClosed);
            }
        }

        private List<IntPoint> ConvertContour(Contour contour)
        {
            List<IntPoint> points = new List<IntPoint>(contour.VertexCount);
            for (int i = 0; i < contour.VertexCount; i++)
            {
                points.Add(new IntPoint(contour.Verts[i].x * FloatToIntMult, contour.Verts[i].y * FloatToIntMult));
            }
            return points;
        }

        private void GetResultsFromNode(PolyNode node, List<Polygon> polygons, bool includeOpenPolygons)
        {
            foreach (var child in node.Childs)
            {
                if (child.IsOpen && !includeOpenPolygons)
                    continue;

                Polygon p = new Polygon(ConvertChain(child.m_polygon, !child.IsOpen));
                polygons.Add(p);
                foreach (var holeNode in child.Childs)
                {
                    var hole = ConvertChain(holeNode.m_polygon, !holeNode.IsOpen);
                    p.Holes.Add(hole);

                    GetResultsFromNode(holeNode, polygons, includeOpenPolygons);
                }
            }
        }

        private Contour ConvertChain(List<IntPoint> chain, bool closed)
        {
            return new Contour(chain.Select(ip => new Vector2(ip.X / IntToFloatDiv, ip.Y / IntToFloatDiv)), closed);
        }

    }
}
