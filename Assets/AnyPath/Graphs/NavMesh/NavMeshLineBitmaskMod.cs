using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Combines <see cref="NavMeshLineMod"/> and <see cref="FlagBitmask{TNode}"/>
    /// into one modifier. Locations that don't pass the bitmask will not be considered walkable.
    /// </summary>
    public struct NavMeshLineBitmaskMod : IEdgeMod<NavMeshGraphLocation>
    {
        public NavMeshLineMod lineMod;
        public FlagBitmask<NavMeshGraphLocation> flagBitmask;
        
        [ExcludeFromDocs]
        public NavMeshLineBitmaskMod(NavMeshGraphLocation start, NavMeshGraphLocation end, int bitmask)
        {
            lineMod = new NavMeshLineMod(start, end);
            flagBitmask = new FlagBitmask<NavMeshGraphLocation>(bitmask);
        }
        
        [ExcludeFromDocs]
        public NavMeshLineBitmaskMod(float3 a, float3 b, int bitmask)
        {
            lineMod = new NavMeshLineMod(a, b);
            flagBitmask = new FlagBitmask<NavMeshGraphLocation>(bitmask);
        }
        
        public bool ModifyCost(in NavMeshGraphLocation from, in NavMeshGraphLocation to, ref float cost)
        {
            if (!flagBitmask.ModifyCost(from, to, ref cost))
                return false;

            return lineMod.ModifyCost(from, to, ref cost);
        }

        public void ModifyEdgeBuffer(in NavMeshGraphLocation from, ref NativeList<Edge<NavMeshGraphLocation>> edgeBuffer) { }
    }
}