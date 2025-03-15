using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    public static class Geometry
    {
        public static float DistancePointLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            return Vector2.Distance(p, ProjectPointOnLineSegment(p, a, b));
        }

        /// <summary>
        /// Finds closest point to p on line a -> b
        /// </summary>
        public static Vector2 ProjectPointOnLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 dir = b - a;
            float l2 = dir.sqrMagnitude;
            if (l2 == 0.0)
                return a;

            float t = Mathf.Clamp01(Vector2.Dot(p - a, dir) / l2);
            Vector2 projection = a + t * dir;
            return projection;
        }

        /// <summary>
        /// Project point on line.
        /// </summary>
        /// <param name="p">Point to proejct</param>
        /// <param name="a">Point on line</param>
        /// <param name="dir">Line tangent. Must be normalized!</param>
        /// <returns>Closets point on line to p</returns>
        public static Vector2 ProjectPointOnLine(Vector2 p, Vector2 a, Vector2 dir)
        {
            float t = Vector2.Dot(p - a, dir);
            return a + t * dir;
        }

        public static bool IsPointOnPositiveSideOfLine(Vector2 point, Vector2 linePointA, Vector2 normal)
        {
            return Vector2.Dot(point - linePointA, normal) >= 0;
        }

        /// <summary>
        /// Returns a rect that encapsulates the passed in rect after being transformed.
        /// </summary>
        public static Rect TransformBoundingRect(Rect bounds, Matrix4x4 transformation)
        {
            // bounds -> extract 2 diagonal corners
            // transform corners
            Vector2 a = transformation.MultiplyPoint3x4(bounds.min);
            Vector2 b = transformation.MultiplyPoint3x4(bounds.max);
            Vector2 c = transformation.MultiplyPoint3x4(new Vector2(bounds.xMin, bounds.yMax));
            Vector2 d = transformation.MultiplyPoint3x4(new Vector2(bounds.xMax, bounds.yMin));

            // create new box containing corners
            Vector2 min = Vector2.Min(d, Vector2.Min(c, Vector2.Min(a, b)));
            Vector2 max = Vector2.Max(d, Vector2.Max(c, Vector2.Max(a, b)));

            return new Rect(min, max - min);
        }

        public static Rect EnlargeRect(Rect rect, float amount)
        {
            return new Rect(rect.position - Vector2.one * amount, rect.size + Vector2.one * amount * 2);
        }
    }
}
