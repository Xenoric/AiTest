using UnityEngine;
using System.Collections.Generic;

namespace PathBerserker2d
{
    internal class ExtendedGeometry
    {
        public static float SignedArea(Vector2 a, Vector2 b, Vector2 c)
        {
            return (a.x - c.x) * (b.y - c.y) - (b.x - c.x) * (a.y - c.y);
        }

        /** Signed area of the triangle ( (0,0), p1, p2) */
        public static float SignedArea(Vector2 b, Vector2 c)
        {
            return -c.x * (b.y - c.y) - -c.y * (b.x - c.x);
        }

        /** Sign of triangle (p1, p2, o) */
        public static int Sign(Vector2 a, Vector2 b, Vector2 o)
        {
            float det = (a.x - o.x) * (b.y - o.y) - (b.x - o.x) * (a.y - o.y);
            return (det < 0 ? -1 : (det > 0 ? +1 : 0));
        }

        public static float SignedAreaDoubledTris(Vector2 a, Vector2 b, Vector2 c)
        {
            return (a.x - c.x) * (b.y - c.y) - (b.x - c.x) * (a.y - c.y);
        }

        /** Signed area of the triangle ( (0,0), p1, p2) */
        public static float SignedAreaDoubledTris(Vector2 b, Vector2 c)
        {
            return -c.x * (b.y - c.y) - -c.y * (b.x - c.x);
        }

        /** Sign of triangle (p1, p2, o) */
        public static int SignTris(Vector2 a, Vector2 b, Vector2 o)
        {
            float det = (a.x - o.x) * (b.y - o.y) - (b.x - o.x) * (a.y - o.y);
            return (det < 0 ? -1 : (det > 0 ? +1 : 0));
        }

        public static float SignedAreaDoubledRect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            return (a.y - c.y) * (d.x - b.x) + (b.y - d.y) * (a.x - c.x);
        }

        public static bool DoesLineIntersectBounds(Vector2 pA, Vector2 pB, Bounds bounds)
        {
            if (pA.x > bounds.max.x && pB.x > bounds.max.x) return false;
            if (pA.x < bounds.min.x && pB.x < bounds.min.x) return false;
            if (pA.y > bounds.max.y && pB.y > bounds.max.y) return false;
            if (pA.y < bounds.min.y && pB.y < bounds.min.y) return false;

            float z = pB.x * pA.y - pA.x * pB.y;
            float x = pB.y - pA.y;
            float y = pA.x - pB.x;

            float sign = Mathf.Sign(bounds.max.x * x + bounds.max.y * y + z);
            return (sign == Mathf.Sign(bounds.min.x * x + bounds.max.y * y + z) && sign == Mathf.Sign(bounds.max.x * x + bounds.max.y * y + z) && sign == Mathf.Sign(bounds.max.x * x + bounds.max.y * y + z));
        }


        public static bool IsOnLeftSideOfLine(Vector2 pA, Vector2 pB, Vector2 point)
        {
            return ((pB.x - pA.x) * (point.y - pA.y) - (pB.y - pA.y) * (point.x - pA.x)) > 0;
        }

        const float LineCircle_FudgeFactor = 0.00001f;
        // -1 = line is completly outside of the circle
        // 0 = 0 intersections found, line is completly inside of circle
        // 1 = 1 intersection found (i1)
        // 2 = 2 intersections found (i1, i2)
        public static int DoesLineIntersectWithCircle(Vector2 lA, Vector2 lB, Vector2 circleCenter, float radius, out Vector2 i1, out Vector2 i2)
        {
            i1 = Vector2.zero;
            i2 = Vector2.zero;

            Vector2 dir = (lB - lA);
            float distL = dir.magnitude;
            dir /= distL;

            float t = dir.x * (circleCenter.x - lA.x) + dir.y * (circleCenter.y - lA.y);

            Vector2 tangent = t * dir + lA;
            float distToCenter = (tangent - circleCenter).sqrMagnitude;
            float radSquared = radius * radius;

            if (distToCenter < radSquared)
            {
                float dt = Mathf.Sqrt(radSquared - distToCenter);
                float tMinDt = t - dt;
                if (tMinDt > 0 + LineCircle_FudgeFactor || tMinDt < distL - LineCircle_FudgeFactor)
                {
                    i1 = tMinDt * dir + lA - circleCenter;

                    tMinDt = t + dt;
                    if (tMinDt > 0 + LineCircle_FudgeFactor || tMinDt < distL - LineCircle_FudgeFactor)
                    {
                        i2 = tMinDt * dir + lA - circleCenter;
                        return 2;
                    }
                    return 1;
                }
                tMinDt = t + dt;
                if (tMinDt > 0 + LineCircle_FudgeFactor || tMinDt < distL - LineCircle_FudgeFactor)
                {
                    i1 = tMinDt * dir + lA - circleCenter;
                    return 1;
                }
                return 0;
            }
            else if (distToCenter == radSquared)
            {
                i1 = tangent - circleCenter;
                return 1;
            }
            else
            {
                return -1;
            }
        }

