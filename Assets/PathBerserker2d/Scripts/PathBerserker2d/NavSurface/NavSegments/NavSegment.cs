using UnityEngine;

namespace PathBerserker2d
{
    [System.Serializable]
    internal class NavSegment : LineSegmentWithClearance, System.IEquatable<NavSegment>
    {
        public NavSurface Owner { get { return owner; } }

        [SerializeField]
        private NavSurface owner;

        public NavSegment(NavSurface owner, Vector2 start, Vector2 dirNorm, float length, float[] cellClearances) : base(start, dirNorm, length, cellClearances)
        {
            this.owner = owner;
        }

        public bool Equals(NavSegment other)
        {
            return ReferenceEquals(other, this);
        }

        public override string ToString()
        {
            return $"NavSegment({Start} -{Tangent}> {End})";
        }
    }
}