using System;
using UnityEngine;

namespace PathBerserker2d
{
    [System.Serializable]
    internal class LineSegment
    {
        public Vector2 Start { get { return start; } }
        public Vector2 End { get { return GetPositionAlongSegment(Length); } }
        public Vector2 Normal { get { return new Vector2(-Tangent.y, Tangent.x); } }
        public Vector2 Tangent { get { return dirNorm; } }
        public float Length { get { return length; } }

        public Rect AABB
        {
            get
            {
                Vector2 min = Vector2.Min(Start, End);
                Vector2 max = Vector2.Max(Start, End);
                return new Rect(min, max - min);
            }
        }

        [SerializeField]
        private Vector2 start;
        [SerializeField]
        private float length;
        [SerializeField]
        private Vector2 dirNorm;

        public LineSegment(Vector2 start, Vector2 dirNorm, float length)
        {
            this.start = start;
            this.length = length;
            this.dirNorm = dirNorm;
        }

        public LineSegment(LineSegment other)
        {
            this.start = other.start;
            this.length = other.length;
            this.dirNorm = other.dirNorm;
        }

        public Vector2 GetPositionAlongSegment(float t)
        {
            return Start + Tangent * t;
        }

        public Vector2 ProjectPointOnSegment(Vector2 lPoint)
        {
            return Start + DistanceOfPointAlongSegment(lPoint) * Tangent;
        }

        public float DistanceOfPointAlongSegment(Vector2 lPoint)
        {
            Vector2 ap = lPoint - Start;
            return Mathf.Clamp(Vector2.Dot(Tangent, ap), 0, Length);
        }

        public float PointDistance(Vector2 position)
        {
            return Vector2.Distance(position, ProjectPointOnSegment(position));
        }
    }
}
