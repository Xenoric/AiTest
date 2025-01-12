using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.SquareGrid
{
    /// <summary>
    /// A 2D-grid where each cell is defined as an int2
    /// Travelling one cell along the map has a cost of 1 + destination cell cost.
    /// Travelling diagonally has a cost of sqrt(2) + destination cell cost.
    /// Unset cells have a default cost of zero.
    /// To make a cell unwalkable, assign it a cost of infinity.
    /// This means that every location is walkable by default.
    /// </summary>
    public struct SquareGrid : IGraph<SquareGridCell>
    {
        /// <summary>
        /// Neighbour lookup table
        /// </summary>
        private static readonly int2[] directions =
        {
            // straight directions first
            new int2(0, 1), new int2(1, 0), new int2(0, -1), new int2(-1, 0), 
            new int2(1,1), new int2(1,-1), new int2(-1,-1), new int2(-1, 1) 
        };
        
        /// <summary>
        /// Movement cost lookup table
        /// </summary>
        private static readonly float[] movementCost =
        {
            // straight directions have a movement cost of one
            1,1,1,1,
            // diagonals sqrt(2)
            SQRT2, SQRT2, SQRT2, SQRT2
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
        /// Type of grid, 4 our 8 neighbours
        /// </summary>
        public readonly SquareGridType neighbourMode;

        /// <summary>
        /// The boundary -min- position
        /// </summary>
        public readonly int2 min; // must be readonly for Enumerator

        /// <summary>
        /// The boundary -max- position
        /// </summary>
        public readonly int2 max;

        
        private NativeHashMap<int2, EnterCostAndFlags> map;

        /// <summary>
        /// Constructs a new grid
        /// </summary>
        /// <param name="min">Boundary min</param>
        /// <param name="max">Boundary max</param>
        /// <param name="neighbourMode">Should cells have four or eight neighbours?</param>
        /// <param name="cells">Array containing the cells to set initially</param>
        /// <param name="allocator">Allocator to use</param>
        public SquareGrid(int2 min, int2 max, SquareGridType neighbourMode, IReadOnlyList<SquareGridCell> cells, Allocator allocator) : 
            this(min, max, neighbourMode, cells.Count, allocator)
        {
            foreach (var cell in cells)
                SetCell(cell.Position, cell.EnterCost, cell.Flags);
        }

        /// <summary>
        /// Constructs a new grid
        /// </summary>
        /// <param name="min">Boundary min</param>
        /// <param name="max">Boundary max</param>
        /// <param name="neighbourMode">Should cells have four or eight neighbours?</param>
        /// <param name="capacity">Initial capacity of the internal hashmap</param>
        /// <param name="allocator">Allocator to use</param>
        public SquareGrid(int2 min, int2 max, SquareGridType neighbourMode, int capacity, Allocator allocator)
        {
            this.map = new NativeHashMap<int2, EnterCostAndFlags>(capacity, allocator);
            this.min = min;
            this.max = max;
            this.neighbourMode = neighbourMode;
            
        }
        
       
        /// <summary>
        /// Allocates an array containing all of the cells that are set on this grid.
        /// </summary>
        public NativeArray<SquareGridCell> GetSetCells(Allocator allocator)
        {
            var kv = map.GetKeyValueArrays(Allocator.Temp);
            
            var cells = new NativeArray<SquareGridCell>(kv.Length, allocator, NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < kv.Length; i++)
            {
                var value = kv.Values[i];
                cells[i] = new SquareGridCell(kv.Keys[i], value.cost, value.flags);
            }

            return cells;
        }
        
        


        /// <summary>
        /// Collects all neighbouring cells from a given location
        /// </summary>
        /// <param name="node">The location to find the neighbours for</param>
        public void Collect(SquareGridCell node, ref NativeList<Edge<SquareGridCell>> edgeBuffer)
        {
            for (int i = 0; i < (int)neighbourMode; i++)
            {
                int2 nextPos = node + directions[i];
                if (!InBounds(nextPos)) continue;
                map.TryGetValue(nextPos, out var costAndFlags); // return default cell if not in map
                if (isfinite(costAndFlags.cost))
                    edgeBuffer.Add(new Edge<SquareGridCell>(new SquareGridCell(nextPos, costAndFlags.cost, costAndFlags.flags), movementCost[i] + costAndFlags.cost));
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
        public SquareGridCell GetCell(int2 position)
        {
            map.TryGetValue(position, out var costAndFlags);
            return new SquareGridCell(position, costAndFlags.cost, costAndFlags.flags);
        }

        /// <summary>
        /// Returns the enter cost of a given position. Note that unset cells are considered open and have an entering cost of zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        public float GetCost(int2 position)
        {
            map.TryGetValue(position, out var costAndFlags);
            return costAndFlags.cost;
        }

        /// <summary>
        /// Returns wether a cell at a position is open/walkable.
        /// </summary>
        public bool IsOpen(int2 position)
        {
            if (!InBounds(position))
                return false;

            return !map.TryGetValue(position, out var costAndFlags) ||
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
            map[position] = new EnterCostAndFlags(enterCost, flags);
        }

        [ExcludeFromDocs] public void Dispose()
        {
            map.Dispose();
        }

        [ExcludeFromDocs] public JobHandle Dispose(JobHandle inputDeps)
        {
            return map.Dispose(inputDeps);
        }

        [ExcludeFromDocs] public bool IsCreated => map.IsCreated;
        
        /// <summary>
        /// Enumerates all of the cells in the grid, including unset ones. This can be used for constructing ALT heuristics.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Struct enumerator that enumerates all cells of a bounded grid. This includes open cells that are not set.
        /// This can be used for constructing ALT heuristics.
        /// </summary>
        public struct Enumerator : IEnumerator<SquareGridCell>
        {
            private int2 position;
            [ReadOnly] private SquareGrid grid;

            public Enumerator(SquareGrid grid)
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

            public SquareGridCell Current => grid.GetCell(position);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}