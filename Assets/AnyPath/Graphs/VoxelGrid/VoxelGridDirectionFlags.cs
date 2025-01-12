using System;

namespace AnyPath.Graphs.VoxelGrid
{
    /// <summary>
    /// Direction flags for usage with <see cref="VoxelGridDirectionMod"/>.
    /// Note that these can be combined. For instance, if you move diagonally forward + right,
    /// then both forward and the right flag will be set.
    /// </summary>
    [Flags]
    public enum VoxelGridDirectionFlags
    {
        None = 0,
        
        Up = 1 << 0,
        Down = 1 << 1,
        Forward = 1 << 2,
        Right = 1 << 3,
        Back = 1 << 4,
        Left = 1 << 5,
        
        All = -1
    }
}