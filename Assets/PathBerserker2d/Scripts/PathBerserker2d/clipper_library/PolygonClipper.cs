using UnityEngine;
using System.Collections.Generic;
using Priority_Queue;
using System;

namespace PathBerserker2d
{
    internal class PolygonClipper : IClipper
    {
        public enum EdgeType { NORMAL, NON_CONTRIBUTING, SAME_TRANSITION, DIFFERENT_TRANSITION };
        public enum PolygonType { SUBJECT, CLIPPING };

        private SweepEventPriorityQueue eventQueue = new SweepEventPriorityQueue(50);
        private SweepEvent[] sweepEventPool = new SweepEvent[100];
        private int usedSweepEventCount = 0;
        private SweepRay sweepRay = new SweepRay(20);

        public PolygonClipper()
        {
            for (int i = 0; i < sweepEventPool.Length; i++)
                sweepEventPool[i] = new SweepEvent();
        }

        public ResultType Compute(Polygon sp, Polygon cp, BoolOpType op, out List<Polygon> result, bool includeOpenPolygons = false)
        {
            result = new List<Polygon>();
            if (sp.IsEmpty || cp.IsEmpty)
            {
                return ResultType.NoOverlap;
            }

            //Trivial case: The polygons cannot intersect each other.
            if (!sp.BoundsOverlap(cp))
            {
                return ResultType.NoOverlap;
            }

            //Init the event queue with the polygon edges
            usedSweepEventCount = 0;
            eventQueue.Clear();
            InsertPolygon(sp, PolygonType.SUBJECT);
            InsertPolygon(cp, PolygonType.CLIPPING);

            sweepRay.Clear();
            SweepEvent cEvent;
            float minRightBounds = Mathf.Min(sp.XMax, cp.XMax) + 0.01f;
            Connector connector = new Connector();
            bool intersectionHappened = false;

            while (eventQueue.Count != 0)
            {
                cEvent = eventQueue.Dequeue();
                if ((op == BoolOpType.INTERSECTION && cEvent.point.x > minRightBounds) ||
                    (op == BoolOpType.DIFFERENCE && cEvent.point.x > sp.XMax))
                {
                    //Exit the loop. No more intersections are to be found.
                    // Create a polygon out of the pointchain
                    return EvaluateResult(connector, op, intersectionHappened, includeOpenPolygons, ref result);
                }
                if (op == BoolOpType.UNION && cEvent.point.x > minRightBounds)
                {
                    if (!cEvent.Left)
                    {
                        AddEventToConnector(connector, cEvent);
                    }
                    while (eventQueue.Count != 0)
                    {
                        cEvent = eventQueue.Dequeue();
                        if (!cEvent.Left)
                        {
                            AddEventToConnector(connector, cEvent);
                        }
                    }
                    return EvaluateResult(connector, op, intersectionHappened, includeOpenPolygons, ref result);
                }

                if (cEvent.Left)
                {// the line segment must be inserted into S
                    int pos = sweepRay.Add(cEvent);
                    SweepEvent prev = sweepRay.Previous(pos);
                    if (prev == null)
                        cEvent.Inside = cEvent.InOut = false;
                    else if (prev.EdgeType != EdgeType.NORMAL)
                    {
                        if (pos - 1 == 0)
                        {
                            cEvent.Inside = true;
                            cEvent.InOut = false;
                        }
                        else
                        {
                            SweepEvent sliEvent = sweepRay.Previous(pos - 1);
                            if (prev.PolyType == cEvent.PolyType)
                            {
                                cEvent.InOut = !prev.InOut;
                                cEvent.Inside = !sliEvent.InOut;
                            }
                            else
                            {
                                cEvent.InOut = !sliEvent.InOut;
                                cEvent.Inside = !prev.InOut;
                            }
                        }
                    }
                    else if (cEvent.PolyType == prev.PolyType)
                    { // previous line segment in S belongs to the same polygon that "cEvent" belongs to
                        cEvent.Inside = prev.Inside;
                        cEvent.InOut = !prev.InOut;
                    }
                    else
                    {                          // previous line segment in S belongs to a different polygon that "cEvent" belongs to
                        cEvent.Inside = !prev.InOut;
                        cEvent.InOut = prev.Inside;
                    }

                    SweepEvent nextEvent = sweepRay.Next(pos);
                    if (nextEvent != null)
                        intersectionHappened |= HandlePossibleIntersection(cEvent, nextEvent);
                    if (prev != null)
                        intersectionHappened |= HandlePossibleIntersection(cEvent, prev);
                }
                else
                {// the line segment must be removed from S
                    int pos = sweepRay.Find(cEvent.other);
                    switch (cEvent.EdgeType)
                    {
                        case EdgeType.NORMAL:
                            switch (op)
                            {
                                case BoolOpType.INTERSECTION:
                                    if (cEvent.other.Inside)
                                        AddEventToConnector(connector, cEvent);
                                    break;
                                case BoolOpType.UNION:
                                    if (!cEvent.other.Inside)
                                        AddEventToConnector(connector, cEvent);
                                    break;
                                case BoolOpType.DIFFERENCE:
                                    if (cEvent.PolyType == PolygonType.SUBJECT && !cEvent.other.Inside)
                                        AddEventToConnector(connector, cEvent);
                                    break;
                            }
                            break;
                        case EdgeType.SAME_TRANSITION:
                            if (op == BoolOpType.INTERSECTION || op == BoolOpType.UNION)
                                AddEventToConnector(connector, cEvent);
                            break;
                        case EdgeType.DIFFERENT_TRANSITION:
                            if (op == BoolOpType.DIFFERENCE)
                                AddEventToConnector(connector, cEvent);
                            break;
                    }
                    // delete line segment associated to e from S and check for intersection between the neighbors of "e" in S
                    SweepEvent next = sweepRay.Next(pos), prev = sweepRay.Previous(pos);
                    sweepRay.RemoveAt(pos);
                    if (next != null && prev != null)
                        intersectionHappened |= HandlePossibleIntersection(prev, next);
                }
            }

            return EvaluateResult(connector, op, intersectionHappened, includeOpenPolygons, ref result);
        }

