using System;
using Unity.Collections;
using Unity.Mathematics;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Intermediate struct used by the <see cref="SSFA"/> algorithm. It keeps track of the original 3D sides of the portals and the 2D projected ones.
    /// This information can then be used to reconstruct the intersection points of the straightened path in 3D.
    /// </summary>
    /// <remarks>This method is not confined to word exclusively with the NavMesh.
    /// If your segments implement <see cref="IUnrolledNavMeshGraphPortal"/> you can unroll them into a plane and use it with
    /// <see cref="SSFA.GetSteerTargetPosition{T}"/> or <see cref="SSFA.AppendCornersUnrolled{T}"/></remarks>
    public struct UnrolledNavMeshGraphPortal : IUnrolledNavMeshGraphPortal
    {
        /// <summary>
        /// Left side of the portal in the XZ plane
        /// </summary>
        public float2 Left2D => TransformPoint(Left3D);
        
        /// <summary>
        /// Right side of the portal in the XZ plane
        /// </summary>
        public float2 Right2D => TransformPoint(Right3D);
        
        /// <summary>
        /// Left side of the portal
        /// </summary>
        public float3 Left3D { get; set; }
        
        /// <summary>
        /// Right side of the portal
        /// </summary>
        public float3 Right3D { get; set; }

        /// <summary>
        /// Original normal of the triangle
        /// </summary>
        public float3 Normal { get; set; }
        
        /// <summary>
        /// Origin of rotation
        /// </summary>
        public float3 Origin3D { get; set; }
        
        /// <summary>
        /// Flattened origin of rotation
        /// </summary>
        public float2 Origin2D { get; set; }

        /// <summary>
        /// The rotation that was used while unrolling this portal
        /// </summary>
        public quaternion Rotation { get; set; }

       
        /// <summary>
        /// Transforms a point in 3D space to a point in this unrolled segment
        /// </summary>
        public float2 TransformPoint(float3 point)
        {
            float3 offA = point - Origin3D;
            return math.mul(Rotation, offA).xz + Origin2D;
        }

        
        /// <summary>
        /// Unrolls a path of portals into the XZ plane, allowing the SSFA algorithm to work on curved paths.
        /// </summary>
        /// <param name="path">The path to process.</param>
        /// <param name="destSegments">Output array of the portals. Needs to be of the same length as the path.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <remarks>This method is not confined to word exclusively with the NavMesh.
        /// If your segments implement <see cref="IUnrolledNavMeshGraphPortal"/> you can unroll them into a plane and use it with
        /// <see cref="SSFA.GetSteerTargetPosition{T}"/> or <see cref="SSFA.AppendCornersUnrolled{T}"/></remarks>
        public static void Unroll<T>(NativeSlice<T> path, NativeSlice<UnrolledNavMeshGraphPortal> destSegments)
            where T : unmanaged, IUnrolledNavMeshGraphPortal
        {
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (path.Length != destSegments.Length)
                throw new ArgumentOutOfRangeException(nameof(destSegments), "Should be the same length as the path");
            if (path.Length == 0)
                return;
            #endif
            
            float3 prevA = path[0].Left3D;
            float3 prevB = path[0].Left3D;
            float3 prevNewA = float3.zero; //path[0].Left3D;
            float3 prevNewB = float3.zero; //path[0].Left3D;
            float3 prevNormal = new float3(0, 1, 0);
            quaternion prevRot = quaternion.identity;

            for (int i = 0; i < destSegments.Length; i++)
            {
                var edge = path[i];
                
                float3 edgeNormal = edge.Normal;
                
                // quick check to see if the normal is valid. if the normal is zero, this will give NaN's that propagate and
                // this can be caused by degenerate triangles in the path. If we encounter one, just assume the same orientation.
                if (math.lengthsq(edgeNormal) < .99f)
                    edgeNormal = prevNormal;
           
                float3 offA = edge.Left3D - prevA;
                float3 offB = edge.Right3D - prevB;

                var fromToRot = FromToRotation(edgeNormal, prevNormal);
              
                quaternion rot = math.mul(prevRot, fromToRot);
                float3 newA = math.mul(rot, offA) + prevNewA;
                float3 newB = math.mul(rot, offB) + prevNewB;

                destSegments[i] = new UnrolledNavMeshGraphPortal()
                {
                    Left3D = edge.Left3D,
                    Right3D = edge.Right3D,
                    Normal = edge.Normal, // this is also optional, but used to make the linerenderer in the example stick out from the surface a bit
                    Origin3D = prevA,
                    Origin2D = prevNewA.xz,
                    Rotation = rot
                };

                prevNewA = newA;
                prevNewB = newB;
                prevA = edge.Left3D;
                prevB = edge.Left3D;
                prevNormal = edgeNormal;
                prevRot = rot;
            }
        }
        
        /**
         * Unity's FromToRotation didn't work precise enough. If the two normals are almost the same, often times a far too large
         * rotation was returned, accumulating the error over a longer strip of triangles.
         * This implementation works precise enough for the use case of unrolling and is taken from:
         * https://stackoverflow.com/questions/1171849/finding-quaternion-representing-the-rotation-from-one-vector-to-another
         */
        private static quaternion FromToRotation(float3 u, float3 v)
        {
            float k_cos_theta = math.dot(u, v);
            float k = math.sqrt(math.lengthsq(u) * math.lengthsq(v));
            
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            // check to prevent NaN's.
            if (k == 0)
                throw new Exception("Invalid input normals");
            #endif

            //if (k_cos_theta / k == -1)
            if (k_cos_theta / k < -.99999f)
            {
                // 180 degree rotation around any orthogonal vector
                var val = math.normalize(Orthogonal(u));
                return new quaternion(val.x, val.y, val.z, 0);
            }

            var val2 = math.cross(u, v);
            float w = k_cos_theta + k;
            return math.normalize(new quaternion(val2.x, val2.y, val2.z, w));
        }
        
        private static float3 Orthogonal(float3 v)
        {
            v = math.abs(v);
            float3 other = v.x < v.y ? (v.x < v.z ? new float3(1,0,0) : new float3(0,0,1)) : (v.y < v.z ? new float3(0, 1, 0) : new float3(0, 0, 1));
            return math.cross(v, other);
        }
    }
}