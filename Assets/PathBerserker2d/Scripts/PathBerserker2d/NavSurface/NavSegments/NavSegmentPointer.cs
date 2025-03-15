using System;
using System.Collections.Generic;

namespace PathBerserker2d
{
    /// <summary>
    /// Points to a segment.
    /// </summary>
    public struct NavSegmentPointer : IEquatable<NavSegmentPointer>
    {
        public static NavSegmentPointer Invalid { get { return new NavSegmentPointer(null, 0); } }

        public readonly int proxyDataIndex;
        public readonly NavSurface surface;

        public NavSegmentPointer(NavSurface surface, int proxyDataIndex)
        {
            this.surface = surface;
            this.proxyDataIndex = proxyDataIndex;
        }

        public static bool operator ==(NavSegmentPointer a, NavSegmentPointer b)
        {
            return a.surface == b.surface && a.proxyDataIndex == b.proxyDataIndex;
        }

        public static bool operator !=(NavSegmentPointer a, NavSegmentPointer b)
        {
            return a.surface != b.surface || a.proxyDataIndex != b.proxyDataIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is NavSegmentPointer && Equals((NavSegmentPointer)obj);
        }

        public bool Equals(NavSegmentPointer other)
        {
            return proxyDataIndex == other.proxyDataIndex &&
                   EqualityComparer<NavSurface>.Default.Equals(surface, other.surface);
        }

        public override int GetHashCode()
        {
            var hashCode = 1340906973;
            hashCode = hashCode * -1521134295 + proxyDataIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<NavSurface>.Default.GetHashCode(surface);
            return hashCode;
        }
    }
}
