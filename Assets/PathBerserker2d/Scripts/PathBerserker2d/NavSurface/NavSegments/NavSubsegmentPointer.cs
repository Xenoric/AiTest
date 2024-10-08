using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Points to a subsection on a segment. It has a start point on the target segment and a length.
    /// </summary>
    public struct NavSubsegmentPointer : IEquatable<NavSubsegmentPointer>
    {
        public static NavSubsegmentPointer Invalid { get { return new NavSubsegmentPointer(null, 0, 0, 0); } }

        public readonly float t;
        public readonly float length;
        public readonly int proxyDataIndex;
        public readonly NavSurface surface;
        public readonly int bakeIteration;

        public NavSubsegmentPointer(NavSurface surface, int proxyDataIndex, float t, float length)
        {
            this.surface = surface;
            this.proxyDataIndex = proxyDataIndex;
            this.t = t;
            this.length = length;
            this.bakeIteration = surface != null ? surface.BakeIteration : -1;
        }

        public static bool operator ==(NavSubsegmentPointer a, NavSubsegmentPointer b)
        {
            return a.surface == b.surface && a.proxyDataIndex == b.proxyDataIndex && a.t == b.t && a.length == b.length;
        }

        public static bool operator !=(NavSubsegmentPointer a, NavSubsegmentPointer b)
        {
            return a.surface != b.surface || a.proxyDataIndex != b.proxyDataIndex || a.t != b.t || a.length != b.length;
        }

        public override bool Equals(object obj)
        {
            return obj is NavSubsegmentPointer && Equals((NavSubsegmentPointer)obj);
        }

        public bool Equals(NavSubsegmentPointer other)
        {
            return t == other.t &&
                   length == other.length &&
                   proxyDataIndex == other.proxyDataIndex &&
                   EqualityComparer<NavSurface>.Default.Equals(surface, other.surface);
        }

        public bool IsInvalid()
        {
            return surface == null || bakeIteration != surface.BakeIteration;
        }

        public override int GetHashCode()
        {
            var hashCode = -1731056371;
            hashCode = hashCode * -1521134295 + t.GetHashCode();
            hashCode = hashCode * -1521134295 + length.GetHashCode();
            hashCode = hashCode * -1521134295 + proxyDataIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<NavSurface>.Default.GetHashCode(surface);
            return hashCode;
        }
    }
}
