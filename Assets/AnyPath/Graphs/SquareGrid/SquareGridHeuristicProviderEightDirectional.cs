using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.SquareGrid
{
    /// <summary>
    /// Heuristic provider for the <see cref="SquareGrid"/> that supports eight directions.
    /// </summary>
    public struct SquareGridHeuristicProviderEightDirectional : IHeuristicProvider<SquareGridCell>
    {
        // Our current goal, set by A* before it begins
        private SquareGridCell goal;

        [ExcludeFromDocs]
        public void SetGoal(SquareGridCell goal)
        {
            this.goal = goal;
        }
        
        /// <summary>
        /// Returns the travel distance between two cells on the grid.
        /// </summary>
        public float Heuristic(SquareGridCell a)
        {
            // fast heuristic for 8 directional grid
            // http://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html
            int2 delta = abs(a.Position - goal.Position);
            return csum(delta) + (SQRT2 - 2) * cmin(delta);
        }
    }
}