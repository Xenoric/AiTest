using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace PathBerserker2d
{
    internal class IntersectionTester
    {
        // 1,23456 -> 1234
        private const float FUDGE_FACTOR = 0.01f;

        public IClipper Clipper { get => clipper; }

        IClipper clipper;
        ColliderConverter colliderConverter;
        INavSegmentCreationParamProvider paramProvider;
        Matrix4x4 surfaceWorldToLocal;

        public IntersectionTester(INavSegmentCreationParamProvider paramProvider, Matrix4x4 surfaceWorldToLocal)
        {
            this.clipper = new ClipperWrapper();
            this.colliderConverter = new ColliderConverter();
            this.paramProvider = paramProvider;
            this.surfaceWorldToLocal = surfaceWorldToLocal;
        }

        public Polygon[] ColliderToPolygon(Collider2D col)
        {
            return colliderConverter.Convert(col);
        }

        public void MarkContour(Contour contour, NavSurface owner, PolygonSet polySet, List<NavSegment> navSegments)
        {
            int prevSegIndex = -1;
            int firstNavSegIndex = navSegments.Count;
            bool lastSegTouchesLastPoint = false;

            for (int iPrevLine = contour.IsClosed ? contour.VertexCount - 1 : 0, iLine = contour.IsClosed ? 0 : 1; iLine < contour.VertexCount; iPrevLine = iLine, iLine++)
            {
                Vector2 a = contour[iPrevLine];
                Vector2 b = contour[iLine];

                Vector2 dir = b - a;
                float length = dir.magnitude;
                dir /= length;
                Vector2 normal = new Vector2(-dir.y, dir.x);

                int cellCount = Math.Max(1, Mathf.RoundToInt(length / paramProvider.CellSize));

                Vector2 fudgedA = a + normal * FUDGE_FACTOR;
                Vector2 fudgedB = b + normal * FUDGE_FACTOR;
                Vector2 cellHeight = normal * paramProvider.MaxClearance;

                List<Vector2> cellSpace = new List<Vector2>(4);
                cellSpace.Add(fudgedB);
                cellSpace.Add(fudgedA);
                cellSpace.Add(fudgedA + cellHeight);
                cellSpace.Add(fudgedB + cellHeight);

                Polygon walkSpacePolygon = new Polygon(new Contour(cellSpace));

                //var ts = new Polygon(new Contour(cellSpace.ToList()));
                //GizmosQueue.Instance.Enqueue(5, () =>
                //{
                //    Gizmos.color = Color.gray;
                //    ts.Draw();
                //});

                List<Polygon> intersections = new List<Polygon>(5);
                foreach (var testCandidate in polySet.Query(walkSpacePolygon.BoundingRect))
                {
                    if (testCandidate.Hull.IsClosed)
                    {
                        //GizmosQueue.Instance.Enqueue(5, () =>
                        //{
                        //    Gizmos.color = Color.red;
                        //    //testCandidate.Draw();
                        //});

                        clipper.Compute(testCandidate, walkSpacePolygon, BoolOpType.INTERSECTION, out var clipResult);
                        intersections.AddRange(clipResult);
                    }
                    else
                        intersections.Add(testCandidate);
                }
                //could do a union now; to evaluate
                // this is nonsense, but better leave as is. It ensures that all cells are the same size
                if (intersections.Count == -1)
                {
                    var newSeg = CreateFreeSegment(cellCount, owner, a, dir, length);
                    if (prevSegIndex != -1)
                    {
                        navSegments[prevSegIndex].NextSegmentIndex = navSegments.Count;
                        newSeg.PrevSegmentIndex = prevSegIndex;
                    }
                    prevSegIndex = navSegments.Count;
                    navSegments.Add(newSeg);
                    lastSegTouchesLastPoint = true;
                }
                else
                {
                    float correctedCellSize = length / cellCount;

                    Vector2 cellOffset = dir * correctedCellSize;

                    cellSpace[0] = (fudgedA + cellOffset);
                    cellSpace[1] = (fudgedA);
                    cellSpace[2] = (fudgedA + cellHeight);
                    cellSpace[3] = (cellSpace[0] + cellHeight);

                    List<float> cellClearances = new List<float>(cellCount);
                    bool searchForHead = true;
                    float segmentStart = 0;
                    for (int iCell = 0; iCell < cellCount; iCell++)
                    {

                        var poly = new Polygon(new Contour(new List<Vector2>(cellSpace)));
                        walkSpacePolygon.UpdateBounds();

                        //GizmosQueue.Instance.Enqueue(5, () => { poly.Draw(); });

                        float clearance = CalculateCellClearance(walkSpacePolygon, normal, paramProvider.MinClearance, paramProvider.MaxClearance, intersections);
                        if (searchForHead)
                        {
                            if (clearance >= paramProvider.MinClearance)
                            {
                                segmentStart = iCell * correctedCellSize;
                                searchForHead = false;
                                cellClearances.Add(clearance);
                            }
                            else
                            {
                                prevSegIndex = -1;
                            }
                        }
                        else
                        {
                            if (clearance < paramProvider.MinClearance)
                            {
                                var newSeg = new NavSegment(
                                    owner,
                                    surfaceWorldToLocal.MultiplyPoint3x4(segmentStart * dir + a),
                                    surfaceWorldToLocal.MultiplyVector(dir),
                                    iCell * correctedCellSize - segmentStart,
                                    cellClearances.ToArray()
                                );
                                if (prevSegIndex != -1)
                                {
                                    navSegments[prevSegIndex].NextSegmentIndex = navSegments.Count;
                                    newSeg.PrevSegmentIndex = prevSegIndex;
                                }
                                prevSegIndex = -1;
                                navSegments.Add(newSeg);

                                cellClearances.Clear();
                                searchForHead = true;

                            }
                            else
                            {
                                cellClearances.Add(clearance);
                            }
                        }
                        cellSpace[1] = fudgedA + cellOffset * iCell;
                        cellSpace[2] = fudgedA + cellHeight + cellOffset * iCell;
                        if (iCell < cellCount - 2)
                        {
                            cellSpace[0] = fudgedA + cellOffset * (iCell + 1);
                            cellSpace[3] = fudgedA + cellHeight + cellOffset * (iCell + 1);
                        }
                        else
                        {
                            cellSpace[0] = b + normal * FUDGE_FACTOR;
                            cellSpace[3] = cellSpace[0] + cellHeight;
                        }
                    }
                    if (!searchForHead)
                    {
                        var newSeg = new NavSegment(
                            owner,
                            surfaceWorldToLocal.MultiplyPoint3x4(segmentStart * dir + a),
                            surfaceWorldToLocal.MultiplyVector(dir),
                            length - segmentStart,
                            cellClearances.ToArray()
                            );

                        if (prevSegIndex != -1)
                        {
                            navSegments[prevSegIndex].NextSegmentIndex = navSegments.Count;
                            newSeg.PrevSegmentIndex = prevSegIndex;
                        }
                        prevSegIndex = navSegments.Count;
                        navSegments.Add(newSeg);

                    }
                    lastSegTouchesLastPoint = !searchForHead;
                }
            }
            //check for reach around
            if (firstNavSegIndex < navSegments.Count &&
                lastSegTouchesLastPoint &&
                (navSegments[firstNavSegIndex].Start
                 - navSegments[navSegments.Count - 1].End).sqrMagnitude < 0.01f * 0.01f
                 )
            {
                navSegments[firstNavSegIndex].PrevSegmentIndex = navSegments.Count - 1;
                navSegments[navSegments.Count - 1].NextSegmentIndex = firstNavSegIndex;
            }
        }

        private NavSegment CreateFreeSegment(int cellCount, NavSurface owner, Vector2 a, Vector2 dir, float length)
        {
            float[] cellClearances = new float[cellCount];
            for (int i = 0; i < cellCount; i++)
                cellClearances[i] = paramProvider.MaxClearance;

            return new NavSegment(owner, surfaceWorldToLocal.MultiplyPoint3x4(a), surfaceWorldToLocal.MultiplyVector(dir), length, cellClearances);
        }

#if false
        public static void UpdateNavObstacle(IEnumerable<NavSurface> surfaces, NavObstacle obstacle, Clipper clipper, NavSegmentCreationSettings settings)
        {
            foreach (var surface in surfaces)
            {
                UpdateNavObstacle(surface, obstacle, clipper, settings);
            }
        }

        public static void UpdateNavObstacle(NavSurface surface, NavObstacle obstacle, Clipper clipper, NavSegmentCreationSettings settings)
        {
            var obstacleBounds = obstacle.Bounds;
            var surfBounds = surface.WorldBounds;
            surfBounds.size += new Vector2(settings.AgentHeight, settings.AgentHeight) * 2;
            if (surfBounds.Overlaps(obstacleBounds))
            {
                for (int iSeg = 0; iSeg < surface.NavSegments.Count; iSeg++)
                {
                    var seg = surface.NavSegments[iSeg];
                    // is within effected bounds?
                    Vector2 min = seg.Start;
                    Vector2 max = seg.Start;
                    min = Vector2.Min(min, seg.End);
                    max = Vector2.Max(max, seg.End);
                    min = Vector2.Min(min, seg.End + seg.Normal * settings.AgentHeight);
                    max = Vector2.Max(max, seg.End + seg.Normal * settings.AgentHeight);
                    min = Vector2.Min(min, seg.Start + seg.Normal * settings.AgentHeight);
                    max = Vector2.Max(max, seg.Start + seg.Normal * settings.AgentHeight);

                    Rect segBounds = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
                    /*GizmosQueue.Instance.Enqueue(10, () =>
                    {
                        Gizmos.color = Color.red;
                        ABC.Utility.DrawRect(segBounds.position, segBounds.size);
                    });*/

                    if (obstacleBounds.Overlaps(segBounds))
                    {
                        float[] clearances;
                        if (UpdateMarkOfNavSegment(seg, obstacle, settings, clipper, out clearances))
                        {
                            obstacle.EffectedNavSegments.Add(new Tuple<NavSegmentDataPointer, float[]>(new NavSegmentDataPointer(surface, iSeg), clearances));
                        }
                    }
                }
            }
        }

        public static void UpdateNavAreaMarker(IEnumerable<NavSurface> surfaces, NavAreaMarker marker)
        {
            Matrix4x4 worldToMarker = marker.transform.worldToLocalMatrix;

            Rect rect = marker.Bounds;
            marker.EffectedNavSegments.Clear();

            foreach (var surf in surfaces)
            {
                for (int iSeg = 0; iSeg < surf.SegmentCount; iSeg++)
                {
                    var seg = surf.NavSegments[iSeg];
                    float u1, u2;

                    /*
                    GizmosQueue.Instance.Enqueue(5, () =>
                    {
                        
                        var old = Gizmos.matrix;
                        Gizmos.color = Color.magenta;
                        ABC.Utility.DrawRect(rect.position, rect.size);
                        Gizmos.matrix = marker.transform.localToWorldMatrix;
                        Gizmos.color = Color.red;
                        ABC.Utility.DrawRect(rect.position, rect.size);
                        Gizmos.color = Color.magenta;
                        Gizmos.matrix = worldToMarker;
                        Gizmos.DrawLine(seg.Start, seg.End);
                        Gizmos.matrix = old;
                    });*/

                    if (RectLineIntersection(rect, worldToMarker.MultiplyPoint(seg.Start), worldToMarker.MultiplyPoint(seg.End), out u1, out u2))
                    {
                        // entirely inside
                        if (u1 < 0 && 1 < u2)
                        {
                            marker.EffectedNavSegments.Add(new Tuple<NavSegmentDataPointer, Vector2>(
                                new NavSegmentDataPointer(surf, iSeg), new Vector2(0, seg.Length)));
                        }

                        // 2 intersections
                        else if (0 <= u1 && u1 < u2 && u2 <= 1)
                        {
                            marker.EffectedNavSegments.Add(new Tuple<NavSegmentDataPointer, Vector2>(
                               new NavSegmentDataPointer(surf, iSeg),
                               new Vector2(u1 * seg.Length, u2 * seg.Length)));
                        }

                        // 1 intersection
                        else if (0 <= u1 && u1 <= 1)
                        {
                            marker.EffectedNavSegments.Add(new Tuple<NavSegmentDataPointer, Vector2>(
                               new NavSegmentDataPointer(surf, iSeg),
                               new Vector2(u1 * seg.Length, seg.Length)));
                        }
                        else if (0 <= u2 && u2 <= 1)
                        {
                            marker.EffectedNavSegments.Add(new Tuple<NavSegmentDataPointer, Vector2>(
                                   new NavSegmentDataPointer(surf, iSeg),
                                   new Vector2(0, u2 * seg.Length)));
                        }
                    }
                }
            }
        }
#endif


#if false
        public static void UpdateMarkAddNavSurface(IEnumerable<NavSegment> navSegments, NavSurface navSurface, NavSegmentCreationSettings settings, Clipper clipper)
        {
            //1. find bounds of navSurface
            var surfaceBounds = navSurface.Bounds;
            var filter = new NavSurfaceColliderFilter(navSurface);

            foreach (var seg in navSegments)
            {
                // is within effected bounds?

                Vector2 min = seg.Start;
                Vector2 max = seg.Start;
                min = Vector2.Min(min, seg.End);
                max = Vector2.Max(max, seg.End);
                min = Vector2.Min(min, seg.End + seg.Normal * settings.AgentHeight);
                max = Vector2.Max(max, seg.End + seg.Normal * settings.AgentHeight);
                min = Vector2.Min(min, seg.Start + seg.Normal * settings.AgentHeight);
                max = Vector2.Max(max, seg.Start + seg.Normal * settings.AgentHeight);

                if (surfaceBounds.Overlaps(new Rect(min.x, min.y, max.x - min.x, max.y - min.y))){
                    UpdateMarkOfNavSegment(seg, filter, settings, clipper);
                }
            }
        }
#endif
#if false
        // filter needs to filter out non new colliders
        public static bool UpdateMarkOfNavSegment(NavSegment navSegment, NavObstacle obstacle, NavSegmentCreationSettings settings, Clipper clipper, out float[] clearanceValues)
        {

            List<IntPoint> polygonToTestAgainst = ConvertHullToClipperFormat(HullFromCollider(obstacle.Collider, settings, obstacle.transform.localToWorldMatrix), settings.FloatToIntMultiplier);

            float correctedCellSize = navSegment.Length / navSegment.CellCount;
            float f2iCellSize = CIntConversion.FloatToCInt(correctedCellSize);
            float f2iMinClearance = settings.FloatToIntMultiplier * settings.MinClearance;
            float f2iMaxClearance = settings.FloatToIntMultiplier * settings.AgentHeight;
            IntPoint f2iA = CIntConversion.Vector2ToIntPoint(navSegment.LStart);

            IntPoint cellOffset = new IntPoint(navSegment.Tangent.x * f2iCellSize, navSegment.Tangent.y * f2iCellSize);

            List<IntPoint> cellSpace = new List<IntPoint>(4);
            cellSpace.Add(f2iA + cellOffset);
            cellSpace.Add(f2iA);
            cellSpace.Add(f2iA);
            cellSpace.Add(f2iA);

            clearanceValues = new float[navSegment.CellCount];
            bool segmentWasChanged = false;
            for (int iCell = 0; iCell < navSegment.CellCount; iCell++)
            {
                float prevClearance = navSegment.GetCellClearance(iCell);
                if (prevClearance <= settings.MinClearance)
                    continue;

                IntPoint cellHeight = CIntConversion.Vector2ToIntPoint(navSegment.Normal * prevClearance);
                cellSpace[2] = cellSpace[1] + cellHeight;
                cellSpace[3] = cellSpace[0] + cellHeight;

                /*
                var clone = new List<IntPoint>(cellSpace);
                GizmosQueue.Instance.Enqueue(5, () =>
                {
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[0], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[1], settings.FloatToIntMultiplier));
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[1], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[2], settings.FloatToIntMultiplier));
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[2], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[3], settings.FloatToIntMultiplier));
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[3], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[0], settings.FloatToIntMultiplier));
                });
                */

                float clearance = CalculateCellClearance(cellSpace, navSegment.Normal, f2iMinClearance, prevClearance * settings.FloatToIntMultiplier, polygonToTestAgainst, clipper) / settings.FloatToIntMultiplier;
                segmentWasChanged = clearance < prevClearance || segmentWasChanged;
                clearanceValues[iCell] = clearance;

                cellSpace[1] += cellOffset;
                if (iCell < navSegment.CellCount - 2)
                {
                    cellSpace[0] += cellOffset;
                }
                else
                {
                    cellSpace[0] = CIntConversion.Vector2ToIntPoint(navSegment.LEnd);
                }
            }
            return segmentWasChanged;
        }
#endif
#if false
        public static bool UpdateMarkOfNavSegmentComplete(NavSegment navSegment, IColliderFilter filter, NavSegmentCreationSettings settings, Clipper clipper)
        {
            Vector2 oBoxPoint = navSegment.Start + navSegment.Tangent * navSegment.Length * 0.5f + navSegment.Normal * settings.AgentHeight * 0.5f;
            Vector2 oBoxSize = new Vector2(navSegment.Length, settings.AgentHeight);
            float oBoxAngle = Vector2.SignedAngle(Vector2.up, navSegment.Normal);

            Collider2D[] touchedColliders = Physics2D.OverlapBoxAll(oBoxPoint, oBoxSize, oBoxAngle, settings.IncludeLayers);

            List<List<IntPoint>> polygonsToTestAgainst = new List<List<IntPoint>>(touchedColliders.Length);
            foreach (var col in filter.Filter(touchedColliders))
            {
                polygonsToTestAgainst.Add(ConvertHullToClipperFormat(HullFromCollider(col, settings), settings.FloatToIntMultiplier));
            }

            float correctedCellSize = navSegment.Length / navSegment.CellCount;
            float f2iCellSize = CIntConversion.FloatToCInt(correctedCellSize, settings.FloatToIntMultiplier);
            float f2iMinClearance = settings.FloatToIntMultiplier * settings.MinClearance;
            float f2iMaxClearance = settings.FloatToIntMultiplier * settings.AgentHeight;
            IntPoint f2iA = CIntConversion.Vector2ToIntPoint(navSegment.Start, settings.FloatToIntMultiplier);

            Vector2 normal = navSegment.Normal;
            IntPoint cellOffset = new IntPoint(navSegment.Tangent.x * f2iCellSize, navSegment.Tangent.y * f2iCellSize);
            IntPoint cellHeight = new IntPoint(Mathf.RoundToInt(normal.x * settings.AgentHeight * settings.FloatToIntMultiplier),
                        Mathf.RoundToInt(normal.y * settings.AgentHeight * settings.FloatToIntMultiplier));

            List<IntPoint> cellSpace = new List<IntPoint>(4);
            cellSpace.Add(f2iA + cellOffset);
            cellSpace.Add(f2iA);
            cellSpace.Add(f2iA);
            cellSpace.Add(f2iA);

            bool segmentWasChanged = false;
            for (int iCell = 0; iCell < navSegment.CellCount; iCell++)
            {
                float prevClearance = navSegment.GetCellClearance(iCell);

                cellSpace[2] = cellSpace[1] + cellHeight;
                cellSpace[3] = cellSpace[0] + cellHeight;

                /*
                var clone = new List<IntPoint>(cellSpace);
                GizmosQueue.Instance.Enqueue(5, () =>
                {
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[0], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[1], settings.FloatToIntMultiplier));
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[1], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[2], settings.FloatToIntMultiplier));
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[2], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[3], settings.FloatToIntMultiplier));
                    Gizmos.DrawLine(CIntConversion.IntPointToVector2(clone[3], settings.FloatToIntMultiplier), CIntConversion.IntPointToVector2(clone[0], settings.FloatToIntMultiplier));
                });
                */

                float clearance = CalculateCellClearance(cellSpace, navSegment.Normal, f2iMinClearance, f2iMaxClearance, polygonsToTestAgainst, clipper) / settings.FloatToIntMultiplier;
                segmentWasChanged = clearance != prevClearance || segmentWasChanged;
                navSegment.SetCellClearanceAt(iCell, clearance);

                cellSpace[1] += cellOffset;
                if (iCell < navSegment.CellCount - 2)
                {
                    cellSpace[0] += cellOffset;
                }
                else
                {
                    cellSpace[0] = CIntConversion.Vector2ToIntPoint(navSegment.End, settings.FloatToIntMultiplier);
                }
            }
            return segmentWasChanged;
        }
#endif
        private float CalculateCellClearance(Polygon cell, Vector2 lineNormalNorm, float minClearance, float maxClearance, List<Polygon> otherPolygons)
        {
            float clearance = maxClearance;
            foreach (var testCandidate in otherPolygons)
            {
                if (testCandidate.Hull.IsClosed)
                {
                    var resultType = clipper.Compute(cell, testCandidate, BoolOpType.INTERSECTION, out var resultPolygon);

                    /*var a = cell;
                    var b = testCandidate;
                    GizmosQueue.Instance.Enqueue(5, () =>
                    {
                        Gizmos.color = Color.red;
                        a.Draw();
                        Gizmos.color = Color.blue;
                        b.Draw();
                    });*/

                    if (resultType == ResultType.NoOverlap)
                        continue;

                    double combinedArea = 0;
                    for (int i = 0; i < resultPolygon.Count; i++)
                    {
                        combinedArea += resultPolygon[i].Hull.Area();
                    }
                    if (combinedArea < 0.001)
                        continue;

                    foreach (var intersectionPolygon in resultPolygon)
                    {
                        foreach (var point in intersectionPolygon.Hull)
                        {
                            float dot = DistancePointLine(point, cell.Hull[1], lineNormalNorm);
                            if (dot < clearance)
                            {
                                clearance = dot;
                                if (clearance < minClearance)
                                {
                                    return 0;
                                }
                            }
                        }
                    }
                }
                else
                {
                    float c = OpenPolygonClearanceClipping(
                        testCandidate, cell, lineNormalNorm, minClearance, maxClearance);
                    if (c < clearance)
                    {
                        clearance = c;
                        if (clearance < minClearance)
                        {
                            return 0;
                        }
                    }
                }
            }
            return clearance;
        }

        private float OpenPolygonClearanceClipping(Polygon openPoly, Polygon target, Vector2 lineNormalNorm, float minClearance, float maxClearance)
        {
            float clearance = maxClearance;
            bool inside = target.PointInPolyon(openPoly.Hull[0]);
            bool prevNormalCheck = false;
            for (int i = 0; i < openPoly.Hull.VertexCount - 1; i++)
            {
                Vector2 a = openPoly.Hull[i];
                Vector2 b = openPoly.Hull[i + 1];

                clearance = EdgePolygonClearanceClipping(openPoly.Hull[i],
                    openPoly.Hull[i + 1],
                    lineNormalNorm,
                    target,
                    minClearance,
                    maxClearance,
                    clearance,
                    ref inside,
                    ref prevNormalCheck);

                if (clearance < minClearance)
                {
                    return 0;
                }
            }
            return clearance;
        }

        private float EdgePolygonClearanceClipping(Vector2 a, Vector2 b, Vector2 lineNormal, Polygon target,
            float minClearance, float maxClearance, float clearance, ref bool inside, ref bool prevNormalCheck)
        {
            Vector2 dir = b - a;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            bool normalCheck = Vector2.Dot(normal, lineNormal) < 0;


            if (inside && (normalCheck || prevNormalCheck))
            {
                float dist = DistancePointLine(a, target.Hull[1], lineNormal);
                if (dist < clearance)
                {
                    clearance = dist;
                    if (clearance < minClearance)
                    {
                        return 0;
                    }
                }
            }
            prevNormalCheck = normalCheck;

            Vector2 inter;
            if ((ExtendedGeometry.FindLineIntersection(a, b, target.Hull[0], target.Hull[1], out inter)
                || ExtendedGeometry.FindLineIntersection(a, b, target.Hull[1], target.Hull[2], out inter)
                || ExtendedGeometry.FindLineIntersection(a, b, target.Hull[2], target.Hull[3], out inter)
                || ExtendedGeometry.FindLineIntersection(a, b, target.Hull[3], target.Hull[0], out inter))
                && inter != a && inter != b)
            {
                if (normalCheck)
                {
                    float dist = DistancePointLine(inter, target.Hull[1], lineNormal);
                    if (dist < clearance)
                    {
                        clearance = dist;
                        if (clearance < minClearance)
                        {
                            return 0;
                        }
                    }
                }
                inside = !inside;

                clearance = EdgePolygonClearanceClipping(
                    inter, b, lineNormal, target, minClearance, maxClearance, clearance,
                    ref inside, ref prevNormalCheck);
            }
            return clearance;
        }

        private float DistancePointLine(Vector2 point, Vector2 la, Vector2 lNormal)
        {
            return lNormal.x * (point.x - la.x) + lNormal.y * (point.y - la.y);
        }
    }
}