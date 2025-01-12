using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Utility to weld close vertices in a mesh together.
    /// </summary>
    public class NavMeshWelder
    {
        /// <summary>
        /// Finds vertices that are close together in a mesh and welds them together.
        /// Depending on your mesh this may or may not be neccessary for correct pathfinding, as neighbouring triangles need to share
        /// the same vertices.
        /// </summary>
        /// <param name="inVertices">Original vertices of the mesh</param>
        /// <param name="inOutIndices">The triangle indices of the mesh. This array is modified in place and will contain the new indices afterwards.</param>
        /// <param name="outVertices">The new unique vertices of the welded mesh, use in conjunction with inOutIndices. This list is not cleared beforehand</param>
        /// <param name="dependsOn">Optional job dependency for the scheduled job</param>
        /// <param name="weldThreshold">Distance below which two vertices will be welded together</param>
        /// <remarks>This method schedules a burst compiled job doing the work and can be run on another thread.</remarks>
        public static JobHandle ScheduleWeld(
            NativeArray<Vector3> inVertices, 
            NativeArray<int> inOutIndices, 
            NativeList<Vector3> outVertices, float weldThreshold = .001f, JobHandle dependsOn = default)
        {
            var job = new WeldJob()
            {
                weldThreshold = weldThreshold,
                inVertices = inVertices,
                inOutIndices = inOutIndices,
                outVertices = outVertices,
            };

            return job.Schedule(dependsOn);
        }
        
        /// <summary>
        /// Finds vertices that are close together in a mesh and welds them together.
        /// Depending on your mesh this may or may not be neccessary for correct pathfinding, as neighbouring triangles need to share
        /// the same vertices.
        /// </summary>
        /// <param name="inOutVertices">The vertices to weld together. This list contains the modified vertices afterwards.</param>
        /// <param name="inOutIndices">The triangle indices of the mesh. This array is modified in place and will contain the new indices afterwards.</param>
        /// <param name="dependsOn">Optional job dependency for the scheduled job</param>
        /// <param name="weldThreshold">Distance below which two vertices will be welded together</param>
        /// <remarks>This method schedules a burst compiled job doing the work and can be run on another thread.</remarks>
        public static JobHandle ScheduleWeld(
            NativeList<Vector3> inOutVertices, 
            NativeArray<int> inOutIndices, float weldThreshold = .001f, JobHandle dependsOn = default)
        {
            var job = new WeldJobInPlace()
            {
                weldThreshold = weldThreshold,
                inOutVertices = inOutVertices,
                inOutIndices = inOutIndices,
            };

            return job.Schedule(dependsOn);
        }

        /// <summary>
        /// Finds vertices that are close together in a mesh and welds them together.
        /// Depending on your mesh this may or may not be neccessary for correct pathfinding, as neighbouring triangles need to share
        /// the same vertices.
        /// </summary>
        /// <param name="inVertices">Original vertices of the mesh</param>
        /// <param name="inOutIndices">The triangle indices of the mesh. This array is modified in place and will contain the new indices afterwards</param>
        /// <param name="outVertices">The new unique vertices of the welded mesh, this list should be cleared beforehand</param>
        /// <param name="buckets">A temporary container used by the algorithm, this list should be cleared beforehand</param>
        /// <param name="shiftedIndices">A temporary container used by the algorithm</param>
        /// <param name="weldThreshold">Distance below which two vertices will be welded together</param>
        /// <remarks>
        /// <para>This method can be used inside a burst compiled job</para>
        /// </remarks>
        public static void Weld( 
            NativeArray<Vector3> inVertices, 
            NativeArray<int> inOutIndices, 
            NativeList<Vector3> outVertices, 
            NativeHashMap<int3, int> buckets,
            NativeHashMap<int, int> shiftedIndices, float weldThreshold = .001f)
        {
            // assumes buckets are cleared
            // no need to clear shiftedIndices, as we're certain to overwrite all values we use

            weldThreshold = 1 / weldThreshold;
            
            for (int i = 0; i < inVertices.Length; i++)
            {
                float3 vert = inVertices[i];
                int3 bucketPos = (int3)math.round(vert * weldThreshold);

                if (buckets.TryGetValue(bucketPos, out int shiftedIndex))
                {
                    shiftedIndices[i] = shiftedIndex;
                }
                else
                {
                    shiftedIndex = outVertices.Length;
                    buckets.Add(bucketPos, shiftedIndex);
                    shiftedIndices[i] = shiftedIndex;
                    outVertices.Add(vert);
                }
            }

            for (int i = 0; i < inOutIndices.Length; i++)
                inOutIndices[i] = shiftedIndices[inOutIndices[i]];
        }

        /// <summary>
        /// Finds vertices that are close together in a mesh and welds them together.
        /// Depending on your mesh this may or may not be neccessary for correct pathfinding, as neighbouring triangles need to share
        /// the same vertices.
        /// </summary>
        /// <param name="inOutVertices">The vertices to weld together. This list contains the modified vertices afterwards.</param>
        /// <param name="inOutIndices">The triangle indices to weld together. This list contains the modified triangles afterwards.</param>
        /// <param name="weldThreshold">Distance below which two vertices will be welded together</param>
        /// <remarks>For best performance, use the native overloads as they can utilize Unity's burst compiler for significant speed gains</remarks>
        public static void Weld(List<Vector3> inOutVertices, List<int> inOutIndices, float weldThreshold)
        {
            List<Vector3> outVertices = new List<Vector3>();
            Weld(inOutVertices, inOutIndices, outVertices, weldThreshold);
            inOutVertices.Clear();
            inOutVertices.AddRange(outVertices);
        }
        
        /// <summary>
        /// Finds vertices that are close together in a mesh and welds them together.
        /// Depending on your mesh this may or may not be neccessary for correct pathfinding, as neighbouring triangles need to share
        /// the same vertices.
        /// </summary>
        /// <param name="inOutVertices">The vertices to weld together. This list contains the modified vertices afterwards.</param>
        /// <param name="inOutIndices">The triangle indices to weld together. This list contains the modified triangles afterwards.</param>
        /// <param name="weldThreshold">Distance below which two vertices will be welded together</param>
        /// <param name="outVertices">List to output the new vertices too, this list should be manually cleared beforehand</param>
        /// <remarks>For best performance, use the native overloads as they can utilize Unity's burst compiler for significant speed gains</remarks>
        public static void Weld(List<Vector3> inOutVertices, List<int> inOutIndices, List<Vector3> outVertices, float weldThreshold)
        {
            Dictionary<Vector3Int, int> buckets = new Dictionary<Vector3Int, int>(inOutVertices.Count);
            Dictionary<int, int> shiftedIndices = new Dictionary<int, int>(inOutIndices.Count);
            weldThreshold = 1 / weldThreshold;

            for (int i = 0; i < inOutVertices.Count; ++i)
            {
                Vector3 vert = inOutVertices[i];
                var bucketPos = Vector3Int.RoundToInt(vert * weldThreshold);
                if (buckets.TryGetValue(bucketPos, out int shiftedIndex))
                {
                    shiftedIndices[i] = shiftedIndex;
                }
                else
                {
                    shiftedIndex = outVertices.Count;
                    buckets.Add(bucketPos, shiftedIndex);
                    shiftedIndices[i] = shiftedIndex;
                    outVertices.Add(vert);
                }
            }
            
            // Walk indices array and replace any repeated vertex indices with their corresponding unique one
            for (int i = 0; i < inOutIndices.Count; ++i)
            {
                var currentIndex = inOutIndices[i];
                inOutIndices[i] = shiftedIndices[currentIndex];
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct WeldJob : IJob
        {
            public float weldThreshold;
            [ReadOnly] public NativeArray<Vector3> inVertices;
            public NativeArray<int> inOutIndices;
            public NativeList<Vector3> outVertices;
          
            public void Execute()
            {
                var buckets = new NativeHashMap<int3, int>(inVertices.Length, Allocator.Temp);
                var shiftedIndices = new NativeHashMap<int, int>(inOutIndices.Length, Allocator.Temp);
                Weld(inVertices, inOutIndices, outVertices, buckets, shiftedIndices, weldThreshold);
            }
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private struct WeldJobInPlace : IJob
        {
            public float weldThreshold;
            public NativeList<Vector3> inOutVertices;
            public NativeArray<int> inOutIndices;

            public void Execute()
            {
                var buckets = new NativeHashMap<int3, int>(inOutVertices.Length, Allocator.Temp);
                var shiftedIndices = new NativeHashMap<int, int>(inOutIndices.Length, Allocator.Temp);
                var tempVertices = new NativeList<Vector3>(inOutVertices.Length, Allocator.Temp);
                
                Weld(inOutVertices.AsArray(), inOutIndices, tempVertices, buckets, shiftedIndices, weldThreshold);
                
                inOutVertices.Clear();
                inOutVertices.CopyFrom(tempVertices);
            }
        }
        
        /*
         * Kept for reference, this weld is the other way around, traverses the triangles instead of the vertices.
         * This saves us an additional hashmap to store the mapping from old indices to new indices, but testing showed
         * this was actually slower because meshes usually have more triangle indices than vertices
         */
        
        /*
        public static void Weld_Slower(float weldThreshold, 
            NativeArray<Vector3> inVertices, 
            NativeArray<int> inOutIndices, 
            NativeList<Vector3> outVertices, 
            NativeHashMap<int3, int> buckets)
        {
            buckets.Clear();
            weldThreshold = 1 / weldThreshold;
            for (int i = 0; i < inOutIndices.Length; i++)
            {
                float3 vert = inVertices[inOutIndices[i]];
                int3 bucketPos = (int3)math.round(vert * weldThreshold);

                if (buckets.TryGetValue(bucketPos, out int existingIndex))
                {
                    inOutIndices[i] = existingIndex;
                }
                else
                {
                    int newIndex = outVertices.Length;
                    buckets.Add(bucketPos, newIndex);
                    inOutIndices[i] = newIndex;
                    outVertices.Add(vert);
                }
            }
        }
        */
    }
}