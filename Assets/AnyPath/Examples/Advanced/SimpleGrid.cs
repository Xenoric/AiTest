using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AnyPath.Examples
{
    /// <summary>
    /// An example of a simple 2D grid that uses a 1D array as backing
    /// </summary>
    public struct SimpleGrid : IGraph<int2>
    {
        private readonly int width;
        private readonly int height;
        private NativeArray<bool> cells;
        
        public SimpleGrid(int width, int height, Allocator allocator)
        {
            this.width = width;
            this.height = height;
            this.cells = new NativeArray<bool>(width * height, allocator);
        }

        /// <summary>
        /// Sets a wall at a given position. This cell will be unwalkable.
        /// </summary>
        public void SetWall(int2 position) => cells[PositionToIndex(position)] = true;
        
        /// <summary>
        /// Clears the wall at a given position. The cell will be walkable.
        /// </summary>
        public void ClearWall(int2 position) => cells[PositionToIndex(position)] = false;
        
        /// <summary>
        /// Returns wether there is a wall at a given position.
        /// </summary>
        public bool IsWall(int2 position) => cells[PositionToIndex(position)];

        /// <summary>
        /// Converts a position into a 1D index into the backing array
        /// </summary>
        private int PositionToIndex(int2 position) => position.x + position.y * width;

        /// <summary>
        /// Returns wether a position is in bounds of the grid
        /// </summary>
        public bool InBounds(int2 position) => position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;

        private static readonly int2[] directions =
        {
            new int2(0, 1), new int2(1, 0), new int2(0, -1), new int2(-1, 0),
        };
        
        /// <summary>
        /// AnyPath will call this function many times during a pathfinding request. It should return the neighbouring cells from node
        /// that are walkable.
        /// </summary>
        public void Collect(int2 node, ref NativeList<Edge<int2>> edgeBuffer)
        {
            // Loop over the four directions
            for (int i = 0; i < directions.Length; i++)
            {
                var neighbour = node + directions[i];
                
                // Skip this node if it's out of bounds, or if it is a wall
                if (!InBounds(neighbour) || IsWall(neighbour))
                    continue;
                
                // This neighbour is a valid location to go to next, add it to the edge buffer with a cost of one
                edgeBuffer.Add(new Edge<int2>(neighbour, 1));
            }
        }
        
        // Dispose of the inner NativeArray:
        public void Dispose() => cells.Dispose();
        public JobHandle Dispose(JobHandle inputDeps) => cells.Dispose(inputDeps);
    }

    /// <summary>
    /// This struct will be used to provde a heuristic value for our pathfinding queries
    /// </summary>
    public struct SimpleGridHeuristc : IHeuristicProvider<int2>
    {
        private int2 goal;

        public void SetGoal(int2 goal)
        {
            this.goal = goal;
        }

        public float Heuristic(int2 x)
        {
            // Manhattan distance
            return math.dot(math.abs(x - goal), 1f);
        }
    }
}