        public static bool FindLineIntersection(Vector2 l1a, Vector2 l1b, Vector2 l2a, Vector2 l2b, out Vector2 inter)
        {
            inter = Vector2.zero;
            var d = (l2b.y - l2a.y) * (l1b.x - l1a.x) - (l2b.x - l2a.x) * (l1b.y - l1a.y);

            //n_a and n_b are calculated as seperate values for readability
            var n_a =
               (l2b.x - l2a.x) * (l1a.y - l2a.y)
               -
               (l2b.y - l2a.y) * (l1a.x - l2a.x);

            var n_b =
               (l1b.x - l1a.x) * (l1a.y - l2a.y)
               -
               (l1b.y - l1a.y) * (l1a.x - l2a.x);

            // Make sure there is not a division by zero - this also indicates that
            // the lines are parallel.  
            // If n_a and n_b were both equal to zero the lines would be on top of each 
            // other (coincidental).  This check is not done because it is not 
            // necessary for this implementation (the parallel check accounts for this).
            if (d == 0)
                return false;

            // Calculate the intermediate fractional point that the lines potentially intersect.
            var ua = n_a / d;
            var ub = n_b / d;

            // The fractional point will be between 0 and 1 inclusive if the lines
            // intersect.  If the fractional calculation is larger than 1 or smaller
            // than 0 the lines would need to be longer to intersect.
            if (ua >= 0.0 && ua <= 1.0 && ub >= 0.0 && ub <= 1.0)
            {
                inter.x = l1a.x + (ua * (l1b.x - l1a.x));
                inter.y = l1a.y + (ua * (l1b.y - l1a.y));
                return true;
            }
            return false;
        }

        //https://gist.github.com/ChickenProp/3194723
        public static bool RectLineIntersection(Rect rect, Vector2 la, Vector2 lb, out float u1, out float u2)
        {
            Vector2 lDelta = lb - la;

            float[] p = new float[4] {
                -lDelta.x,
                lDelta.x,
                -lDelta.y,
                lDelta.y
            };

            float[] q = new float[4] {
                la.x - rect.xMin,
                rect.xMax - la.x,
                la.y - rect.yMin,
                rect.yMax - la.y,
            };

            u1 = Mathf.NegativeInfinity;
            u2 = Mathf.Infinity;
            for (int i = 0; i < 4; i++)
            {
                if (p[i] == 0)
                {
                    if (q[i] < 0)
                        return false;
                }
                else
                {
                    var t = q[i] / p[i];
                    if (p[i] < 0 && u1 < t)
                    {
                        u1 = t;
                    }
                    else if (p[i] > 0 && u2 > t)
                    {
                        u2 = t;
                    }
                }
            }
            return u1 <= u2;

            /*
            // entirely outside
            if (u1 > u2)
            {
                return false;
            }
            
            // entirely inside
            if (u1 < 0 && 1 < u2)
            {
                return 0;
            }

            // 2 intersections
            if (0 < u1 && u1 < u2 && u2 < 1)
            {
                return 3;
            }

            // 1 intersection
            if (0 < u1 && u1 < 1)
            {
                return 1;
            }
            return 2;*/
        }

        public static void MergeCloseVerts(List<Vector2> points, float maxDistance)
        {
            float maxDistSq = maxDistance * maxDistance;
            for (int i = 0, j = points.Count - 1; i < points.Count && points.Count > 3; j = i, i++)
            {
                float dist = (points[i] - points[j]).sqrMagnitude;

                if (dist <= maxDistSq)
                {
                    Vector2 avg = (points[i] + points[j]) / 2f;
                    points[j] = avg;
                    points.RemoveAt(i);

                    MergeCloseVerts(points, maxDistance);
                    return;
                }
            }
        }

        public static List<Vector2> SimplifyContour(List<Vector2> points, float tolerance)
        {
            if (points == null || points.Count < 3)
                return points;

            List<int> pointIndeciesToKeep = new List<int>();

            int firstPoint = 0;

            //find furthest point to first point
            int lastPoint = FurthestPointFrom(points, firstPoint);

            pointIndeciesToKeep.Add(firstPoint);

            DouglasPeuckerReduction(points, firstPoint, lastPoint,
            tolerance, pointIndeciesToKeep);

            pointIndeciesToKeep.Add(lastPoint);

            DouglasPeuckerReductionReverse(points, lastPoint, firstPoint + points.Count, tolerance, pointIndeciesToKeep);


            
            if (pointIndeciesToKeep.Count < 3)
            {
#if PBDEBUG
                Debug.LogWarning("Simplification failed!");
#endif
                return points;
            }
            List<Vector2> returnPoints = new List<Vector2>();
            foreach (int index in pointIndeciesToKeep)
            {
                returnPoints.Add(points[index]);
            }
#if PBDEBUG
            Debug.Log(string.Format("Reduced vert count from {0}, to {1} with douglas peucker.", points.Count, returnPoints.Count));
#endif
            return returnPoints;
        }