        private void AddEventToConnector(Connector connector, SweepEvent ev)
        {
            if (ev.WrapWiseLeft) connector.Add(ev.point, ev.other.point); else connector.Add(ev.other.point, ev.point);
        }

        private ResultType EvaluateResult(Connector connector, BoolOpType opType, bool intersectionHappened, bool includeOpenPaths, ref List<Polygon> result)
        {
            if (!intersectionHappened && (opType == BoolOpType.INTERSECTION || (opType == BoolOpType.UNION && !includeOpenPaths)))
                return ResultType.NoOverlap;

            Contour[] contours = new Contour[connector.GetNumClosedPolygons() + (includeOpenPaths ? connector.GetNumOpenPolygons() : 0)];

            int fill = 0;
            foreach (var closedPoly in connector.closedPolygons)
            {
                contours[fill++] = new Contour(closedPoly.points, true);
            }
            if (includeOpenPaths)
            {
                foreach (var openPoly in connector.openPolygons)
                {
                    contours[fill++] = new Contour(openPoly.points, false);
                }
            }

            if (opType == BoolOpType.UNION)
            {
                if (connector.GetNumClosedPolygons() == 0)
                    return ResultType.NoOverlap;

                List<Contour> holes = new List<Contour>(contours.Length - 1);
                Contour hull = null;
                double largestArea = 0;

                foreach (var contour in contours)
                {
                    double area = contour.Area();

                    if (area > largestArea)
                    {
                        if (hull != null)
                        {
                            holes.Add(hull);
                        }
                        hull = contour;
                        largestArea = area;
                    }
                }
                result.Add(new Polygon(hull, holes));
            }
            else if (opType == BoolOpType.INTERSECTION || opType == BoolOpType.DIFFERENCE)
            {
                for (int i = 0; i < contours.Length; i++)
                {
                    if (contours[i].IsClosed)
                    {
                        for (int j = i + 1; j < contours.Length; j++)
                        {
                            foreach (var cp in contours[j])
                            {
                                if (ExtendedGeometry.ContainsPoint(contours[i], contours[i].Verts[contours[i].VertexCount - 1], cp)
    )
                                {
                                    goto label1;
                                }
                            }
                        }
                    }
                    result.Add(new Polygon(contours[i]));
                label1:
                    continue;
                }
            }
            return ResultType.Clipped;
        }

