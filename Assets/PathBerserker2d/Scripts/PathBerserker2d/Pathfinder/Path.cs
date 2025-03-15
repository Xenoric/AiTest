using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// A path ready to be traversed
    /// </summary>
    public class Path : IEnumerator<PathSegment>
    {
        public PathSegment Current { get; private set; }
        object IEnumerator.Current => Current;

        public PathSegment NextSegment { get { return Current.Next; } }
        public bool HasNext { get { return Current.Next != null; } }

        public Vector2 Goal => lastSeg.LinkStart;
        public Vector2 Start => start;
        public readonly float totalCosts;

        private readonly PathSegment firstSeg;
        private readonly PathSegment lastSeg;
        private readonly int segmentCount;
        private readonly Vector2 start;
        private int remainingSegmentCount;

        internal Path(PathSegment firstSeg, PathSegment lastSeg, Vector2 start, int segCount, float totalCosts)
        {
            this.firstSeg = firstSeg;
            this.lastSeg = lastSeg;
            this.Current = firstSeg;
            this.segmentCount = segCount;
            this.remainingSegmentCount = segCount;
            this.totalCosts = totalCosts;
            this.start = start;
        }

        public override string ToString()
        {
            return string.Format("Path (VertCount: {0}, Costs: {1})", segmentCount, totalCosts);
        }

        public void Dispose()
        {

        }

        /// <summary>
        /// Advanced the path by 1 segment.
        /// </summary>
        public bool MoveNext()
        {
            if (Current.Next != null)
            {
                Current = Current.Next;
                remainingSegmentCount--;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            remainingSegmentCount = segmentCount;
            Current = firstSeg;
        }

        /// <summary>
        /// Creates a list of all path points. Current enumerator progress is ignored.
        /// List starts with path start and ends with path goal. Corner links will result in the same point being enumerated twice in a row.
        /// </summary>
        public List<Vector2> AllPathPoints()
        {
            List<Vector2> pathPoints = new List<Vector2>(segmentCount * 2 + 2);
            pathPoints.Add(Start);

            var seg = firstSeg;
            while (seg.Next != null)
            {
                pathPoints.Add(seg.LinkStart);
                pathPoints.Add(seg.LinkEnd);
                seg = seg.Next;
            }
            pathPoints.Add(Goal);
            return pathPoints;
        }

        /// <summary>
        /// Creates a list of all path points, starting from current segment.
        /// List starts with current.linkstart and ends with path goal. If these points are equivalent, only goal will be returned. Corner links will result in the same point being enumerated twice in a row.
        /// </summary>
        public List<Vector2> RemainingPathPoints()
        {
            List<Vector2> pathPoints = new List<Vector2>(remainingSegmentCount * 2 + 2);
            pathPoints.Add(Current.LinkStart);

            var seg = Current;
            while (seg.Next != null)
            {
                pathPoints.Add(seg.LinkStart);
                pathPoints.Add(seg.LinkEnd);
                seg = seg.Next;
            }

            if (Current.Next != null)
                pathPoints.Add(Goal);
            return pathPoints;
        }
    }
}
