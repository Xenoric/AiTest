using AnyPath.Graphs.NavMesh;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AnyPath.Graphs.PlatformerGraph
{
    /// <summary>
    /// Utility to populate PlatformerGraph using Unity's Job system. This can be useful if you have frequent updates and want
    /// this process to be as fast as possible and/or on another thread.
    /// </summary>
    /// <remarks>Works in the same manner as the <see cref="NavMeshPopulator"/></remarks>
    public static class PlatformerGraphPopulator
    {
        [BurstCompile]
        struct PopulateJob : IJob
        {
            public NativeArray<float2> vertices;
            public NativeArray<PlatformerGraph.Edge> undirectedEdges;
            public PlatformerGraph graph;
   
            public void Execute()
            {
                graph.Populate(vertices, undirectedEdges);
            }
        }
        
        [BurstCompile]
        struct PopulateFullJob : IJob
        {
            public NativeArray<float2> vertices;
            public NativeArray<PlatformerGraph.Edge> undirectedEdges;
            public NativeArray<PlatformerGraph.Edge> directedEdges;
            public PlatformerGraph graph;
   
            public void Execute()
            {
                graph.Populate(vertices, undirectedEdges, directedEdges);
            }
        }
        
        /// <summary>
        /// Schedules a job that populates the PlatformerGraph with only undirected edges
        /// </summary>
        /// <example>
        /// <code>
        /// // Example on how to weld vertices and populate the PlatformerGraph using Unity's Job system
        /// // these NativeLists and arrays will be modified in place
        /// var weldHandle = PlatformerGraphWelder.ScheduleWeld(vertices, directedEdges);
        ///     
        /// // Schedule the populate job with a dependency on the weld job handle
        /// // Pass in the welded verts as a deffered array
        /// var populateHandle =
        ///     PlatformerGraphPopulator.SchedulePopulate(graph, vertices.AsDeferredJobArray(), undirectedEdges, weldHandle);
        ///     
        /// // complete the job, or wait for the result somewhere else
        /// populateHandle.Complete();
        /// </code>
        /// </example>
        public static JobHandle SchedulePopulate(
            this PlatformerGraph graph, NativeArray<float2> vertices, NativeArray<PlatformerGraph.Edge> undirectedEdges, JobHandle dependsOn = default)
        {
            var job = new PopulateJob()
            {
                graph = graph,
                vertices = vertices,
                undirectedEdges = undirectedEdges
            };

            return job.Schedule(dependsOn);
        }
        
        /// <summary>
        /// Schedules a job that populates the PlatformerGraph with undirected edges and directed edges
        /// </summary>
        /// <example>
        /// <code>
        /// // Example on how to weld vertices and populate the PlatformerGraph using Unity's Job system
        /// // these NativeLists and arrays will be modified in place
        /// var weldHandle = PlatformerGraphWelder.ScheduleWeld(vertices, directedEdges, undirectedEdges);
        ///     
        /// // Schedule the populate job with a dependency on the weld job handle
        /// // Pass in the welded verts as a deffered array
        /// var populateHandle =
        ///     PlatformerGraphPopulator.SchedulePopulate(graph, vertices.AsDeferredJobArray(), undirectedEdges, directedEdges, weldHandle);
        ///     
        /// // complete the job, or wait for the result somewhere else
        /// populateHandle.Complete();
        /// </code>
        /// </example>
        public static JobHandle SchedulePopulate(
            this PlatformerGraph graph, NativeArray<float2> vertices, 
            NativeArray<PlatformerGraph.Edge> undirectedEdges, NativeArray<PlatformerGraph.Edge> directedEdges, JobHandle dependsOn = default)
        {
            var job = new PopulateFullJob()
            {
                graph = graph,
                vertices = vertices,
                undirectedEdges = undirectedEdges,
                directedEdges = directedEdges
            };

            return job.Schedule(dependsOn);
        }
    }
}