        private void InsertPolygon(Polygon polygon, PolygonType polygonType)
        {
            int sweepIndex = RequestSweepEvents(polygon.TotalVertCount() * 2);
            foreach (var contour in polygon)
            {
                Vector2 cVal;
                Vector2 cPrevVal = contour[contour.IsClosed ? contour.VertexCount - 1 : 0];
                for (int iVert = contour.IsClosed ? 0 : 1; iVert < contour.VertexCount; iVert++)
                {
                    cVal = contour[iVert];
                    if (cVal.x < cPrevVal.x || (cVal.x == cPrevVal.x && cVal.y < cPrevVal.y))
                    {
                        sweepEventPool[sweepIndex++].SetData(cVal, true, false, polygonType);
                        sweepEventPool[sweepIndex++].SetData(cPrevVal, false, true, polygonType);
                    }
                    else
                    {
                        sweepEventPool[sweepIndex++].SetData(cVal, false, false, polygonType);
                        sweepEventPool[sweepIndex++].SetData(cPrevVal, true, true, polygonType);
                    }
                    sweepEventPool[sweepIndex - 1].other = sweepEventPool[sweepIndex - 2];
                    sweepEventPool[sweepIndex - 2].other = sweepEventPool[sweepIndex - 1];
                    cPrevVal = cVal;

                    eventQueue.Enqueue(sweepEventPool[sweepIndex - 1]);
                    eventQueue.Enqueue(sweepEventPool[sweepIndex - 2]);
                }
            }
        }

        private int RequestSweepEvents(int count)
        {
            if (sweepEventPool.Length - usedSweepEventCount < count)
            {
                int i = sweepEventPool.Length;
                System.Array.Resize<SweepEvent>(ref sweepEventPool, usedSweepEventCount + count + 10);

                for (; i < sweepEventPool.Length; i++)
                {
                    sweepEventPool[i] = new SweepEvent();
                }
            }
            usedSweepEventCount += count;
            return usedSweepEventCount - count;
        }

        private int FindIntersection(SweepEvent se1, SweepEvent se2, out Vector2 pA, out Vector2 pB)
        {
            //Assign the resulting points some dummy values
            pA = Vector2.zero;
            pB = Vector2.zero;
            Vector2 se1_Begin = (se1.Left) ? se1.point : se1.other.point;
            Vector2 se1_End = (se1.Left) ? se1.other.point : se1.point;
            Vector2 se2_Begin = (se2.Left) ? se2.point : se2.other.point;
            Vector2 se2_End = (se2.Left) ? se2.other.point : se2.point;

            Vector2 d0 = se1_End - se1_Begin;
            Vector2 d1 = se2_End - se2_Begin;
            Vector2 e = se2_Begin - se1_Begin;

            const double sqrEpsilon = 0.000001;
            const double epsilon = 0.00001;

            double kross = d0.x * d1.y - d0.y * d1.x;
            double sqrKross = kross * kross;
            double sqrLen0 = d0.sqrMagnitude;
            double sqrLen1 = d1.sqrMagnitude;

            if (sqrKross > sqrEpsilon * sqrLen0 * sqrLen1)
            {
                // lines of the segments are not parallel
                double s = (e.x * d1.y - e.y * d1.x) / kross;
                if ((s < 0) || (s > 1))
                {
                    return 0;
                }
                double t = (e.x * d0.y - e.y * d0.x) / kross;
                if ((t < 0) || (t > 1))
                {
                    return 0;
                }
                // intersection of lines is a point an each segment
                pA = new Vector2((float)(se1_Begin.x + s * d0.x), (float)(se1_Begin.y + s * d0.y));
                if ((pA - se1_Begin).magnitude < epsilon) pA = se1_Begin;
                if ((pA - se1_End).magnitude < epsilon) pA = se1_End;
                if ((pA - se2_Begin).magnitude < epsilon) pA = se2_Begin;
                if ((pA - se2_End).magnitude < epsilon) pA = se2_End;
                return 1;
            }

            // lines of the segments are parallel
            double sqrLenE = e.sqrMagnitude;
            kross = e.x * d0.y - e.y * d0.x;
            sqrKross = kross * kross;
            if (sqrKross > sqrEpsilon * sqrLen0 * sqrLenE)
            {
                // lines of the segment are different
                return 0;
            }

            // Lines of the segments are the same. Need to test for overlap of segments.
            double s0 = (d0.x * e.x + d0.y * e.y) / sqrLen0;  // so = Dot (D0, E) * sqrLen0
            double s1 = s0 + (d0.x * d1.x + d0.y * d1.y) / sqrLen0;  // s1 = s0 + Dot (D0, D1) * sqrLen0
            double smin = Math.Min(s0, s1);
            double smax = Math.Max(s0, s1);
            double[] w = new double[2];
            int imax = FindIntersection(0.0, 1.0, smin, smax, w);

            if (imax > 0)
            {
                pA = new Vector2((float)(se1_Begin.x + w[0] * d0.x), (float)(se1_Begin.y + w[0] * d0.y));
                if ((pA - se1_Begin).magnitude < epsilon) pA = se1_Begin;
                if ((pA - se1_End).magnitude < epsilon) pA = se1_End;
                if ((pA - se2_Begin).magnitude < epsilon) pA = se2_Begin;
                if ((pA - se2_End).magnitude < epsilon) pA = se2_End;
                if (imax > 1)
                {
                    pB = new Vector2((float)(se1_Begin.x + w[1] * d0.x), (float)(se1_Begin.y + w[1] * d0.y));
                }
            }
            return imax;
        }

