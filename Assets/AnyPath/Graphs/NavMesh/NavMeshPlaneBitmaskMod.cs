using AnyPath.Native;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Internal;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Combines <see cref="NavMeshPlaneMod"/> and <see cref="FlagBitmask{TNode}"/>
    /// into one modifier. Locations that don't pass the bitmask will not be considered walkable.
    /// </summary>
    public struct NavMeshPlaneBitmaskMod : IEdgeMod<NavMeshGraphLocation>
    {
        public NavMeshPlaneMod planeMod;
        public FlagBitmask<NavMeshGraphLocation> flagBitmask;

        [ExcludeFromDocs]
        public NavMeshPlaneBitmaskMod(Plane plane, int bitmask)
        {
            planeMod = new NavMeshPlaneMod(plane);
            flagBitmask = new FlagBitmask<NavMeshGraphLocation>(bitmask);
        }
        
        public bool ModifyCost(in NavMeshGraphLocation from, in NavMeshGraphLocation to, ref float cost)
        {
            if (!flagBitmask.ModifyCost(from, to, ref cost))
                return false;

            return planeMod.ModifyCost(from, to, ref cost);
        }

        public void ModifyEdgeBuffer(in NavMeshGraphLocation from, ref NativeList<Edge<NavMeshGraphLocation>> edgeBuffer) { }
    }
}