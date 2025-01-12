using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace AnyPath.Graphs.Extra
{
    /// <summary>
    /// 2D line segment with a few math utility functions
    /// </summary>
    public struct Line2D
    {
        /// <summary>
        /// Endpoint of the line segment
        /// </summary>
        public float2 a { get; set; }
        
        /// <summary>
        /// Endpoint of the line segment
        /// </summary>
        public float2 b { get; set; }
        
        public Line2D(float2 a, float2 b)
        {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        /// Shorthand for the center of the line
        /// </summary>
        public float2 Center => .5f * (a + b);

        public float2 GetDirection() => math.normalize(b - a);

        public float GetLength() => math.distance(a, b);
        
        /// <summary>
        /// Check if this line segment intersects with a ray
        /// </summary>
        /// <param name="ray">The ray to test against</param>
        /// <param name="pos">The position the line was hit</param>
        /// <returns>Wether the line was hit by a ray</returns>
        public bool IntersectsRay(Ray2D ray, out float2 pos)
        {
            float t, u;
            pos = float2.zero;

            var r = this.b - a;
            var s = ray.direction;
            var rxs = cross2D(r, s); //r.Cross(s);
            var qpxr = cross2D((float2)ray.origin - a, r); //(q - p).Cross(r);

            // If r x s = 0 and (q - p) x r = 0, then the two lines are collinear.
            if (IsZero(rxs) && IsZero(qpxr))
            {
                // 2. If neither 0 <= (q - p) * r = r * r nor 0 <= (p - q) * s <= s * s
                // then the two lines are collinear but disjoint.
                // No need to implement this expression, as it follows from the expression above.
                return false;
            }

            // 3. If r x s = 0 and (q - p) x r != 0, then the two lines are parallel and non-intersecting.
            if (IsZero(rxs) && !IsZero(qpxr))
                return false;

            // t = (q - p) x s / (r x s)
            t = cross2D((float2)ray.origin - a, s) / rxs;  //(q - p).Cross(s)/rxs;

            // u = (q - p) x r / (r x s)
            u = cross2D((float2)ray.origin - a, r) / rxs; //  (q - p).Cross(r)/rxs;

            // 4. If r x s != 0 and 0 <= t <= 1 and 0 <= u <= 1
            // the two line segments meet at the point p + t r = q + u s.
            if (!IsZero(rxs) && (0 <= t && t <= 1) && (0 <= u))
            {
                // An intersection was found.
                pos = a + t * r;
                return true;
            }

            // 5. Otherwise, the two line segments are not parallel but do not intersect.
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float cross2D(float2 v1, float2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZero(float2 x) => math.all(math.abs(x) <= 0.000001f);
        
        /// <summary>
        /// Returns the closest point on this line from a point
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float2 GetClosestPoint(float2 pos)
        {
            return math.lerp(a, b, GetClosestPositionT(pos));
        }
        
        /// <summary>
        /// Returns a value between 0 and 1 describing how far along the line the closest point on this line is to a given point 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetClosestPositionT(float2 pos)
        {
            float2 ba = b - a;
            return math.saturate(math.dot(pos - a, ba) / math.dot(ba, ba));
        }
    }
}