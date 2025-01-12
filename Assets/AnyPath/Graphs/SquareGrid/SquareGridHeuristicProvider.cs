using System;
using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.SquareGrid
{
    /// <summary>
    /// Heuristic provider for the <see cref="SquareGrid"/> that supports both <see cref="SquareGridType.FourNeighbours"/> and
    /// <see cref="SquareGridType.EightNeighbours"/>
    /// </summary>
    /// <remarks>
    /// If you are 100% sure about the modes of direction of your grid,
    /// use Manhattan Distance or Eight Directional for better performance.
    /// </remarks>
    public struct SquareGridHeuristicProvider : IHeuristicProvider<SquareGridCell>
    {
        /// <summary>
        /// The type of grid. This should match the <see cref="SquareGrid"/> type.
        /// </summary>
        public SquareGridType gridType;

        // Our current goal, set by A* before it begins
        private SquareGridCell goal;

        /// <summary>
        /// Create a heuristic provider for a grid.
        /// </summary>
        /// <param name="gridType">he type of grid. This should match the <see cref="SquareGrid"/> type.</param>
        public SquareGridHeuristicProvider(SquareGridType gridType)
        {
            this.gridType = gridType;
            this.goal = default;
        }

        [ExcludeFromDocs]
        public void SetGoal(SquareGridCell goal)
        {
            this.goal = goal;
        }
        
        /// <summary>
        /// Returns the travel distance between two cells on the grid.
        /// </summary>
        [ExcludeFromDocs]
        public float Heuristic(SquareGridCell a)
        {
            switch (gridType)
            {
                case SquareGridType.FourNeighbours:
                    // manhattan distance
                    return dot(abs(a.Position - goal.Position), 1f);
                default:
                    // fast heuristic for 8 directional grid
                    // http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
                    int2 delta = abs(a.Position - goal.Position);
                    return csum(delta) + (SQRT2 - 2) * cmin(delta);
            }
        }
    }
}