        private int FindIntersection(double u0, double u1, double v0, double v1, double[] w)
        {
            if ((u1 < v0) || (u0 > v1))
                return 0;
            if (u1 > v0)
            {
                if (u0 < v1)
                {
                    w[0] = (u0 < v0) ? v0 : u0;
                    w[1] = (u1 > v1) ? v1 : u1;
                    return 2;
                }
                else
                {
                    // u0 == v1
                    w[0] = u0;
                    return 1;
                }
            }
            else
            {
                // u1 == v0
                w[0] = u1;
                return 1;
            }
        }

        private bool HandlePossibleIntersection(SweepEvent e1, SweepEvent e2)
        {
            /*
            Vector2 ip1, ip2;  // intersection points
            int nintersections;

            if ((nintersections = FindIntersection(e1, e2, out ip1, out ip2)) == 0)
                return false;

            if ((nintersections == 1) && (
                (e1.point == e2.point) ||
                (e1.other.point == e2.other.point) ||
                (e1.other.point == e2.point) ||
                (e2.point == e2.other.point)))
                return false; // the line segments intersect at an endpoint of both line segments

            if (nintersections == 2 && e1.PolyType == e2.PolyType)
                return false; // the line segments overlap, but they belong to the same polygon
            */

            if (e1.PolyType == e2.PolyType)
                return false; // the line segments belong to the same polygon

            Vector2 ip1, ip2;  // intersection points
            int nintersections;

            if ((nintersections = FindIntersection(e1, e2, out ip1, out ip2)) == 0)
                return false;

            if ((nintersections == 1) && (
                (e1.point == e2.point) ||
                (e1.other.point == e2.other.point) ||
                (e1.other.point == e2.point) ||
                (e2.point == e2.other.point)))
                return true; // the line segments intersect at an endpoint of both line segments



            // The line segments associated to e1 and e2 intersect
            if (nintersections == 1)
            {
                if (e1.point != ip1 && e1.other.point != ip1)  // if ip1 is not an endpoint of the line segment associated to e1 then divide "e1"
                    DivideEdge(e1, ip1);
                if (e2.point != ip1 && e2.other.point != ip1)  // if ip1 is not an endpoint of the line segment associated to e2 then divide "e2"
                    DivideEdge(e2, ip1);
                return true;
            }

            // The line segments overlap
            List<SweepEvent> sortedEvents = new List<SweepEvent>(2);
            if (e1.point == e2.point)
            {
                sortedEvents.Add(null);
            }
            else if (e1.CompareTo(e2) > 0)
            {
                sortedEvents.Add(e2);
                sortedEvents.Add(e1);
            }
            else
            {
                sortedEvents.Add(e1);
                sortedEvents.Add(e2);
            }

            if (e1.other.point == e2.other.point)
            {
                sortedEvents.Add(null);
            }
            else if (e1.other.CompareTo(e2.other) > 0)
            {
                sortedEvents.Add(e2.other);
                sortedEvents.Add(e1.other);
            }
            else
            {
                sortedEvents.Add(e1.other);
                sortedEvents.Add(e2.other);
            }

            if (sortedEvents.Count == 2)
            { // are both line segments equal?
                e1.EdgeType = e1.other.EdgeType = EdgeType.NON_CONTRIBUTING;
                e2.EdgeType = e2.other.EdgeType = (e1.InOut == e2.InOut) ? EdgeType.SAME_TRANSITION : EdgeType.DIFFERENT_TRANSITION;
                return true;
            }
            if (sortedEvents.Count == 3)
            { // the line segments share an endpoint
                sortedEvents[1].EdgeType = sortedEvents[1].other.EdgeType = EdgeType.NON_CONTRIBUTING;
                if (sortedEvents[0] != null)         // is the right endpoint the shared point?
                    sortedEvents[0].other.EdgeType = (e1.InOut == e2.InOut) ? EdgeType.SAME_TRANSITION : EdgeType.DIFFERENT_TRANSITION;
                else                                // the shared point is the left endpoint
                    sortedEvents[2].other.EdgeType = (e1.InOut == e2.InOut) ? EdgeType.SAME_TRANSITION : EdgeType.DIFFERENT_TRANSITION;
                DivideEdge(sortedEvents[0] != null ? sortedEvents[0] : sortedEvents[2].other, sortedEvents[1].point);
                return true;
            }
            if (sortedEvents[0] != sortedEvents[3].other)
            { // no line segment includes totally the other one
                sortedEvents[1].EdgeType = EdgeType.NON_CONTRIBUTING;
                sortedEvents[2].EdgeType = (e1.InOut == e2.InOut) ? EdgeType.SAME_TRANSITION : EdgeType.DIFFERENT_TRANSITION;
                DivideEdge(sortedEvents[0], sortedEvents[1].point);
                DivideEdge(sortedEvents[1], sortedEvents[2].point);
                return true;
            }
            // one line segment includes the other one
            sortedEvents[1].EdgeType = sortedEvents[1].other.EdgeType = EdgeType.NON_CONTRIBUTING;
            DivideEdge(sortedEvents[0], sortedEvents[1].point);
            sortedEvents[3].other.EdgeType = (e1.InOut == e2.InOut) ? EdgeType.SAME_TRANSITION : EdgeType.DIFFERENT_TRANSITION;
            DivideEdge(sortedEvents[3].other, sortedEvents[2].point);

            return true;
        }

