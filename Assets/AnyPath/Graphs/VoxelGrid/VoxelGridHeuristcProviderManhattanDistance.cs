using AnyPath.Native;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.VoxelGrid
{
    /// <summary>
    /// Only use this heuristic provider when your grid supports straight movements.
    /// <see cref="VoxelGrid.Foward_Right_Back_Left_Up_Down"/>.
    /// Will produce incorrect results otherwise and won't yield the shortest path.
    /// </summary>
    public struct VoxelGridHeuristicProviderManhattanDistance : IHeuristicProvider<VoxelGridCell>
    {
        private VoxelGridCell goal;

        [ExcludeFromDocs]
        public void SetGoal(VoxelGridCell goal)
        {
            this.goal = goal;
        }

        [ExcludeFromDocs]
        public float Heuristic(VoxelGridCell a) => csum(abs(a.Position - goal.Position));
    }
}