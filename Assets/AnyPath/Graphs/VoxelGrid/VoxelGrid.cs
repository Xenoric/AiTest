using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.VoxelGrid
{
    /// <summary>
    /// A 3D voxel grid that's easily extendable with out of the box support for various movement types.
    /// </summary>
    public struct VoxelGrid : IGraph<VoxelGridCell>
    {
        /// <summary>
        /// A direction + movement cost. Used when constructing the grid to indicate what kind of movement is supported.
        /// </summary>
        public readonly struct DirCost
        {
            public const float SQRT2 = 1.41421356237f;
            public const float SQRT3 = 1.73205080757f;
            public readonly int3 direction;
            public readonly float cost;

            public DirCost(int x, int y, int z)
            {
                this.direction = new int3(x,y,z);
                this.cost = 1;
            }
            
            public DirCost(int x, int y, int z, float cost)
            {
                this.direction = new int3(x, y, z);
                this.cost = cost;
            }
        }
        /// <summary>
        /// Default straight 6 directional movement.
        /// </summary>
        public static readonly DirCost[] Foward_Right_Back_Left_Up_Down =
        {
            new DirCost(0, -1, 0), 
            new DirCost(0, 0, 1), new DirCost(1, 0, 0), new DirCost(0, 0, -1), new DirCost(-1, 0, 0),
            new DirCost(0, 1, 0)
        };
        
        /// <summary>
        /// Five straight directions: Forward, Right, Back, Left and Down
        /// Only allows upwards travel diagonally. Like walking "stairs".
        /// </summary>
        public static readonly DirCost[] Foward_Right_Back_Left_Down_StairsUp =
        {
            new DirCost(0, -1, 0),
            new DirCost(0, 0, 1), new DirCost(1, 0, 0), new DirCost(0, 0, -1), new DirCost(-1, 0, 0),
            new DirCost(0, 1, 1, DirCost.SQRT2), new DirCost(1, 1, 0, DirCost.SQRT2), new DirCost(0, 1, -1, DirCost.SQRT2), new DirCost(-1, 1, 0, DirCost.SQRT2),
        };
        
        /// <summary>
        /// Five straight directions: Forward, Right, Back, Left and Down
        /// Supports diagonal movement up and down.
        /// </summary>
        public static readonly DirCost[] Foward_Right_Back_Left_Down_StairsUp_StairsDown =
        {
            new DirCost(0, -1, 0),
            new DirCost(0, 0, 1), new DirCost(1, 0, 0), new DirCost(0, 0, -1), new DirCost(-1, 0, 0),
            
            // Stairs down
            new DirCost(0, -1, 1, DirCost.SQRT2), new DirCost(1, -1, 0, DirCost.SQRT2), new DirCost(0, -1, -1, DirCost.SQRT2), new DirCost(-1, -1, 0, DirCost.SQRT2),
            
            // Stairs up
            new DirCost(0, 1, 1, DirCost.SQRT2), new DirCost(1, 1, 0, DirCost.SQRT2), new DirCost(0, 1, -1, DirCost.SQRT2), new DirCost(-1, 1, 0, DirCost.SQRT2),
        };
        
        /// <summary>
        /// Allowed to move diagonally only when not going up or down.
        /// Up and down movement is only supported straight.
        /// </summary>
        public static readonly DirCost[] Eight_Up_Down =
        {
            new DirCost(0, -1, 0),
            new DirCost(0, 0, 1), new DirCost(1, 0, 1, DirCost.SQRT2), new DirCost(1, 0, 0), new DirCost(1, 0, -1, DirCost.SQRT2), 
            new DirCost(0,0,-1), new DirCost(-1,0,-1, DirCost.SQRT2), new DirCost(-1,0,0), new DirCost(-1, 0, 1, DirCost.SQRT2),
            new DirCost(0, 1, 0)
        };
        
        /// <summary>
        /// All 26 directions are supported.
        /// </summary>
        public static readonly DirCost[] All26 =
        {
            new DirCost(0, -1, 0),
            
            // Low plane
            new DirCost(0, -1, 1, DirCost.SQRT2), new DirCost(1, -1, 1, DirCost.SQRT3), new DirCost(1, -1, 0, DirCost.SQRT2), new DirCost(1, -1, -1, DirCost.SQRT3), 
            new DirCost(0, -1, -1, DirCost.SQRT2), new DirCost(-1, -1,-1, DirCost.SQRT3), new DirCost(-1, -1, 0, DirCost.SQRT2), new DirCost(-1, -1, 1, DirCost.SQRT3),
            
            // Leveled
            new DirCost(0, 0, 1), new DirCost(1, 0, 1, DirCost.SQRT2), new DirCost(1, 0, 0), new DirCost(1, 0, -1, DirCost.SQRT2), 
            new DirCost(0, 0,-1), new DirCost(-1,0,-1, DirCost.SQRT2), new DirCost(-1,0,0), new DirCost(-1, 0, 1, DirCost.SQRT2),
            
            // Up plane
            new DirCost(0, 1, 1, DirCost.SQRT2), new DirCost(1, 1, 1, DirCost.SQRT3), new DirCost(1, 1, 0, DirCost.SQRT2), new DirCost(1, 1, -1, DirCost.SQRT3), 
            new DirCost(0, 1,-1, DirCost.SQRT2), new DirCost(-1, 1,-1, DirCost.SQRT3), new DirCost(-1, 1,0, DirCost.SQRT2), new DirCost(-1, 1, 1, DirCost.SQRT3),
            
            new DirCost(0, 1, 0)
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
        /// The boundary -min- position
        /// </summary>
        public readonly int3 min; // must be readonly for Enumerator

        /// <summary>
        /// The boundary -max- position
        /// </summary>
        public readonly int3 max;

    
        private readonly EnterCostAndFlags defaultEnterCostAndFlags;


        /// <summary>
        /// <para>
        /// What (enter) cost to use for cells that have not been explicity set. When this is zero, this means
        /// that all open cells are navigatable by default.
        /// </para>
        /// <para>
        /// One use case here is to make this float.PositiveInfinity. Then by default,
        /// no cells are navigatable. Only cells that are explicity set (for example, with a cost of zero)
        /// would be navigatable. In essence creating a "cave".
        /// </para>
        /// </summary>
        public float DefaultCost => defaultEnterCostAndFlags.cost;

        /// <summary>
        /// Flags to use for a cells that have not been explicity set.
        /// </summary>
        public int DefaultFlags => defaultEnterCostAndFlags.flags;
        
        
        private NativeHashMap<int3, EnterCostAndFlags> map;
        private NativeArray<DirCost> directions;
        
        /// <summary>
        /// Constructs a new grid that supports straight 6 directional movement.
        /// </summary>
        /// <param name="min">Boundary min</param>
        /// <param name="max">Boundary max</param>
        /// <param name="capacity">Initial capacity of the internal hashmap. If you know how many cells you are going to add beforehand,
        /// set it to this value for faster creation.</param>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="defaultCost">The default cost to use for cells that are not set with any value</param>
        /// <param name="defaultFlags">The default flags to use for cells that are not set with any value</param>
        public VoxelGrid(
            int3 min, int3 max,
            Allocator allocator,
            float defaultCost = 0, 
            int defaultFlags = 0, 
            int capacity = 0) : this(min, max, Foward_Right_Back_Left_Up_Down, allocator, defaultCost, defaultFlags, capacity)
        {
        }

        /// <summary>
        /// Constructs a new grid.
        /// </summary>
        /// <param name="min">Boundary min</param>
        /// <param name="max">Boundary max</param>
        /// <param name="capacity">Initial capacity of the internal hashmap. If you know how many cells you are going to add beforehand,
        /// set it to this value for faster creation.</param>
        /// <param name="directionsAndCost">
        /// Supported movement directions. Use any of the following:
        /// <list type="bullet">
        /// <item><see cref="Foward_Right_Back_Left_Up_Down"/></item>
        /// <item><see cref="Foward_Right_Back_Left_Down_StairsUp"/></item>
        /// <item><see cref="Foward_Right_Back_Left_Down_StairsUp_StairsDown"/></item>
        /// <item><see cref="Eight_Up_Down"/></item>
        /// <item><see cref="All26"/></item>
        /// </list>
        /// </param>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="defaultCost">The default cost to use for cells that are not set with any value</param>
        /// <param name="defaultFlags">The default flags to use for cells that are not set with any value</param>
        public VoxelGrid(
            int3 min, int3 max, 
            DirCost[] directionsAndCost,
            Allocator allocator,
            float defaultCost = 0, 
            int defaultFlags = 0, 
            int capacity = 0)
        {
            this.map = new NativeHashMap<int3, EnterCostAndFlags>(capacity, allocator);
            this.min = min;
            this.max = max;
            this.defaultEnterCostAndFlags = new EnterCostAndFlags(defaultCost, defaultFlags);
            this.directions = new NativeArray<DirCost>(directionsAndCost, allocator);
        }
        
        /// <summary>
        /// Constructs a new grid. Can be used inside of jobs.
        /// </summary>
        /// <param name="min">Boundary min</param>
        /// <param name="max">Boundary max</param>
        /// <param name="capacity">Initial capacity of the internal hashmap. If you know how many cells you are going to add beforehand,
        /// set it to this value for faster creation.</param>
        /// <param name="directionsAndCost">Supported movement directions + associated costs. The supplied nativearray is copied.</param>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="defaultCost">The default cost to use for cells that are not set with any value</param>
        /// <param name="defaultFlags">The default flags to use for cells that are not set with any value</param>
        public VoxelGrid(
            int3 min, int3 max, 
            NativeArray<DirCost> directionsAndCost,
            Allocator allocator,
            float defaultCost,
            int defaultFlags,
            int capacity)
        {
            this.map = new NativeHashMap<int3, EnterCostAndFlags>(capacity, allocator);
            this.min = min;
            this.max = max;
            this.defaultEnterCostAndFlags = new EnterCostAndFlags(defaultCost, defaultFlags);
            this.directions = new NativeArray<DirCost>(directionsAndCost, allocator);
        }
       
        /// <summary>
        /// Allocates an array containing all of the cells that are explicity set on this grid.
        /// </summary>
        public NativeArray<VoxelGridCell> GetSetCells(Allocator allocator)
        {
            var kv = map.GetKeyValueArrays(Allocator.Temp);
            
            var cells = new NativeArray<VoxelGridCell>(kv.Length, allocator, NativeArrayOptions.UninitializedMemory);

            for (var i = 0; i < kv.Length; i++)
            {
                var value = kv.Values[i];
                cells[i] = new VoxelGridCell(kv.Keys[i], value.cost, value.flags);
            }

            return cells;
        }

        /// <summary>
        /// Collects all neighbouring cells from a given location
        /// </summary>
        public void Collect(VoxelGridCell node, ref NativeList<Edge<VoxelGridCell>> edgeBuffer)
        {
            for (int i = 0; i < directions.Length; i++)
            {
                var directionAndCost = directions[i];
                int3 nextPos = node + directionAndCost.direction;
                if (!InBounds(nextPos)) continue;

                if (!map.TryGetValue(nextPos, out var costAndFlags))
                    costAndFlags = defaultEnterCostAndFlags;
                
                if (isfinite(costAndFlags.cost))
                    edgeBuffer.Add(new Edge<VoxelGridCell>(
                        new VoxelGridCell(nextPos, costAndFlags.cost, costAndFlags.flags), 
                        directionAndCost.cost + costAndFlags.cost));
            }
        }
        
        /// <summary>
        /// Returns wether a certain position is within the bounds of the grid
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InBounds(int3 position) => all(position >= min) && all(position <= max);

        /// <summary>
        /// Returns the cell at a given position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]     
        public VoxelGridCell GetCell(int3 position)
        {
            return map.TryGetValue(position, out var costAndFlags)
                ? new VoxelGridCell(position, costAndFlags.cost, costAndFlags.flags)
                : new VoxelGridCell(position, defaultEnterCostAndFlags.cost, defaultEnterCostAndFlags.flags);
        }

        /// <summary>
        /// Returns the enter cost of a given position. Note that unset cells are considered open and have an entering cost of zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        public float GetCost(int3 position)
        {
            return map.TryGetValue(position, out var costAndFlags) ? 
                costAndFlags.cost : 
                defaultEnterCostAndFlags.cost;
        }

       
        /// <summary>
        /// Sets the cost for a cell.
        /// </summary>
        /// <param name="position">Position to set</param>
        /// <param name="enterCost">Additional cost for walking this cell. Use float.PositiveInfinity to make this cell unwalkable</param>
        /// <param name="flags">Flags for this cell, this can be used in conjunction with <see cref="FlagBitmask{TNode}"/> to exclude certain areas.</param>
        /// <remarks>No bounds checking is done on the position</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetCell(int3 position, float enterCost, int flags = 0)
        {
            map[position] = new EnterCostAndFlags(enterCost, flags);
        }

        /// <summary>
        /// Clear a cell at a given position.
        /// </summary>
        /// <remarks>The cell will be considered as unset again and the default cost and flags will be used for this position.</remarks>
        public void ClearCell(int3 position) => map.Remove(position);

        [ExcludeFromDocs] public void Dispose()
        {
            map.Dispose();
            directions.Dispose();
        }

        [ExcludeFromDocs] public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(map.Dispose(inputDeps), directions.Dispose(inputDeps));
        }

        [ExcludeFromDocs] public bool IsCreated => map.IsCreated;
        
        /// <summary>
        /// Enumerates all of the cells in the grid, including unset ones. This can be used for constructing ALT heuristics.
        /// </summary>
        /// <remarks>Be cautious as a 3D grid contains a lot of positions. I don't advise to use ALT heuristics on large 3D grids.</remarks>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Struct enumerator that enumerates all cells of a bounded grid. This includes open cells that are not set.
        /// This can be used for constructing ALT heuristics.
        /// </summary>
        /// <remarks>A lot of cells will be returned on a large space.</remarks>
        public struct Enumerator : IEnumerator<VoxelGridCell>
        {
            private int3 position;
            [ReadOnly] private VoxelGrid grid;

            public Enumerator(VoxelGrid grid)
            {
                this.grid = grid;
                this.position = new int3(grid.min.x - 1, grid.min.y, grid.min.z);
            }
            
            public bool MoveNext()
            {
                position.x++;
                if (position.x > grid.max.x)
                {
                    position.x = grid.min.x;
                    position.y++;
                    
                    if (position.y > grid.max.y)
                    {
                        position.y = grid.min.y;
                        position.z++;
                    }
                }
                
                return all(position <= grid.max);
            }

            public void Reset()
            {
                this.position = new int3(grid.min.x - 1, grid.min.y, grid.min.z);
            }

            public VoxelGridCell Current => grid.GetCell(position);

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
    }
}