        private void DivideEdge(SweepEvent e, Vector2 p)
        {
            int sweepIndex = RequestSweepEvents(2);

            // "Right event" of the "left line segment" resulting from dividing e (the line segment associated to e)
            SweepEvent r = sweepEventPool[sweepIndex++];
            r.SetData(p, false, e.other.WrapWiseLeft, e.PolyType);
            r.EdgeType = e.EdgeType;
            r.other = e;

            // "Left event" of the "right line segment" resulting from dividing e (the line segment associated to e)
            SweepEvent l = sweepEventPool[sweepIndex];
            l.SetData(p, true, e.WrapWiseLeft, e.PolyType);
            l.EdgeType = e.other.EdgeType;
            l.other = e.other;

            if (l.CompareTo(e.other) > 0)
            { // avoid a rounding error. The left event would be processed after the right event
                e.other.Left = true;
                l.Left = false;
            }
            e.other.other = l;
            e.other = r;
            eventQueue.Enqueue(r);
            eventQueue.Enqueue(l);
        }

        public class SweepEvent
        {
            public int queueIndex;
            public Vector2 point; // point associated with the event
            public SweepEvent other; // Event associated to the other endpoint of the segment

            public bool Left { get { return (data & 1) == 1; } set { data = (byte)(value ? (data | 1) : (data & ~1)); } }       // is the point the left endpoint of the segment (p, other.p)?
            public bool InOut { get { return (data & (1 << 1)) == 1 << 1; } set { data = (byte)(value ? (data | (1 << 1)) : (data & ~(1 << 1))); } } //  Does the segment (p, other.p) represent an inside-outside transition in the polygon for a vertical ray from (p.x, -infinite) that crosses the segment?
            public bool Inside { get { return (data & (1 << 2)) == 1 << 2; } set { data = (byte)(value ? (data | (1 << 2)) : (data & ~(1 << 2))); } } // Only used in "left" events. Is the segment (p, other.p) inside the other polygon?
            public bool WrapWiseLeft { get { return (data & (1 << 3)) == 1 << 3; } set { data = (byte)(value ? (data | (1 << 3)) : (data & ~(1 << 3))); } }

