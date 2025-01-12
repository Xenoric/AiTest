using System;
using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;

namespace AnyPath.Graphs.SquareGrid
{
    public struct SquareGridCell : IEquatable<SquareGridCell>, INodeFlags
    {
        /// <summary>
        /// Flags for this location. Can be used in conjunction with <see cref="FlagBitmask{TNode}"/>.
        /// </summary>
        public readonly int Flags { get; }
        
        /// <summary>
        /// Coordinate of this cell
        /// </summary>
        public readonly int2 Position { get; }
        
        /// <summary>
        /// The cost associated with entering this cell. Note that this is additional to the distance and should not be a negative value.
        /// </summary>
        public readonly float EnterCost { get; }

        /// <summary>
        /// Returns wether this cell is "walkable", that is, the EnterCost is not infinity.
        /// </summary>
        public bool IsOpen => math.isfinite(EnterCost);

        /// <summary>
        /// Utility to convert this cell into a Vector3Int for usage with a Unity Tilemap
        /// </summary>
        /// <param name="z">Optional Z value for the Vector3Int</param>
        /// <returns>The position of this cell as a Vector3Int</returns>
        public Vector3Int ToVector3Int(int z = 0) => new Vector3Int(Position.x, Position.y, z);
        
        /// <summary>
        /// Creates a square grid cell
        /// </summary>
        /// <param name="position">Position of this cell. If you're using this as a query start, only the position is sufficient.</param>
        /// <param name="enterCost">Optional extra cost for entering this cell</param>
        /// <param name="flags">Optional flags for this cell</param>
        public SquareGridCell(int2 position, float enterCost = 0, int flags = 0)
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
        public SquareGridCell(Vector3Int position, float enterCost = 0, int flags = 0) : this(new int2(position.x, position.y), enterCost, flags)
        {
        }

        /// <summary>
        /// Implicitly convert this location to an int2 value.
        /// </summary>
        public static implicit operator int2(SquareGridCell cell) => cell.Position;

        /// <summary>
        /// Implicitly convert this location to an Vector2Int value.
        /// </summary>
        public static implicit operator Vector2Int(SquareGridCell cell) => new Vector2Int(cell.Position.x, cell.Position.y);
        
        [ExcludeFromDocs]
        public bool Equals(SquareGridCell other) => math.all(Position == other.Position);

        [ExcludeFromDocs]
        public override int GetHashCode() => Position.GetHashCode();
    }
}