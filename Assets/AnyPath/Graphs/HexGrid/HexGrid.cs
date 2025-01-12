using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Internal;
using static Unity.Mathematics.math;


/*
 * For a great reference:
 * https://www.redblobgames.com/grids/hexagons
 */

namespace AnyPath.Graphs.HexGrid
{
    /// <summary>
    /// A simple hexagonal grid structure that can be used for pathfinding queries. Each cell can have an additional cost and flags
    /// associated with it.
    /// </summary>
    /// <remarks>The cells are stored in a hash map, which is more memory efficient for sparse grids. For raw performance, this grid can
    /// be easily modified to using an array instead.</remarks>
    public struct HexGrid : IGraph<HexGridCell>
    {
        // neighbour lookup table
        private static readonly int2[] directions =
        {
            // Odd-R (Pointy top)
            new int2(1, 0), new int2(0, -1), new int2(-1, -1),
            new int2(-1, 0), new int2(-1, +1), new int2(0, 1),
            // --
            new int2(1, 0), new int2(1, -1), new int2(0, -1),
            new int2(-1, 0), new int2(0, +1), new int2(1, 1),
            
            // Even-R
            new int2(1, 0), new int2(1, -1), new int2(0, -1),
            new int2(-1, 0), new int2(0, +1), new int2(1, 1),
            // --
            new int2(1, 0), new int2(0, -1), new int2(-1, -1),
            new int2(-1, 0), new int2(-1, +1), new int2(0, 1),

            // Odd-Q (Flat top)
            new int2(1, 0), new int2(1, -1), new int2(0, -1),
            new int2(-1, -1), new int2(-1, 0), new int2(0, 1),
            
            // --
            new int2(1, 1), new int2(1, 0), new int2(0, -1),
            new int2(-1, 0), new int2(-1, 1), new int2(0, 1),

            // Even-Q
            new int2(1, 1), new int2(1, 0), new int2(0, -1),
            new int2(-1, 0), new int2(-1, 1), new int2(0, 1),
            
            // --
            new int2(1, 0), new int2(1, -1), new int2(0, -1),
            new int2(-1, -1), new int2(-1, 0), new int2(0, 1),
        };

        private readonly struct EnterCostAndFlags
        {
            public readonly float cost;
            public readonly int flags;

            public EnterCostAndFlags(float cost, int flags)
            {
                this.cost = cost;
                this.flags = flags;
            }
        }
        
        /// <summary>
        /// Type of hexgrid
        /// </summary>
        public readonly HexGridType gridType;
            
        /// <summary>
        /// The boundary -min- position
        /// </summary>
        public readonly int2 min; // must be readonly for Enumerator.

        /// <summary>
        /// The boundary -max- position
        /// </summary>
        public readonly int2 max;

        /// <summary>
        /// Cost+flags per position. Using a NativeArray would be a bit faster, but benchmarking showed that the gains aren't huge.
        /// </summary>
        private NativeHashMap<int2, EnterCostAndFlags> positionToCell;

        /// <summary>
        /// Constructs a new hexagonal grid.
        /// </summary>
        /// <param name="min">Boundary min</param>
        /// <param name="max">Boundary max</param>
        /// <param name="type">Type of hexgrid</param>
        /// <param name="cells">A list containing all cells that need to be set and the cost associated with them.
        /// Use a cost of PositiveInfinity to make a cell unwalkable. Locations that are ommitted in the array will be considered as open.</param>
        /// <param name="allocator"></param>
        public HexGrid(int2 min, int2 max, IReadOnlyList<HexGridCell> cells, Allocator allocator, HexGridType type = HexGridType.UnityTilemap)
        {
            this.min = min;
            this.max = max;
            this.gridType = type;
            this.positionToCell = new NativeHashMap<int2, EnterCostAndFlags>(cells.Count, allocator);
            foreach (var cell in cells)
            {
                positionToCell.Add(cell.Position, new EnterCostAndFlags(cell.EnterCost, cell.Flags));
            }
        }
        