            public PolygonType PolyType
            {
                get { return (data & (1 << 4)) != 0 ? PolygonType.CLIPPING : PolygonType.SUBJECT; }
                set { data = (byte)(value == PolygonType.CLIPPING ? (data | (1 << 4)) : (data & ~(1 << 4))); }
            }    // Polygon to which the associated segment belongs to                    

            public EdgeType EdgeType
            {
                get
                {
                    return (EdgeType)(data >> 5);
                }
                set
                {
                    data = (byte)(data & ~(3 << 5) | (((int)value) << 5));
                }
            }

            private byte data;

            public void SetData(Vector2 point, bool left, bool wrapwiseLeft, PolygonType polyType)
            {
                this.data = 0;
                this.point = point;
                this.Left = left;
                this.WrapWiseLeft = wrapwiseLeft;
                this.PolyType = polyType;
            }

            /** Is the line segment (p, other.p) below point x */
            public bool IsBelow(Vector2 o) { return (Left) ? ExtendedGeometry.SignedAreaDoubledTris(point, other.point, o) > 0 : ExtendedGeometry.SignedAreaDoubledTris(other.point, point, o) > 0; }
            /** Is the line segment (p, other.p) above point x */
            public bool IsAbove(Vector2 o) { return !IsBelow(o); }

            public override string ToString()
            {
                return "SE (p = " + point + ", l = " + Left + ", pl = " + PolyType + ", inOut = " + ((Left) ? InOut : other.InOut) + ", inside = " + ((Left) ? Inside : other.Inside) + ", other.p = " + other.point + ")";
            }

            // Return true(1) means that e1 is placed at the event queue after e2, i.e,, e1 is processed by the algorithm after e2
            public int CompareTo(SweepEvent other)
            {
                if (point.x > other.point.x)
                    return 1;
                if (point.x < other.point.x)
                    return -1;
                if (point.y > other.point.y)
                    return 1;
                if (point.y < other.point.y)
                    return -1;
                if (Left != other.Left)
                {
                    if (Left)
                        return 1;
                    return -1;
                }
                if (IsAbove(other.other.point))
                    return 1;
                return -1;
            }
        }

        class SweepRay
        {
            List<SweepEvent> s;

            public SweepRay(int capacity)
            {
                s = new List<SweepEvent>(capacity);
            }

            public int Add(SweepEvent e)
            {
                for (int i = 0; i < s.Count; i++)
                {
                    SweepEvent se = s[i];
                    if (IsEventOneMoreImportant(se, e))
                        continue;
                    s.Insert(i, e);
                    return i;
                }
                s.Add(e);
                return s.Count - 1;
            }

            public int Find(SweepEvent e)
            {
                return s.IndexOf(e);
            }

            public void RemoveAt(int index)
            {
                s.RemoveAt(index);
            }

            public SweepEvent Next(int index)
            {
                index++;
                if (index < s.Count)
                    return s[index];
                return null;
            }

            public SweepEvent Previous(int index)
            {
                index--;
                if (index >= 0)
                    return s[index];
                return null;
            }

            public override string ToString()
            {
                string result = "[" + s.Count + "] ";
                foreach (SweepEvent se in s)
                    result += ((se.Left) ? "l" : "r") + se.ToString() + ", ";
                return result;
            }

            private bool IsEventOneMoreImportant(SweepEvent se1, SweepEvent se2)
            {
                if (se1 == se2)
                    return false;
                if (ExtendedGeometry.SignedAreaDoubledTris(se1.point, se1.other.point, se2.point) != 0 || ExtendedGeometry.SignedAreaDoubledTris(se1.point, se1.other.point, se2.other.point) != 0)
                {
                    if (se1.point == se2.point)
                        return se1.IsBelow(se2.other.point);

                    if (se1.CompareTo(se2) > 0)
                        return se2.IsAbove(se1.point);
                    return se1.IsBelow(se2.point);
                }
                if (se1.point == se2.point)
                    return false; //Not sure here. Seems like lines exactly overlap each other. Didnt found the < operator though.
                return se1.CompareTo(se2) > 0;
            }

            internal void Clear()
            {
                s.Clear();
            }
        }
    }
}