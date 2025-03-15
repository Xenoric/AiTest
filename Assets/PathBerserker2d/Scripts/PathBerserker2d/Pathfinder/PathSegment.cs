using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Encapsulates the movement on a segment to a link and the traversal of that link.
    /// </summary>
    /// <remarks>
    /// Only the last PathSegment does not have a link.
    /// </remarks>
    public class PathSegment
    {
        public PathSegment Next { get; internal set; }
        /// <summary>
        /// World position of the link start. May change from frame to frame, if corresponding NavSurface moves.
        /// </summary>
        public Vector2 LinkStart { get { return owner.LocalToWorld(linkStart); } }
        /// <summary>
        /// World position of the link end. May change from frame to frame, if corresponding NavSurface moves.
        /// </summary>
        public Vector2 LinkEnd { get { return Next.owner.LocalToWorld(linkEnd); } }
        /// <summary>
        /// Segments normal. May change from frame to frame, if corresponding NavSurface moves.
        /// </summary>
        public Vector2 Normal => owner.LocalToWorldMatrix.MultiplyVector(cluster.Normal);
        /// <summary>
        /// Segments tangent. May change from frame to frame, if corresponding NavSurface moves.
        /// </summary>
        public Vector2 Tangent => owner.LocalToWorldMatrix.MultiplyVector(cluster.Tangent);
        /// <summary>
        /// Point on segment that together with Tangent defines the segments line equation. May change from frame to frame, if corresponding NavSurface moves.
        /// </summary>
        public Vector2 Point => owner.LocalToWorldMatrix.MultiplyPoint3x4(cluster.Start);

        public readonly INavLinkInstance link;
        public readonly NavSurface owner;
        private Vector2 linkStart;
        private Vector2 linkEnd;
        internal NavGraphNodeCluster cluster;

        internal PathSegment(Vector2 linkStart, Vector2 linkEnd, NavSurface owner, INavLinkInstance link, NavGraphNodeCluster cluster)
        {
            this.linkStart = linkStart;
            this.linkEnd = linkEnd;
            this.owner = owner;
            this.link = link;
            this.cluster = cluster;
        }

        /// <summary>
        /// Get the NavTag vector at a distance along the segment.
        /// </summary>
        /// <param name="t">Distance along segment.</param>
        /// <returns>Integer with bits set to the existence of the corresponding nav tag at that position.</returns>
        public int GetTagVector(float t)
        {
            return cluster.GetNavTagVector(t);
        }

        /// <summary>
        /// Like GetTagVector, but works by projecting the parameter on the segment.
        /// </summary>
        /// <returns>Integer with bits set to the existence of the corresponding nav tag at that position.</returns>
        public int GetTagVector(Vector2 pos)
        {
            return cluster.GetNavTagVector(owner.WorldToLocal(pos));
        }

        /// <summary>
        /// Projects a position on the segment and returns its distance from the segment start Result may change from frame to frame, if corresponding NavSurface moves.
        /// </summary>
        public float DistanceAlongSegment(Vector2 pos)
        {
            return Vector2.Dot(cluster.Tangent, owner.WorldToLocal(pos) - cluster.Start);
        }
    }
}