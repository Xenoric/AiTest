using AnyPath.Native;
using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Graphs.VoxelGrid
{
    /// <summary>
    /// Uses the <see cref="VoxelGridCell.Flags"/> field to determine if movement between to adjecent cells is valid.
    /// This can be used for simulating gravity for instance.
    /// For example by setting the <see cref="VoxelGrid.DefaultFlags"/> to <see cref="VoxelGridDirectionFlags.Down"/>,
    /// all open cells only support falling down as movement. You could then make "ground" cells that support all directions,
    /// forcing your paths to stick to the ground.
    /// </summary>
    public struct VoxelGridDirectionMod : IEdgeMod<VoxelGridCell>
    {
        [ExcludeFromDocs]
        public bool ModifyCost(in VoxelGridCell from, in VoxelGridCell to, ref float cost)
        {
            int supportedDirections = from.Flags;
            int direction = (int)VoxelGridCell.GetDirection(from, to);
            
            // Return true if the cell's flags contain a direction we support.
            return (direction & supportedDirections) != 0;
            
            /* Alternatively we could increase the cost if it's a direction we don't support:
            
            if ((direction & supportedDirections) == 0)
                cost += 1;

            return true;
            
            */
        }

        [ExcludeFromDocs]
        public void ModifyEdgeBuffer(in VoxelGridCell from, ref NativeList<Edge<VoxelGridCell>> edgeBuffer) { }
    }
}