        private static void DouglasPeuckerReduction(List<Vector2> points, int firstPoint, int lastPoint, float tolerance, List<int> pointsToKeep)
        {
            float dmax = tolerance;
            int index = -1;

            for (int i = firstPoint + 1; i < lastPoint; i++)
            {
                float distance = PerpendicularDistance
                    (points[firstPoint], points[lastPoint], points[i]);
                if (distance > dmax)
                {
                    dmax = distance;
                    index = i;
                }
            }

            if (index != -1)
            {
                //Add the largest point that exceeds the tolerance
                DouglasPeuckerReduction(points, firstPoint, index, tolerance, pointsToKeep);
                pointsToKeep.Add(index);
                DouglasPeuckerReduction(points, index, lastPoint, tolerance, pointsToKeep);
            }
        }

        private static void DouglasPeuckerReductionReverse(List<Vector2> points, int firstPoint, int lastPoint, float tolerance, List<int> pointsToKeep)
        {
            float dmax = tolerance;
            int index = -1;
            int workingLastPoint = lastPoint >= points.Count ? lastPoint - points.Count : lastPoint;

            for (int i = firstPoint + 1; i < lastPoint; i++)
            {
                float distance = PerpendicularDistance
                    (points[firstPoint], points[workingLastPoint], points[i >= points.Count ? i - points.Count : i]);
                if (distance > dmax)
                {
                    dmax = distance;
                    index = i;
                }
            }

            if (index != -1)
            {
                //Add the largest point that exceeds the tolerance
                DouglasPeuckerReductionReverse(points, firstPoint, index, tolerance, pointsToKeep);
                pointsToKeep.Add(index >= points.Count ? index - points.Count : index);
                DouglasPeuckerReductionReverse(points, index, lastPoint, tolerance, pointsToKeep);
            }
        }

        private static int FurthestPointFrom(List<Vector2> points, int index)
        {
            Vector2 v = points[index];
            float maxDistanceSq = 0;
            int furthestIndex = -1;

            for (int i = 0; i < points.Count; i++)
            {
                if (i == index)
                    continue;

                float dist = (v - points[i]).sqrMagnitude;
                if (dist > maxDistanceSq)
                {
                    furthestIndex = i;
                    maxDistanceSq = dist;
                }
            }
            return furthestIndex;
        }

        private static float PerpendicularDistance(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = (b - a).normalized;
            Vector2 ap = p - a;

            float dot = Vector2.Dot(ab, ap);
            Vector2 proj = ap - dot * ab;
            return proj.magnitude;
        }

        public static void RemoveCollinearEdges(List<Vector2> points)
        {
            if (points.Count <= 3)
                return;

            for (int i = 0, j = points.Count - 1, k = points.Count - 2; i < points.Count; k = j, j = i, i++)
            {
                while (SlopesEqual(points[k], points[j], points[j], points[i]))
                {
                    points.RemoveAt(j);
                }
            }
        }

        public static bool SlopesEqual(Vector2 a, Vector2 b, Vector2 x, Vector2 y)
        {
            return Vector2.Dot(b - a, y - x) == 0;
        }

        public static bool ContainsPoint(IEnumerable<Vector2> path, Vector2 lastPointInPath, Vector2 point)
        {
            bool c = false;
            foreach (var p in path)
            {
                if (((p.y > point.y) != (lastPointInPath.y > point.y)) &&
                 (point.x < (lastPointInPath.x - p.x) * (point.y - p.y) / (lastPointInPath.y - p.y) + p.x))
                    c = !c;

                lastPointInPath = p;
            }
            return c;
        }

        /*
         * angle in degrees
         */
        public static Vector2[] RotateRectangle(Rect rect, float angle)
        {
            Vector2[] rotatedCorners = new Vector2[4];
            Vector2 center = rect.center;
            Rect boundingRect = new Rect(center, Vector2.zero);

            float rotationInRad = Mathf.Deg2Rad * angle;
            float angleCos = Mathf.Cos(rotationInRad);
            float angleSin = Mathf.Sin(rotationInRad);

            rotatedCorners[0] = new Vector2(
                ((rect.xMin - center.x) * angleCos) - ((rect.yMin - center.y) * angleSin),
                ((rect.xMin - center.x) * angleSin) + ((rect.yMin - center.y) * angleCos));
           
            rotatedCorners[1] = new Vector2(
                ((rect.xMax - center.x) * angleCos) - ((rect.yMin - center.y) * angleSin),
                ((rect.xMax - center.x) * angleSin) + ((rect.yMin - center.y) * angleCos));
            rotatedCorners[2] = -rotatedCorners[0];
            rotatedCorners[3] = -rotatedCorners[2];

            for (int i = 0; i < 4; i++)
                rotatedCorners[i] += center;

            return rotatedCorners;
        }

        public static bool IsAngleBetweenAngles(float from, float to, float angle)
        {
            angle = angle < 0 ? angle + 360 : angle;

            to = to - from;
            to = to < 0 ? to + 360 : to;

            angle = angle - from;
            angle = angle < 0 ? angle + 360 : angle;

            return angle < to;
        }
    }
}