        /// <summary>
        /// Constructs a new hexagonal grid.
        /// </summary>
        /// <param name="min">Boundary min</param>
        /// <param name="max">Boundary max</param>
        /// <param name="type">Type of hexgrid</param>
        /// <param name="allocator"></param>
        public HexGrid(int2 min, int2 max, Allocator allocator, HexGridType type = HexGridType.UnityTilemap)
        {
            this.min = min;
            this.max = max;
            this.gridType = type;
            this.positionToCell = new NativeHashMap<int2, EnterCostAndFlags>(32, allocator);
        }
        
     
        /// <summary>
        /// Collects all neighbouring cells from a given location
        /// </summary>
        /// <param name="node">The location to find the neighbours for</param>
        public void Collect(HexGridCell node, ref NativeList<Edge<HexGridCell>> edgeBuffer)
        {
            // value of enum is 'large' starting offset
            int offset = (int) gridType;

            // now add parity offset
            // determine if number is even (bitwise and with 1 will produce zero)
            // and use it as starting offset for reading neighbours
            switch (gridType)
            {
                case HexGridType.OddR:
                case HexGridType.EvenR:
                    offset += 6 * (node.Position.y & 1);
                    break;
                case HexGridType.OddQ:
                case HexGridType.EvenQ:
                    offset += 6 * (node.Position.x & 1);
                    break;
            }

            for (int i = offset; i < offset + 6; i++)
            {
                int2 nextPos = node + directions[i];
                if (!InBounds(nextPos)) continue;
                
                positionToCell.TryGetValue(nextPos, out var costAndFlags); // return default cell when not present in map
                if (isfinite(costAndFlags.cost))
                    edgeBuffer.Add(new Edge<HexGridCell>(new HexGridCell(nextPos, costAndFlags.cost, costAndFlags.flags), 1 + costAndFlags.cost));
            }
        }

        /// <summary>
        /// Returns wether a certain position is within the bounds of the grid
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InBounds(int2 position) => all(position >= min) && all(position <= max);

        /// <summary>
        /// Returns the cell at a given position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]     
        public HexGridCell GetCell(int2 position)
        {
            positionToCell.TryGetValue(position, out var costAndFlags);
            return new HexGridCell(position, costAndFlags.cost, costAndFlags.flags);
        }

        /// <summary>
        /// Returns wether a cell at a position is open/walkable.
        /// </summary>
        public bool IsOpen(int2 position)
        {
            if (!InBounds(position))
                return false;

            return !positionToCell.TryGetValue(position, out var costAndFlags) ||
                   isfinite(costAndFlags.cost);
        }

        /// <summary>
        /// Sets the cost for a cell.
        /// </summary>
        /// <param name="position">Position to set</param>
        /// <param name="enterCost">Additional cost for walking this cell. Use float.PositiveInfinity to make this cell unwalkable</param>
        /// <param name="flags">Flags for this cell, this can be used in conjunction with <see cref="FlagBitmask{TNode}"/> to exclude certain areas.</param>
        /// <remarks>No bounds checking is done on the position</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCell(int2 position, float enterCost, int flags = 0)
        {
            positionToCell[position] = new EnterCostAndFlags(enterCost, flags);
        }

        /// <summary>
        /// Allocates an array containing all of the cells that are set on this grid.
        /// </summary>
        public NativeArray<HexGridCell> GetSetCells(Allocator allocator)
        {
            var kv = positionToCell.GetKeyValueArrays(Allocator.Temp);
            
            var cells = new NativeArray<HexGridCell>(kv.Length, allocator, NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < kv.Length; i++)
            {
                var value = kv.Values[i];
                cells[i] = new HexGridCell(kv.Keys[i], value.cost, value.flags);
            }

            return cells;
        }
        
        [ExcludeFromDocs]
        public void Dispose() => positionToCell.Dispose();
        
        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps) => positionToCell.Dispose(inputDeps);

        /// <summary>
        /// Enumerates all of the cells in the grid, including unset ones. This can be used for constructing ALT heuristics.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Struct enumerator that enumerates all cells of a bounded grid. This includes open cells that are not set.
        /// This can be used for constructing ALT heuristics.
        /// </summary>
        public struct Enumerator : IEnumerator<HexGridCell>
        {
            private int2 position;
            [ReadOnly] private HexGrid grid;

            public Enumerator(HexGrid grid)
            {
                this.grid = grid;
                this.position = new int2(grid.min.x - 1, grid.min.y);
            }
            
            public bool MoveNext()
            {
                position.x++;
                if (position.x > grid.max.x)
                {
                    position.x = grid.min.x;
                    position.y++;
                }

                return all(position <= grid.max);
            }

            public void Reset()
            {
                this.position = new int2(grid.min.x - 1, grid.min.y);
            }

            public HexGridCell Current => grid.GetCell(position);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}