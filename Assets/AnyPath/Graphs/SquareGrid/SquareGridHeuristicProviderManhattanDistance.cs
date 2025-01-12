using AnyPath.Native;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.SquareGrid
{
    /// <summary>
    /// Heuristic provider for the <see cref="SquareGrid"/> that only calculates manhattan distance.
    /// This is the most performant way if your grid only supports four movement directions.
    /// Do not use when the grid type is set to <see cref="SquareGridType.EightNeighbours"/>,
    /// use <see cref="SquareGridHeuristicProvider"/> or
    /// </summary>
    public struct SquareGridHeuristicProviderManhattanDistance : IHeuristicProvider<SquareGridCell>
    {
        // Our current goal, set by A* before it begins
        private SquareGridCell goal;

        public void SetGoal(SquareGridCell goal)
        {
            this.goal = goal;
        }
        
        /// <summary>
        /// Returns the travel distance between two cells on the grid.
        /// </summary>
        public float Heuristic(SquareGridCell a) => dot(abs(a.Position - goal.Position), 1f);
    }
}