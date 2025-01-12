using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Utility to populate a NavMesh using Unity's Job system. This can be useful if you have frequent updates and want
    /// this process to be as fast as possible and/or on another thread.
    /// </summary>
    /// <example>
    /// <code>
    /// <summary>
    /// Demonstrates how to initialize the navmesh by doing all of the heavy lifting using Unity's Job System
    /// This can be benificial for large meshes or when you need to update your mesh frequently. As most of the work
    /// can be performed on another thread.
    /// </summary>
    /// void StartPopulateWithJob()
    /// {
    ///     // pre allocate our navmesh graph:
    ///     graph = new NavMeshGraph(Allocator.Persistent);
    ///    
    ///     // Obtain the vertices and triangle indices from our mesh:
    ///     var mesh = meshFilter.mesh;
    ///     NativeList{Vector3} vertices = new NativeList{Vector3}(Allocator.TempJob);
    ///     vertices.CopyFromNBC(mesh.vertices);
    ///     var indices = new NativeArray{int}(mesh.triangles, Allocator.TempJob);
    ///     
    ///     // We'll do both the welding of vertices and navmesh populating using the Job system
    ///     
    ///     // schedule a job that welds the vertices together
    ///     var weldJobHandle = NavMeshWelder.ScheduleWeld(vertices, indices);
    ///     
    ///     // schedule a job that generates the navmesh data
    ///     // we pass in the weld JobHandle to the populate job, as we want to populate the mesh with our welded verts
    ///     // notice that we must pass in the vertices as a deffered job array, because at the time of schedule the list is still empty
    ///     var populateHandle = NavMeshPopulator.SchedulePopulate(graph, 
    ///         vertices.AsDeferredJobArray(), 
    ///         indices,
    ///         meshFilter.transform.localToWorldMatrix, // we want our navmesh to be in worldspace, supplying this matrix of the mesh's transform will fix this
    ///         weldJobHandle);
    /// 
    ///     // after our navmesh is completed, we can dispose of these temp containers
    ///     vertices.Dispose(populateHandle);
    ///     indices.Dispose(populateHandle);
    /// 
    ///     populateHandle.Complete(); // or use Schedule and monitor when the job is done, afterwards the graph is usable
    /// }    
    /// </code>
    /// </example>
    /// <remarks>It's advised to always create a new NavMesh for updates, to avoid safety issues. If you're worried about
    /// too many allocations, a double buffering technique can be used where you swap two NavMeshes after an update is completed</remarks>
    public static class NavMeshPopulator
    {
        [BurstCompile]
        struct PopulateJob : IJob
        {
            public NativeArray<int> indices;
            public NativeArray<Vector3> vertices;
            public NavMeshGraph graph;
            public Matrix4x4 localToWorldMatrix;

            public void Execute()
            {
                graph.Populate(vertices, indices, default, localToWorldMatrix);
            }
        }
        
        [BurstCompile]
        struct PopulateJobFull : IJob
        {
            public NativeArray<int> indices;
            public NativeArray<Vector3> vertices;
            public NativeArray<NavMeshGraph.EnterCostAndFlags> enterCostAndFlags;
            public NavMeshGraph graph;
            public Matrix4x4 localToWorldMatrix;

            public void Execute()
            {
                graph.Populate(vertices, indices, enterCostAndFlags, localToWorldMatrix);
            }
        }
        
        /// <summary>
        /// Schedules a job that populates the NavMesh
        /// </summary>
        /// <param name="graph">The NavMesh to populate</param>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="indices">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle</param>
        public static JobHandle SchedulePopulate(
            this NavMeshGraph graph, NativeArray<Vector3> vertices, NativeArray<int> indices, JobHandle dependsOn = default)
        {
            var job = new PopulateJob()
            {
                graph = graph,
                vertices = vertices,
                indices = indices,
                localToWorldMatrix = Matrix4x4.identity,
            };

            return job.Schedule(dependsOn);
        }
        
        /// <summary>
        /// Schedules a job that populates the NavMesh
        /// </summary>
        /// <param name="graph">The NavMesh to populate</param>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="indices">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle</param>
        /// <param name="localToWorldMatrix">
        /// The local to world matrix to use. This is useful if you want the navmesh to use world space coordinates.
        /// Use Matrix4x4.identity if the vertices are already in world space or if you want to keep the coordinates in the local space of the mesh.</param>
        public static JobHandle SchedulePopulate(
            this NavMeshGraph graph, NativeArray<Vector3> vertices, NativeArray<int> indices, Matrix4x4 localToWorldMatrix, JobHandle dependsOn = default)
        {
            var job = new PopulateJob()
            {
                graph = graph,
                vertices = vertices,
                indices = indices,
                localToWorldMatrix = localToWorldMatrix,
            };

            return job.Schedule(dependsOn);
        }
        
        /// <summary>
        /// Schedules a job that populates the NavMesh
        /// </summary>
        /// <param name="graph">The NavMesh to populate</param>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="indices">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle</param>
        /// <param name="enterCostAndFlags">Array with cost and flags per triangle. Note that one triangle equals 3 indices in the triangles array.
        /// So index zero in this array corresponds to the first set of 3 in the triangles array.
        /// Length should be the amount of triangles. (triangles parameter's length divided by 3).
        /// default is allowed for this parameter, and will assign no extra cost and flags to the triangles.</param>
        public static JobHandle SchedulePopulate(
            this NavMeshGraph graph,
            NativeArray<Vector3> vertices,
            NativeArray<int> indices,
            NativeArray<NavMeshGraph.EnterCostAndFlags> enterCostAndFlags, JobHandle dependsOn = default)
        {
            return SchedulePopulate(graph, vertices, indices, enterCostAndFlags, Matrix4x4.identity, dependsOn);
        }

        /// <summary>
        /// Schedules a job that populates the NavMesh
        /// </summary>
        /// <param name="graph">The NavMesh to populate</param>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="indices">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle</param>
        /// <param name="enterCostAndFlags">Array with cost and flags per triangle. Note that one triangle equals 3 indices in the triangles array.
        /// So index zero in this array corresponds to the first set of 3 in the triangles array.
        /// Length should be the amount of triangles. (triangles parameter's length divided by 3).
        /// default is allowed for this parameter, and will assign no extra cost and flags to the triangles.</param>
        /// <param name="localToWorldMatrix">
        /// The local to world matrix to use. This is useful if you want the navmesh to use world space coordinates.
        /// Use Matrix4x4.identity if the vertices are already in world space or if you want to keep the coordinates in the local space of the mesh.</param>
        public static JobHandle SchedulePopulate(
            this NavMeshGraph graph,
            NativeArray<Vector3> vertices,
            NativeArray<int> indices,
            NativeArray<NavMeshGraph.EnterCostAndFlags> enterCostAndFlags, Matrix4x4 localToWorldMatrix, JobHandle dependsOn = default)
        {
            var job = new PopulateJobFull()
            {
                graph = graph,
                vertices = vertices,
                indices = indices,
                enterCostAndFlags = enterCostAndFlags,
                localToWorldMatrix = localToWorldMatrix,
            };

            return job.Schedule(dependsOn);
        }
    }
}