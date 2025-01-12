using AnyPath.Native;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.VoxelGrid
{
    /// <summary>
    /// Voxel grid heuristic provider that works for all directions.
    /// </summary>
    public struct VoxelGridHeuristicProvider : IHeuristicProvider<VoxelGridCell>
    {
        private VoxelGridCell goal;

        [ExcludeFromDocs]
        public void SetGoal(VoxelGridCell goal)
        {
            this.goal = goal;
        }

        [ExcludeFromDocs]
        public float Heuristic(VoxelGridCell a) => distance(a.Position, goal.Position);
    }
}