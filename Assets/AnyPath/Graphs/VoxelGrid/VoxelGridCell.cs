using System;
using System.Runtime.CompilerServices;
using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;

namespace AnyPath.Graphs.VoxelGrid
{
    public struct VoxelGridCell : IEquatable<VoxelGridCell>, INodeFlags
    {
        /// <summary>
        /// Flags for this location. Can be used in conjunction with <see cref="FlagBitmask{TNode}"/>.
        /// </summary>
        public int Flags { get; set;  }
        
        /// <summary>
        /// Coordinate of this cell
        /// </summary>
        public readonly int3 Position { get; }
        
        /// <summary>
        /// The cost associated with entering this cell. Note that this is additional to the distance and should not be a negative value.
        /// </summary>
        public readonly float EnterCost { get; }

        /// <summary>
        /// Returns wether this cell is "walkable", that is, the EnterCost is not infinity.
        /// </summary>
        public bool IsOpen => math.isfinite(EnterCost);

        /// <summary>
        /// Creates a square grid cell
        /// </summary>
        /// <param name="position">Position of this cell. If you're using this as a query start, only the position is sufficient.</param>
        /// <param name="enterCost">Optional extra cost for entering this cell</param>
        /// <param name="flags">Optional flags for this cell</param>
        public VoxelGridCell(int3 position, float enterCost = 0, int flags = 0)
        {
            this.Position = position;
            this.EnterCost = enterCost;
            this.Flags = flags;
        }
        
        /// <summary>
        /// Creates a square grid cell
        /// </summary>
        /// <param name="position">Position of this cell, note that the Z value is not used. If you're using this as a query start, only the position is sufficient.</param>
        /// <param name="enterCost">Optional extra cost for entering this cell</param>
        /// <param name="flags">Optional flags for this cell</param>
        public VoxelGridCell(Vector3Int position, float enterCost = 0, int flags = 0) : this(new int3(position.x, position.y, position.z), enterCost, flags)
        {
        }

        /// <summary>
        /// Implicitly convert this location to an int2 value.
        /// </summary>
        public static implicit operator int3(VoxelGridCell cell) => cell.Position;

        /// <summary>
        /// Implicitly convert this location to an Vector2Int value.
        /// </summary>
        public static implicit operator Vector3Int(VoxelGridCell cell) => new Vector3Int(cell.Position.x, cell.Position.y, cell.Position.z);

        public Vector3 ToVector3() => new Vector3(Position.x, Position.y, Position.z);
        
        /// <summary>
        /// Returns the direction from one cell to another.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VoxelGridDirectionFlags GetDirection(in VoxelGridCell from, in VoxelGridCell to)
        {
            int3 delta = to.Position - from.Position;
            return
                (delta.x > 0 ? VoxelGridDirectionFlags.Right : delta.x < 0 ? VoxelGridDirectionFlags.Left : VoxelGridDirectionFlags.None) |
                (delta.y > 0 ? VoxelGridDirectionFlags.Up : delta.y < 0 ? VoxelGridDirectionFlags.Down : VoxelGridDirectionFlags.None) |
                (delta.z > 0 ? VoxelGridDirectionFlags.Forward : delta.z < 0 ? VoxelGridDirectionFlags.Back : VoxelGridDirectionFlags.None);
        }
        
        [ExcludeFromDocs]
        public bool Equals(VoxelGridCell other) => math.all(Position == other.Position);

        [ExcludeFromDocs]
        public override int GetHashCode() => Position.GetHashCode();
    }
}