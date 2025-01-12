using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// <para>
    /// Similar to <see cref="NavMeshLineMod"/>, but instead measures the distance to a plane.
    /// </para>
    /// <para>
    /// Using this modifier makes A* prefer paths that are close to a plane.
    /// </para>
    /// <para>
    /// A use case for this is to obtain more geodesic paths on a spherical navmesh.
    /// Or more straight looking paths on navmeshes that contain a lot of hills or more complicated 3D shapes.
    /// </para>
    /// </summary>
    public struct NavMeshPlaneMod : IEdgeMod<NavMeshGraphLocation>
    {
        private Plane plane;

        /// <summary>
        /// Construct the mod for usage with a sphere.
        /// </summary>
        /// <param name="start">Start location of your query. This should be on the sphere</param>
        /// <param name="end">Target location of your query</param>
        /// <param name="center">The center of the sphere</param>
        public NavMeshPlaneMod(NavMeshGraphLocation start, NavMeshGraphLocation end, float3 center)
        {
            this.plane = new Plane(start.ExitPosition, end.ExitPosition, center);
        }
        
        /// <summary>
        /// Construct the mod using a custom plane
        /// </summary>
        /// <param name="plane">The plane to measure the distance to</param>
        public NavMeshPlaneMod(Plane plane)
        {
            this.plane = plane;
        }
        
        public bool ModifyCost(in NavMeshGraphLocation from, in NavMeshGraphLocation to, ref float cost)
        {
            cost += math.abs(plane.GetDistanceToPoint(to.ExitPosition));
            return true;
        }

        public void ModifyEdgeBuffer(in NavMeshGraphLocation from, ref NativeList<Edge<NavMeshGraphLocation>> edgeBuffer) { }
    }
}