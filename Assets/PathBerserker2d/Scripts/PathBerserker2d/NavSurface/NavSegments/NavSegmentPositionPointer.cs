using System;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Points to a position on a segment.
    /// </summary>
    public struct NavSegmentPositionPointer : IEquatable<NavSegmentPositionPointer>
    {
        public static NavSegmentPositionPointer Invalid { get { return new NavSegmentPositionPointer(null, null, 0); } }

        public Vector2 Position => cluster.owner.LocalToWorld.MultiplyPoint3x4(cluster.GetPositionAlongSegment(t));
        public Vector2 Normal => surface.LocalToWorldMatrix.MultiplyVector(cluster.Normal);

        internal readonly float t;
        internal readonly NavGraphNodeCluster cluster;
        internal readonly NavSurface surface;
        // used to detect if the surface has changed
        internal readonly int bakeIteration;

        internal NavSegmentPositionPointer(NavSurface surface, NavGraphNodeCluster cluster, float t)
        {
            this.surface = surface;
            this.cluster = cluster;
            this.t = t;
            this.bakeIteration = surface != null ? surface.BakeIteration : -1;
        }

        public static bool operator ==(NavSegmentPositionPointer a, NavSegmentPositionPointer b)
        {
            return a.surface == b.surface && a.cluster == b.cluster && a.t == b.t;
        }

        public static bool operator !=(NavSegmentPositionPointer a, NavSegmentPositionPointer b)
        {
            return a.surface != b.surface || a.cluster != b.cluster || a.t != b.t;
        }

        [Obsolete("Use the property Position instead.")]
        public Vector2 GetPosition()
        {
            return cluster.owner.LocalToWorld.MultiplyPoint3x4(cluster.GetPositionAlongSegment(t));
        }

        public bool IsInvalid()
        {
            return surface == null || bakeIteration != surface.BakeIteration;
        }

        public bool IsValid()
        {
            return surface != null && surface.BakeIteration == bakeIteration;
        }

        public bool Equals(NavSegmentPositionPointer other)
        {
            return other != null && t == other.t &&
                   cluster == other.cluster &&
                   surface == other.surface;
        }


        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                var p = (NavSegmentPositionPointer)obj;
                return Equals(p);
            }
        }

        public override string ToString()
        {
            if (IsInvalid())
                return $"NSP is invalid";
            return $"NSP points at " + Position;
        }

        public override int GetHashCode()
        {
            int hashCode = 1774642579;
            hashCode = hashCode * -1521134295 + t.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<NavGraphNodeCluster>.Default.GetHashCode(cluster);
            hashCode = hashCode * -1521134295 + EqualityComparer<NavSurface>.Default.GetHashCode(surface);
            hashCode = hashCode * -1521134295 + bakeIteration.GetHashCode();
            return hashCode;
        }
    }
}
