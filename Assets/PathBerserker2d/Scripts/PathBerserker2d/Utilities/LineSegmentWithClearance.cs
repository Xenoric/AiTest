using System;
using UnityEngine;

namespace PathBerserker2d
{
    [System.Serializable]
    internal class LineSegmentWithClearance : LineSegment
    {
        public int CellCount { get { return cellClearances.Length; } }
        public int NextSegmentIndex { get => nextSegIndex; internal set => nextSegIndex = value; }
        public int PrevSegmentIndex { get => prevSegIndex; internal set => prevSegIndex = value; }

        public bool HasNext => nextSegIndex >= 0;
        public bool HasPrev => prevSegIndex >= 0;

        [SerializeField]
        protected float[] cellClearances;
        [SerializeField]
        private int prevSegIndex = -1;
        [SerializeField]
        private int nextSegIndex = -1;

        public LineSegmentWithClearance(Vector2 start, Vector2 dirNorm, float length, float[] cellClearances) : base(start, dirNorm, length)
        {
            this.cellClearances = cellClearances;
        }

        public LineSegmentWithClearance(LineSegmentWithClearance other) : base(other)
        {
            this.prevSegIndex = other.prevSegIndex;
            this.nextSegIndex = other.nextSegIndex;
            this.cellClearances = (float[])other.cellClearances.Clone();
        }

        public float GetClearanceAlongSegment(float dist)
        {
            return cellClearances[CellIndexOf(dist)];
        }

        public bool DoesClearanceAllowTraversal(float startT, float endT, float agentHeight)
        {
            return GetMinClearanceTo(startT, endT) < agentHeight;
        }

        private int CellIndexOf(float t)
        {
            if (t >= Length)
                return cellClearances.Length - 1;
            else if (t <= 0)
                return 0;
            else
                return Mathf.FloorToInt(t * (cellClearances.Length / Length));
        }

        public float GetMinClearanceTo(float from, float to)
        {
            int cellFrom = CellIndexOf(from);
            int cellTo = CellIndexOf(to);

            int dir = cellTo - cellFrom;
            if (dir == 0)
            {
                // same cell
                return cellClearances[cellFrom];
            }
            else if (dir < 0)
            {
                float minClearance = cellClearances[cellFrom];
                for (int iCell = cellFrom - 1; iCell >= cellTo; iCell--)
                {
                    if (minClearance > cellClearances[iCell])
                        minClearance = cellClearances[iCell];
                }
                return minClearance;
            }
            else
            {
                float minClearance = cellClearances[cellFrom];
                for (int iCell = cellFrom + 1; iCell <= cellTo; iCell++)
                {
                    if (minClearance > cellClearances[iCell])
                        minClearance = cellClearances[iCell];
                }
                return minClearance;
            }
        }

        public float[] CloneCellClearances()
        {
            return (float[])cellClearances.Clone();
        }
    }
}
