using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AnyPath.Graphs.Line
{
    /// <summary>
    /// Utility to populate LineGraph using Unity's Job system. This can be useful if you have frequent updates and want
    /// this process to be as fast as possible and/or on another thread.
    /// </summary>
    public static class LineGraphPopulator
    {
        [BurstCompile]
        struct PopulateJob : IJob
        {
            public NativeArray<float3> vertices;
            public NativeArray<LineGraph.Edge> undirectedEdges;
            public LineGraph graph;
   
            public void Execute()
            {
                graph.Populate(vertices, undirectedEdges);
            }
        }
        
        [BurstCompile]
        struct PopulateFullJob : IJob
        {
            public NativeArray<float3> vertices;
            public NativeArray<LineGraph.Edge> undirectedEdges;
            public NativeArray<LineGraph.Edge> directedEdges;
            public LineGraph graph;
   
            public void Execute()
            {
                graph.Populate(vertices, undirectedEdges, directedEdges);
            }
        }
        
        /// <summary>
        /// Schedules a job that populates the LineGraph with only undirected edges
        /// </summary>
        /// <example>
        /// <code>
        /// // Example on how to weld vertices and populate the LineGraph using Unity's Job system
        /// // these NativeLists and arrays will be modified in place
        /// var weldHandle = LineGraphWelder.ScheduleWeld(vertices, directedEdges);
        ///     
        /// // Schedule the populate job with a dependency on the weld job handle
        /// // Pass in the welded verts as a deffered array
        /// var populateHandle =
        ///     LineGraphPopulator.SchedulePopulate(graph, vertices.AsDeferredJobArray(), undirectedEdges, weldHandle);
        ///     
        /// // complete the job, or wait for the result somewhere else
        /// populateHandle.Complete();
        /// </code>
        /// </example>
        public static JobHandle SchedulePopulate(
            this LineGraph graph, NativeArray<float3> vertices, NativeArray<LineGraph.Edge> undirectedEdges, JobHandle dependsOn = default)
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
        /// Schedules a job that populates the LineGraph with undirected edges and directed edges
        /// </summary>
        /// <example>
        /// <code>
        /// // Example on how to weld vertices and populate the LineGraph using Unity's Job system
        /// // these NativeLists and arrays will be modified in place
        /// var weldHandle = LineGraphhWelder.ScheduleWeld(vertices, directedEdges, undirectedEdges);
        ///     
        /// // Schedule the populate job with a dependency on the weld job handle
        /// // Pass in the welded verts as a deffered array
        /// var populateHandle =
        ///     LineGraphPopulator.SchedulePopulate(graph, vertices.AsDeferredJobArray(), undirectedEdges, directedEdges, weldHandle);
        ///     
        /// // complete the job, or wait for the result somewhere else
        /// populateHandle.Complete();
        /// </code>
        /// </example>
        public static JobHandle SchedulePopulate(
            this LineGraph graph, NativeArray<float3> vertices, 
            NativeArray<LineGraph.Edge> undirectedEdges, NativeArray<LineGraph.Edge> directedEdges, JobHandle dependsOn = default)
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