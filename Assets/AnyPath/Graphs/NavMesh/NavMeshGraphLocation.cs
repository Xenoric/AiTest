using System;
using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// A location on the NavMeshGraph represents a triangle and a position within that triangle.
    /// Furthermore, when part of a path, the Left and Right properties point to the shared edge with the next triangle in the path.
    /// </summary>
    public struct NavMeshGraphLocation : IEquatable<NavMeshGraphLocation>, IUnrolledNavMeshGraphPortal, INodeFlags
    {
        /// <summary>
        /// Normal of the triangle for this location
        /// </summary>
        public float3 Normal { get; set; }

        /// <summary>
        /// Points to the left side of the edge connecting this triangle to the next triangle in the path.
        /// For start and ending positions, this value is equal to <see cref="Right3D"/>, to allow for navigating to an exact position within
        /// the triangle.
        /// </summary>
        public float3 Left3D { get; set; }

        /// <summary>
        /// Points to the left side of the edge connecting this triangle to the next triangle in the path.
        /// For start and ending positions, this value is equal to <see cref="Left3D"/>, to allow for navigating to an exact position within
        /// the triangle.
        /// </summary>
        public float3 Right3D { get; set; }

            
        /// <summary>
        /// This value is used internally by A* to provide better distance estimates between triangles. This is the closest point
        /// on the portal of this triangle inside a path to the previous position. This eliminates the problem where small triangles
        /// are adjecent to large ones and a weird detour gets taken in case we'd measure distances from the centers of the triangles.
        /// For locations used as start/goal, this position is also equal to Left and Right, but for locations inside a path, it is somewhere in between.
        /// </summary>
        public float3 ExitPosition { get; set; }

        /// <summary>
        /// Flags of this segment
        /// </summary>
        public int Flags { get; private set; }
            
        /// <summary>
        /// The index of the triangle for this location
        /// </summary>
        public int TriangleIndex { get; set; }
        
        /// <summary>
        /// For flat (XZ) worlds, this circumvents the need to unroll the path before the SSFA algorithm can run. Since we can use
        /// the XZ components directly.
        /// </summary>
        float2 IUnrolledNavMeshGraphPortal.Right2D => Right3D.xz;
        
        /// <summary>
        /// For flat (XZ) worlds, this circumvents the need to unroll the path before the SSFA algorithm can run. Since we can use
        /// the XZ components directly.
        /// </summary>
        float2 IUnrolledNavMeshGraphPortal.Left2D => Left3D.xz;

        [ExcludeFromDocs]
        public NavMeshGraphLocation(int triangleIndex, float3 left, float3 right, float3 normal, float3 exitPosition, int flags)
        {
            this.TriangleIndex = triangleIndex;
            //this.Triangle = triangle;
            this.Normal = normal;
            this.ExitPosition = exitPosition;
            this.Flags = flags;
            this.Left3D = left;
            this.Right3D = right;
        }
        
        /// <summary>
        /// Used by the <see cref="SSFA"/> for the start and goal segments. This returns a copy of this segment, only
        /// with the triangle's A (Left) and and B (Right) set exactly to the target position.
        /// </summary>
        /// <returns>A copy of this segment that has both Left and Right set to target Position.
        /// Note that this alters the triangle's A + B causing it to be a degenerate triangle. As such, the normal of the triangle can't be determined</returns>
        public NavMeshGraphLocation AsStartOrGoal()
        {
            // maybe triangle's AB + C should be target position idk
            var edge = new NavMeshGraphLocation(this.TriangleIndex, Left3D, Right3D, Normal, ExitPosition, Flags);
            edge.Left3D = ExitPosition;
            edge.Right3D = ExitPosition;
            return edge;
        }

        /// <summary>
        /// Shrinks the portal, where zero is no shrink and one is a total shrink towards the center.
        /// </summary>
        /// <remarks>Does not modify the location in place, returns a shrinked copy</remarks>
        public NavMeshGraphLocation Shrink(float ratio)
        {
            var edge = new NavMeshGraphLocation(this.TriangleIndex, Left3D, Right3D, Normal, ExitPosition, Flags);
            var center = .5f * (edge.Left3D + edge.Right3D);
            edge.Left3D = math.lerp(edge.Left3D, center, ratio);
            edge.Right3D = math.lerp(edge.Right3D, center, ratio);
            return edge;
        }

        public bool Equals(NavMeshGraphLocation other) => other.TriangleIndex == this.TriangleIndex;
        
        public override int GetHashCode() => TriangleIndex.GetHashCode();

        public override string ToString()
        {
            return
                $"Left3D: {Left3D} Right3D: {Right3D} Normal {Normal} ExitPosition: {ExitPosition} Flags: {Flags} TriangleIndex: {TriangleIndex}";
        }
    }
}