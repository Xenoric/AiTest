using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AnyPath.Graphs.Extra;
using AnyPath.Native;
using AnyPath.NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;
using static Unity.Mathematics.math;
using Debug = UnityEngine.Debug;
using float3 = Unity.Mathematics.float3;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Describes a function that validates if a location on a triangle is valid as a return value for the Closest queries.
    /// This can be used for instance to check if there are objects obstructing the line of sight between the origin and the location.
    /// </summary>
    public delegate bool ClosestNavMeshLocationPredicate(float3 origin, NavMeshGraphLocation location);
    
    /// <summary>
    /// <para>
    /// Navigation Mesh implementation with support for raycasting, area cost, flags and arbitrarily curved surfaces.
    /// </para>
    /// <para>
    /// v1.1 adds features to populate the graph from within jobs. See <see cref="Populate(Unity.Collections.NativeArray{UnityEngine.Vector3},Unity.Collections.NativeArray{int})"/>
    /// as well as some variations on this, including one that copies the contents from another NavMehsGraph, which can be efficient if
    /// you need to make slight adjustments to the graph as fast as possible.
    /// </para>
    /// <para>
    /// <see cref="SetUnwalkable"/>, which can be used to essentialy cut some areas from the NavMesh.
    /// Depending on your use case, this may be a very efficient way to dynamically alter the graph without a lot of overhead.
    /// Using <see cref="GetOverlappingTriangles"/>, an area can be scanned and then these triangles can be excluded from pathfinding queries.
    /// </para>
    /// <para>
    /// Paths can be processed using:
    /// <see cref="NavMeshGraphCorners"/> - creates a nice corner array from a starting position to the goal for flat worlds with slopes
    /// <see cref="NavMeshGraphCorners3D"/> - creates a nice corner array for arbitrarily curved worlds
    /// <see cref="NavMeshGraphUnroller"/> - processes the path so that it can be used for realtime steering behaviour
    /// </para>
    /// </summary>
    /// <remarks>Since v1.1, all of the constructors make an internal copy of the supplied arrays. Also, any triangle
    /// that has a cost of infinity is considered unwalkable and will never be part of a path</remarks>
    public struct NavMeshGraph : IGraph<NavMeshGraphLocation>
    {
        /// <summary>
        /// A struct per triangle that holds information about extra cost and flags.
        /// </summary>
        public struct EnterCostAndFlags
        {
            /// <summary>
            /// Additional cost associated with walking this triangle. This value gets added to the distance from the previous triangle.
            /// So a value higher than zero discourages A* to travel this triangle.
            /// </summary>
            public readonly float enterCost;

            /// <summary>
            /// Other properties of the triangle that can be used with a bitmask to exclude triangles from a query.
            /// </summary>
            public readonly int flags;

            public EnterCostAndFlags(float enterCost, int flags)
            {
                this.enterCost = enterCost;
                this.flags = flags;
            }

            /// <summary>
            /// A cost of infinity. Apply to a triangle to essentialy remove it from the graph, as this triangle becomes
            /// unreachable.
            /// </summary>
            public static readonly EnterCostAndFlags Unwalkable = new EnterCostAndFlags(float.PositiveInfinity, 0);
        }

        private NativeList<int> adjecency;
        private NativeList<int> triangles;
        private NativeList<EnterCostAndFlags> enterCostAndFlags;
        private NativeOctree<int> octree;
        private NativeList<Vector3> vertices;

        /// <summary>
        /// Read only access to the triangle indices of the mesh. Each triangle has three consecutive ints pointing to the
        /// index in the <see cref="Vertices"/> array
        /// </summary>
        public NativeArray<int>.ReadOnly Triangles => triangles.AsParallelReader();

        /// <summary>
        /// Read only access to the vertices of the mesh
        /// </summary>
        public NativeArray<Vector3>.ReadOnly Vertices => vertices.AsParallelReader();

        /// <summary>
        /// Read only access to the enter cost and flags of the mesh
        /// </summary>
        public NativeArray<EnterCostAndFlags>.ReadOnly CostAndFlags => enterCostAndFlags.AsParallelReader();

        /// <summary>
        /// Access to the internal octree of the graph for advanced location queries.
        /// </summary>
        /// <remarks>Warning: do not modify the octree as this will corrupt the state of the graph.</remarks>
        public NativeOctree<int> Octree => octree;


        /// <summary>
        /// Construct a NavMesh from a set of vertices and triangles. The NavMesh is populated automatically.
        /// </summary>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="triangles">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle</param>
        /// <param name="enterCostAndFlags">Array with cost and flags per triangle. Note that one triangle equals 3 indices in the triangles array.
        /// So index zero in this array corresponds to the first set of 3 in the triangles array.
        /// Length should be the amount of triangles. (triangles parameter's length divided by 3).
        /// Null is allowed for this parameter, and will assign no extra cost and flags to the triangles.</param>
        /// <param name="localToWorldMatrix">
        /// The local to world matrix to use. This is useful if you want the navmesh to use world space coordinates.
        /// Use Matrix4x4.identity if the vertices are already in world space or if you want to keep the coordinates in the local space of the mesh.</param>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="trianglesPerOctant">Max triangles per octant for the internal octree. Depending on </param>
        /// <param name="maxOctreeDepth">Max depth of the internal octree for raycast and closest location accellerating. If your mesh is very large, it may be beneficial
        /// to increase this value. Finding a good balance is key to having optimal raycast and closest location performance.</param>
        /// <remarks>
        /// <para>
        /// Note that the vertices may need to be welded together first, depending on your source of data.
        /// </para>
        /// <para>Not super efficient, it's advised to use another constructor overload</para>
        /// </remarks>
        public NavMeshGraph(List<Vector3> vertices, List<int> triangles,
            Matrix4x4 localToWorldMatrix, Allocator allocator, List<EnterCostAndFlags> enterCostAndFlags = null,
            int trianglesPerOctant = 16, int maxOctreeDepth = 5)
            : this(allocator, trianglesPerOctant, maxOctreeDepth)
        {
            var nVertices = FromList(vertices, Allocator.Temp);
            var nTriangles = FromList(triangles, Allocator.Temp);
            var nCostFlags = enterCostAndFlags != null
                ? FromList(enterCostAndFlags, Allocator.Temp)
                : default(NativeArray<EnterCostAndFlags>);

            Populate(nVertices, nTriangles, nCostFlags, localToWorldMatrix);

            nVertices.Dispose();
            nTriangles.Dispose();
            if (enterCostAndFlags != null)
                nCostFlags.Dispose();
        }

        /// <summary>
        /// Construct a NavMesh from a set of vertices and triangles. The NavMesh is populated automatically.
        /// </summary>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="triangles">Triangle indices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="enterCostAndFlags">Array with cost and flags per triangle. Length should be the amount of triangles. (triangles array divided by 3).
        /// Null is allowed for this parameter, and will assign no extra cost and flags to the triangles.</param>
        /// <param name="localToWorldMatrix">
        /// The local to world matrix to use.
        /// Use Matrix4x4.identity if the vertices are already in world space or if you want to keep the coordinates in the local mesh space.</param>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="trianglesPerOctant">Max triangles per octant for the internal octree</param>
        /// <param name="maxOctreeDepth">Max depth of the internal octree for raycast and closest location accellerating. If your mesh is very large, it may be beneficial
        /// to increase this value. Finding a good balance is key to having optimal raycast and closest location performance.</param>
        /// <remarks>An internal copy is made of the supplied arrays</remarks>
        public NavMeshGraph(Vector3[] vertices, int[] triangles,
            Matrix4x4 localToWorldMatrix, Allocator allocator, EnterCostAndFlags[] enterCostAndFlags = null,
            int trianglesPerOctant = 16, int maxOctreeDepth = 5)
            : this(allocator, trianglesPerOctant, maxOctreeDepth)
        {
            Populate(vertices, triangles, enterCostAndFlags, localToWorldMatrix);
        }

        /// <summary>
        /// Construct a NavMesh from a set of vertices and triangles. The NavMesh is populated automatically.
        /// </summary>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="triangles">Triangle indices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="enterCostAndFlags">Array with cost and flags per triangle. Length should be the amount of triangles. (triangles array divided by 3).</param>
        /// <param name="localToWorldMatrix">
        /// The local to world matrix to use. This is useful if you want the navmesh to use world space coordinates.
        /// Use Matrix4x4.identity if the vertices are already in world space or if you want to keep the coordinates in the local space of the mesh.</param>
        /// <param name="allocator">Allocator to use. Make sure to use the same allocator as the supplied arrays.</param>
        /// <param name="trianglesPerOctant">Max triangles per octant for the internal octree</param>
        /// <param name="maxOctreeDepth">Max depth of the internal octree for raycast and closest location accellerating. If your mesh is very large, it may be beneficial
        /// to increase this value. Finding a good balance is key to having optimal raycast and closest location performance.</param>
        /// <remarks>
        /// Breaking change: the supplied arrays are copied as of v1.1. Dispose of the native arrays separately.
        ///</remarks>
        public NavMeshGraph(
            NativeArray<Vector3> vertices,
            NativeArray<int> triangles,
            NativeArray<EnterCostAndFlags> enterCostAndFlags,
            Matrix4x4 localToWorldMatrix, Allocator allocator, int trianglesPerOctant = 16, int maxOctreeDepth = 5) :

            this(allocator, trianglesPerOctant, maxOctreeDepth, vertices.Length, triangles.Length)
        {
            Populate(vertices, triangles, enterCostAndFlags, localToWorldMatrix);
        }

        /// <summary>
        /// Constructs an empty nav mesh graph, to be populated later with any of the
        /// <see cref="Populate(Unity.Collections.NativeArray{UnityEngine.Vector3},Unity.Collections.NativeArray{int})"/> methods.
        /// </summary>
        /// <param name="allocator">Allocator to use. Make sure to use the same allocator as the supplied arrays.</param>
        /// <param name="trianglesPerOctant">Max triangles per octant for the internal octree</param>
        /// <param name="maxOctreeDepth">Max depth of the internal octree for raycast accellerating</param>
        /// <param name="initialVertexCapacity">Pre allocate memory for the vertices, if you know how large your mesh is going to be, this helps
        /// with performance when populating</param>
        /// <param name="initialIndicesCapacity">Pre allocate memory for the triangle indices, if you know how large your mesh is going to be, this helps
        /// with performance when populating</param>
        public NavMeshGraph(Allocator allocator, int trianglesPerOctant = 16, int maxOctreeDepth = 5,
            int initialVertexCapacity = 0, int initialIndicesCapacity = 0)
        {
            this.vertices = new NativeList<Vector3>(initialVertexCapacity, allocator);
            this.triangles = new NativeList<int>(initialIndicesCapacity, allocator);
            this.enterCostAndFlags = new NativeList<EnterCostAndFlags>(initialIndicesCapacity / 3, allocator);
            this.adjecency = new NativeList<int>(initialIndicesCapacity, allocator);
            this.octree = new NativeOctree<int>(new AABB(float3.zero, new float3(1, 1, 1)), trianglesPerOctant, maxOctreeDepth, allocator);
        }

        #region Population

        //public bool IsPopulated => octree.NodeCount > 1;

        /// <summary>
        /// Populates the graph with data and calculates everything neccessary to perform pathfinding on the mesh
        /// </summary>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed. Note that an internal copy is made</param>
        /// <param name="triangles">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle. Note that an internal copy is made</param>
        /// <remarks>
        /// <para>
        /// This method is burst compatible, meaning it can be used inside a job to populate the graph with high performance, if you
        /// need frequent updates.
        /// </para>
        /// <para>
        /// Note that this method writes to the graph and as such it should not be used while there are
        /// active pathfinding queries running on it.
        /// </para>
        /// </remarks>
        public void Populate(
            NativeArray<Vector3> vertices,
            NativeArray<int> triangles)
        {
            Populate(vertices, triangles, default, Matrix4x4.identity);
        }

        /// <summary>
        /// Populates the graph with data and calculates everything neccessary to perform pathfinding on the mesh
        /// </summary>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed. Note that an internal copy of the array is made</param>
        /// <param name="triangles">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle. Note that an internal copy is made</param>
        /// <param name="enterCostAndFlags">Array with cost and flags per triangle. Note that one triangle equals 3 indices in the triangles array.
        /// So index zero in this array corresponds to the first set of 3 in the triangles array.
        /// Length should be the amount of triangles. (triangles parameter's length divided by 3).
        /// Null is allowed for this parameter, and will assign no extra cost and flags to the triangles.</param>
        /// <remarks>
        /// <para>
        /// This method is burst compatible, meaning it can be used inside a job to populate the graph with high performance, if you
        /// need frequent updates.
        /// </para>
        /// <para>
        /// Note that this method writes to the graph and as such it should not be used while there are
        /// active pathfinding queries running on it.
        /// </para>
        /// </remarks>
        public void Populate(
            NativeArray<Vector3> vertices,
            NativeArray<int> triangles,
            NativeArray<EnterCostAndFlags> enterCostAndFlags)
        {
            Populate(vertices, triangles, enterCostAndFlags, Matrix4x4.identity);
        }

        /// <summary>
        /// Populates the graph with data and calculates everything neccessary to perform pathfinding on the mesh
        /// </summary>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="triangles">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle</param>
        /// <param name="enterCostAndFlags">Array with cost and flags per triangle. Note that one triangle equals 3 indices in the triangles array.
        /// So index zero in this array corresponds to the first set of 3 in the triangles array.
        /// Length should be the amount of triangles. (triangles parameter's length divided by 3).
        /// default is allowed for this parameter, and will assign no extra cost and flags to the triangles.</param>
        /// <param name="localToWorldMatrix">
        /// The local to world matrix to use. This is useful if you want the navmesh to use world space coordinates.
        /// Use Matrix4x4.identity if the vertices are already in world space or if you want to keep the coordinates in the local space of the mesh.</param>
        /// <remarks>
        /// <para>
        /// This method is burst compatible, meaning it can be used inside a job to populate the graph with high performance, if you
        /// need frequent updates.
        /// </para>
        /// <para>
        /// Note that this method writes to the graph and as such it should not be used while there are
        /// active pathfinding queries running on it.
        /// </para>
        /// </remarks>
        public void Populate(
            NativeArray<Vector3> vertices,
            NativeArray<int> triangles,
            NativeArray<EnterCostAndFlags> enterCostAndFlags,
            Matrix4x4 localToWorldMatrix)
        {
            this.vertices.CopyFrom(vertices);
            if (!localToWorldMatrix.isIdentity)
            {
                for (int i = 0; i < vertices.Length; i++)
                    this.vertices[i] = localToWorldMatrix.MultiplyPoint(vertices[i]); //localToWorldMatrix * vertices[i];
            }

            this.triangles.CopyFrom(triangles);
            if (enterCostAndFlags.IsCreated)
                this.enterCostAndFlags.CopyFrom(enterCostAndFlags);
            else
                this.enterCostAndFlags.Resize(triangles.Length / 3, NativeArrayOptions.ClearMemory);

            Calculate();
        }

        /// <summary>
        /// Copies the contents of the source graph into this graph. This can be useful if you want *almost* the exact same
        /// graph but will apply small modifications using <see cref="SetUnwalkable"/> or <see cref="SetEnterCostAndFlags"/>
        /// on this graph after populating it.
        /// </summary>
        /// <param name="source">The source graph to copy</param>
        /// <remarks>
        /// <para>
        /// This method is burst compatible, meaning it can be used inside a job to populate the graph with high performance, if you
        /// need frequent updates.
        /// </para>
        /// <para>
        /// Note that this method writes to the graph and as such it should not be used while there are
        /// active pathfinding queries running on it. The source graph is allowed be in use though.
        /// </para>
        /// </remarks>
        public void Populate(NavMeshGraph source)
        {
            this.vertices.Resize(source.Vertices.Length, NativeArrayOptions.UninitializedMemory);
            source.Vertices.CopyTo(this.vertices.AsArray());

            this.triangles.Resize(source.Triangles.Length, NativeArrayOptions.UninitializedMemory);
            source.Triangles.CopyTo(this.triangles.AsArray());

            this.enterCostAndFlags.Resize(source.CostAndFlags.Length, NativeArrayOptions.UninitializedMemory);
            source.CostAndFlags.CopyTo(this.enterCostAndFlags.AsArray());

            this.adjecency.Resize(source.adjecency.Length, NativeArrayOptions.UninitializedMemory);
            source.adjecency.AsParallelReader().CopyTo(this.adjecency.AsArray());

            this.octree.CopyFrom(source.octree);
        }


        /// <summary>
        /// <para>
        /// Overwrites the cost and flags for a triangle, which can be obtained via <see cref="RaycastTriangle"/>
        /// or <see cref="GetOverlappingTriangles(AnyPath.NativeTrees.AABB,Unity.Collections.NativeList{int})"/>.
        /// </para>
        /// <para>
        /// This can be useful if you've made a copy of a graph and want to apply slight modifications to it, or want
        /// to exclude certain triangles from being walkable (essentialy removing them)
        /// </para>
        /// <para>
        /// To make a triangle unwalkable, supply <see cref="EnterCostAndFlags.Unwalkable"/> as a parameter.
        /// Note that the triangle still exists in the navmesh, so it will still be returned from a raycast query.
        /// But A* will never use them in a path.
        /// </para>
        /// </summary>
        /// <param name="triangleIndex">The index of the triangle</param>
        /// <param name="costAndFlags">The new cost and flags parameters for this triangle</param>
        /// <remarks>Note that this method writes to the graph and as such it should not be used while there are
        /// active pathfinding queries running on it.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEnterCostAndFlags(int triangleIndex, EnterCostAndFlags costAndFlags)
        {
            this.enterCostAndFlags[triangleIndex] = costAndFlags;
        }

        /// <summary>
        /// <para>
        /// Makes a triangle unwalkable. Making it impossible for A* to navigate through it.
        /// Note that the triangle still exists in the navmesh, so it will still be returned from a raycast query.
        /// </para>
        /// </summary>
        /// <param name="triangleIndex">The index of the triangle, which can be obtained via <see cref="RaycastTriangle"/> or <see cref="GetOverlappingTriangles"/></param>
        /// <remarks>
        /// <para>
        /// Note that this method writes to the graph and as such it should not be used while there are
        /// active pathfinding queries running on it.
        /// </para>
        /// <para>
        /// If you're comfortable with the flags being overwritten, <see cref="SetEnterCostAndFlags"/> with <see cref="EnterCostAndFlags.Unwalkable"/> is faster.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetUnwalkable(int triangleIndex)
        {
            var e = enterCostAndFlags[triangleIndex];
            enterCostAndFlags[triangleIndex] = new EnterCostAndFlags(float.PositiveInfinity, e.flags);
        }

        /// <summary>
        /// Populates the graph with data and calculates everything neccessary to perform pathfinding on the mesh
        /// </summary>
        /// <param name="vertices">Vertices to use. Similar to how a Unity mesh is constructed.</param>
        /// <param name="triangles">Array describing the triangles using the indices in the vertex array. Length must be a multiple of 3 as each set
        /// of 3 indices describres a triangle. Note that an internal copy is made</param>
        public void Populate(Vector3[] vertices, int[] triangles)
        {
            Populate(vertices, triangles, null);
        }

        [ExcludeFromDocs]
        public void Populate(Vector3[] vertices, int[] triangles, EnterCostAndFlags[] enterCostAndFlags)
        {
            Populate(vertices, triangles, enterCostAndFlags, Matrix4x4.identity);
        }

        [ExcludeFromDocs]
        public void Populate(
            Vector3[] vertices,
            int[] triangles,
            EnterCostAndFlags[] enterCostAndFlags, Matrix4x4 localToWorldMatrix)
        {
            this.vertices.CopyFromNBC(vertices);
            if (!localToWorldMatrix.isIdentity)
            {
                for (int i = 0; i < vertices.Length; i++)
                    this.vertices[i] = localToWorldMatrix.MultiplyPoint(vertices[i]); //localToWorldMatrix * vertices[i];
            }

            this.triangles.CopyFromNBC(triangles);

            if (enterCostAndFlags != null)
                this.enterCostAndFlags.CopyFromNBC(enterCostAndFlags);
            else
                this.enterCostAndFlags.Resize(triangles.Length / 3, NativeArrayOptions.ClearMemory);

            Calculate();
        }

        /// <summary>
        /// Calculates the adjecency data and internal hierarchy for the navigation mesh.
        /// This is useful if your mesh is very large and you want to perform this on a separate thread. This method can run inside
        /// a job that has a pre-allocated NavMesh struct.
        /// </summary>
        private void Calculate()
        {
#if UNITY_EDITOR
            int degenerates = 0;
            int duplicateTris = 0;
            int duplicateEdges = 0;
#endif

            CheckThrowLengths();

            int triangleCount = triangles.Length / 3;
            float3 boundsMin = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            float3 boundsMax = new float3(float.MinValue, float.MinValue, float.MinValue);
            var halfEdges = new NativeHashMap<int2, int>(0, Allocator.Temp);
            var aabbs = new NativeArray<AABB>(triangleCount, Allocator.Temp);
            var triangleIndexes = new NativeArray<int>(triangleCount, Allocator.Temp);

            // https://gamedev.stackexchange.com/questions/62097/building-triangle-adjacency-data
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int t0 = triangles[i];
                int t1 = triangles[i + 1];
                int t2 = triangles[i + 2];
                int triangleIndex = i / 3;

                bool a1 = halfEdges.TryAdd(new int2(t0, t1), triangleIndex);
                bool a2 = halfEdges.TryAdd(new int2(t1, t2), triangleIndex);
                bool a3 = halfEdges.TryAdd(new int2(t2, t0), triangleIndex);

                Vector3 a = this.vertices[t0];
                Vector3 b = this.vertices[t1];
                Vector3 c = this.vertices[t2];

#if UNITY_EDITOR
                // Unity editor mesh checks

                if (!a1 && !a2 && !a3)
                    duplicateTris++;
                else if (!a1 || !a2 || !a3)
                    duplicateEdges++;

                if (new Triangle(a, b, c).IsDegenerate())
                    degenerates++;

#endif

                var aabb = new AABB(min(min(a, b), c), max(max(a, b), c));
                triangleIndexes[triangleIndex] = triangleIndex;
                aabbs[triangleIndex] = aabb;

                boundsMin = min(boundsMin, min(min(a, b), c));
                boundsMax = max(boundsMax, max(max(a, b), c));
            }
            
            adjecency.Clear();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int t0 = triangles[i];
                int t1 = triangles[i + 1];
                int t2 = triangles[i + 2];
                adjecency.Add(halfEdges.TryGetValue(new int2(t1, t0), out int triangleIndex) ? triangleIndex : -1);
                adjecency.Add(halfEdges.TryGetValue(new int2(t2, t1), out triangleIndex) ? triangleIndex : -1);
                adjecency.Add(halfEdges.TryGetValue(new int2(t0, t2), out triangleIndex) ? triangleIndex : -1);
            }

            // add some margin to the bounds, in case all triangles lie in a flat plane
            octree.Clear(new AABB(boundsMin - float3(1, 1, 1), boundsMax + float3(1, 1, 1)));
            for (int i = 0; i < aabbs.Length; i++)
                octree.Insert(triangleIndexes[i], aabbs[i]);

            halfEdges.Dispose();
            aabbs.Dispose();
            triangleIndexes.Dispose();

#if UNITY_EDITOR
            if (duplicateTris > 0)
                Debug.LogWarning($"Mesh contains duplicate triangles");
            if (duplicateEdges > 0)
                Debug.LogWarning($"Mesh contains duplicate edges");
            if (degenerates > 0)
                Debug.LogWarning($"Mesh contains degenerate triangles");
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckThrowLengths()
        {
            int triangleCount = triangles.Length / 3;
            if (triangles.Length % 3 != 0)
                throw new ArgumentException("Array length should be divisible by 3", nameof(triangles));
            if (triangles.Length < 3)
                throw new ArgumentException("Array length should be greater than or equal to 3", nameof(triangles));
            if (enterCostAndFlags.Length != triangleCount)
                throw new ArgumentException($"Length does match the triangle count",
                    nameof(enterCostAndFlags));
            if (vertices.Length == 0)
                throw new ArgumentException("Vertices length must be greater than 0");
        }

        private static NativeArray<T> FromList<T>(List<T> list, Allocator allocator) where T : unmanaged
        {
            var nativeArray = new NativeArray<T>(list.Count, allocator, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < nativeArray.Length; i++)
                nativeArray[i] = list[i];
            return nativeArray;
        }

        #endregion

        /// <summary>
        /// Amount of triangles contained in the mesh
        /// </summary>
        public int TriangleCount => adjecency.Length / 3;

        /// <summary>
        /// Returns a triangle by index
        /// </summary>
        /// <param name="index">The index of the triangle</param>
        /// <returns></returns>
        public Triangle GetTriangle(int index)
        {
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= TriangleCount)
                throw new ArgumentOutOfRangeException(nameof(index), "Must be >= 0 and < TriangleCount");
            #endif
            
            index *= 3;
            return new Triangle(
                vertices[triangles[index]],
                vertices[triangles[index + 1]],
                vertices[triangles[index + 2]]);
        }


        /// <summary>
        /// Returns the adjecent triangle indexes of a triangle.
        /// </summary>
        /// <param name="triangleIndex">The triangle's index to get the adjecent triangles from.</param>
        /// <param name="triangleIndex1">First adjecent triangle, -1 if none</param>
        /// <param name="triangleIndex2">Second adjecent triangle, -1 if none</param>
        /// <param name="triangleIndex3">Third adjecent triangle, -1 if none</param>
        public void GetAdjecency(int triangleIndex, out int triangleIndex1, out int triangleIndex2, out int triangleIndex3)
        {

#if UNITY_EDITOR
            if (triangleIndex >= TriangleCount) throw new ArgumentOutOfRangeException(nameof(triangleIndex));
#endif
            triangleIndex *= 3;
            triangleIndex1 = adjecency[triangleIndex];
            triangleIndex2 = adjecency[triangleIndex + 1];
            triangleIndex3 = adjecency[triangleIndex + 2];
        }

        public void Dispose()
        {
            triangles.Dispose();
            enterCostAndFlags.Dispose();
            vertices.Dispose();
            adjecency.Dispose();
            octree.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            NativeArray<JobHandle> tmp = new NativeArray<JobHandle>(5, Allocator.Temp);
            tmp[0] = vertices.Dispose(inputDeps);
            tmp[1] = adjecency.Dispose(inputDeps);
            tmp[2] = triangles.Dispose(inputDeps);
            tmp[3] = octree.Dispose(inputDeps);
            tmp[4] = enterCostAndFlags.Dispose(inputDeps);

            var handle = JobHandle.CombineDependencies(tmp);
            tmp.Dispose();
            return handle;
        }

        public void Collect(NavMeshGraphLocation location, ref NativeList<Edge<NavMeshGraphLocation>> edgeBuffer)
        {
            // catch invalid index in case the start/goal was queried with RaycastNode and no node was found
            if (location.TriangleIndex < 0 || location.TriangleIndex >= TriangleCount)
                return;

            GetAdjecency(location.TriangleIndex, out int i1, out int i2, out int i3);
            var prevTriangle = GetTriangle(location.TriangleIndex);

            if (i1 >= 0)
            {
                var costAndFlags = enterCostAndFlags[i1];
                if (isfinite(costAndFlags.enterCost))
                {
                    // using the closest point on the portal to the previous triangle gives far better results than using
                    // the triangle's centers, especially when some triangles are very large and others small. this will still encourage
                    // movement over a large triangle even though we only travel it for a small portion.
                    float3 nextPosition = GetClosestPoint(location.ExitPosition, prevTriangle.a, prevTriangle.b);

                    // since prevTriangle's connected at i1, the shared portal left-right is a-b
                    // we use the normal of the triangle we came from, since that's the up orientation we should travel
                    // when going to the next triangle.
                    var nextLocation = new NavMeshGraphLocation(i1, prevTriangle.a, prevTriangle.b, prevTriangle.Normal, nextPosition,
                        costAndFlags.flags);
                    float cost = distance(location.ExitPosition, nextPosition) + costAndFlags.enterCost;

                    edgeBuffer.Add(new Edge<NavMeshGraphLocation>(nextLocation, cost));
                }
            }

            if (i2 >= 0)
            {
                var costAndFlags = enterCostAndFlags[i2];
                if (isfinite(costAndFlags.enterCost))
                {
                    float3 nextPosition = GetClosestPoint(location.ExitPosition, prevTriangle.b, prevTriangle.c);

                    // since prevTriangle's connected at i2, the shared portal left-right is b-c
                    var nextLocation = new NavMeshGraphLocation(i2, prevTriangle.b, prevTriangle.c, prevTriangle.Normal, nextPosition,
                        costAndFlags.flags);
                    float cost = distance(location.ExitPosition, nextPosition) + costAndFlags.enterCost;

                    edgeBuffer.Add(new Edge<NavMeshGraphLocation>(nextLocation, cost));
                }
            }

            if (i3 >= 0)
            {
                var costAndFlags = enterCostAndFlags[i3];
                if (isfinite(costAndFlags.enterCost))
                {
                    float3 nextPosition = GetClosestPoint(location.ExitPosition, prevTriangle.c, prevTriangle.a);

                    // since prevTriangle's connected at i3, the shared portal left-right is c-a
                    var nextLocation = new NavMeshGraphLocation(i3, prevTriangle.c, prevTriangle.a, prevTriangle.Normal, nextPosition,
                        costAndFlags.flags);
                    float cost = distance(location.ExitPosition, nextPosition) + costAndFlags.enterCost;

                    edgeBuffer.Add(new Edge<NavMeshGraphLocation>(nextLocation, cost));
                }
            }
        }

        /// <summary>
        /// Returns the closest point on this line from a point
        /// </summary>
        private static float3 GetClosestPoint(float3 prevLeft, float3 a, float3 b)
        {
            float3 ba = b - a;
            float t = dot(prevLeft - a, ba) / dot(ba, ba);
            return lerp(a, b, saturate(t));
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Enumerator that enumerates all triangles/locations contained in this navigation mesh. This can be used to construct ALT heuristics.
        /// </summary>
        public struct Enumerator : IEnumerator<NavMeshGraphLocation>
        {
            private int index;
            [ReadOnly] private NavMeshGraph graph;

            public Enumerator(NavMeshGraph graph)
            {
                this.graph = graph;
                index = -1;
                Current = default;
            }

            public bool MoveNext()
            {
                if (++index < graph.TriangleCount)
                {
                    var tri = graph.GetTriangle(index);
                    Current = new NavMeshGraphLocation(index, tri.a, tri.b, tri.Normal, tri.c, graph.enterCostAndFlags[index].flags);
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            public NavMeshGraphLocation Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// Performs a raycast against the NavMesh and returns the closest triangle that was hit.
        /// </summary>
        /// <param name="ray">Ray to cast</param>
        /// <param name="triangle">The triangle that was hit</param>
        /// <param name="hitPoint">The point where the triangle was hit</param>
        /// <returns>True if there was a hit</returns>
        public bool RaycastTriangle(Ray ray, out Triangle triangle, out float3 hitPoint)
        {
            var intersecter = new RaycastCollector(this);
            if (octree.Raycast(ray, out var hit, intersecter))
            {
                int triangleIndex = hit.obj;
                triangle = GetTriangle(triangleIndex);
                hitPoint = hit.point;

                return true;
            }

            triangle = default;
            hitPoint = default;
            return false;
        }
        
        /// <summary>
        /// Performs a raycast against the NavMesh and returns the closest node that was hit.
        /// This can then be used as the starting node for a pathfinding request.
        /// The node contains information about the triangle as well as the exact location the path should start.
        /// </summary>
        /// <param name="ray">The ray to cast</param>
        /// <param name="location">The node that can be used as part of a path finding request</param>
        /// <returns>True if there was a hit</returns>
        public bool Raycast(Ray ray, out NavMeshGraphLocation location)
        {
            var intersecter = new RaycastCollector(this);
            if (octree.Raycast(ray, out var hit, intersecter))
            {
                int triangleIndex = hit.obj;
                var tri = GetTriangle(triangleIndex);
                location = new NavMeshGraphLocation(triangleIndex, hit.point, hit.point, tri.Normal, hit.point,
                    enterCostAndFlags[triangleIndex].flags);

                return true;
            }

            location = default;
            return false;
        }


        /// <summary>
        /// Returns a location at the center of a triangle.
        /// </summary>
        /// <param name="triangleIndex">The index of the triangle to derive a location from</param>
        /// <returns>A location which can be used for pathfinding queries</returns>
        public NavMeshGraphLocation LocationFromTriangleIndex(int triangleIndex)
        {
            var tri = GetTriangle(triangleIndex);
            var center = tri.Centroid;
            return new NavMeshGraphLocation(triangleIndex, center, center, tri.Normal, center,
                enterCostAndFlags[triangleIndex].flags);
        }

        
        struct RaycastCollector : IOctreeRayIntersecter<int>
        {
            private NavMeshGraph graph;

            public RaycastCollector(NavMeshGraph graph)
            {
                this.graph = graph;
            }

            public bool IntersectRay(in PrecomputedRay ray, int triangleIndex, AABB objBounds, out float distance)
            {
                if (!objBounds.IntersectsRay(ray))
                {
                    distance = default;
                    return false;
                }
                
                var tri = graph.GetTriangle(triangleIndex);
                return tri.Raycast((Ray)ray, out distance);
            }
        }
        
        /// <summary>
        /// Returns the closest location on the navmesh from a point.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="flagBitMask">A bitwise AND is performed on the triangle candidates and if any bit is true, the triangle is considered.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float3 position, float maxDistance, int flagBitMask, out NavMeshGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            var result = ClosestLocation(cache, position, maxDistance, true, flagBitMask, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the navmesh from a point.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="location">Returns the closest location on the navmesh</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float3 position, float maxDistance, out NavMeshGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            var result = ClosestLocation(cache, position, maxDistance, false, 0, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the navmesh from a point.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="flagBitMask">A bitwise AND is performed on the triangle candidates and if any bit is true, the triangle is considered.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, int flagBitMask, out NavMeshGraphLocation location)
        {
            return ClosestLocation(cache, position, maxDistance, true, flagBitMask, out location);
        }
        
        /// <summary>
        /// Returns the closest location on the navmesh from a point.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="location">Returns the closest location on the navmesh</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, out NavMeshGraphLocation location)
        {
            return ClosestLocation(cache, position, maxDistance, false, 0, out location);
        }
        
        private bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, bool useFlagBitMask, int flagBitMask, out NavMeshGraphLocation location)
        {
            var visitor = new NearestVisitor(this, position, useFlagBitMask, flagBitMask);
            cache.Nearest(ref octree, position, maxDistance, ref visitor, new DistanceProvider(this));
            location = visitor.closestLocation;
            return visitor.hasResult;
        }

        /// <summary>
        /// Returns the closest location on the navmesh from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited. Keeping this value within reasonable limits
        /// may dramatically increase performance.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the navmesh</param>
        /// <returns>Wether a location was found within the given radius</returns>
        /// <remarks>This function cannot be used in a burst compiled job context. Use the overload that accepts a <see cref="FunctionPointer{T}"/> instead.</remarks>
        public bool ClosestLocation(float3 position, float maxDistance, ClosestNavMeshLocationPredicate predicate, out NavMeshGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            var result = ClosestLocation(cache, position, maxDistance, predicate, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the navmesh from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited. Keeping this value within reasonable limits
        /// may dramatically increase performance.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the navmesh</param>
        /// <returns>Wether a location was found within the given radius</returns>
        /// <remarks>
        /// This function cannot be used in a burst compiled job context. Use the overload that accepts a <see cref="FunctionPointer{T}"/> instead.</remarks>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, ClosestNavMeshLocationPredicate predicate, out NavMeshGraphLocation location)
        {
            var visitor = new NearestVisitorFunc(this, position, predicate);
            cache.Nearest(ref octree, position, maxDistance, ref visitor, new DistanceProvider(this));
            location = visitor.closestLocation;
            return visitor.hasResult;
        }
        
        /// <summary>
        /// Returns the closest location on the navmesh from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited. Keeping this value within reasonable limits
        /// may dramatically increase performance.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the navmesh</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float3 position, float maxDistance, FunctionPointer<ClosestNavMeshLocationPredicate> predicate, out NavMeshGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            var result = ClosestLocation(cache, position, maxDistance, predicate, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the navmesh from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited. Keeping this value within reasonable limits
        /// may dramatically increase performance.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the navmesh</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, FunctionPointer<ClosestNavMeshLocationPredicate> predicate, out NavMeshGraphLocation location)
        {
            var visitor = new NearestVisitorFuncPointer(this, position, predicate);
            cache.Nearest(ref octree, position, maxDistance, ref visitor, new DistanceProvider(this));
            location = visitor.closestLocation;
            return visitor.hasResult;
        }
        
        struct DistanceProvider : IOctreeDistanceProvider<int>
        {
            private NavMeshGraph graph;

            public DistanceProvider(NavMeshGraph graph)
            {
                this.graph = graph;
            }
            
            public float DistanceSquared(float3 point, int edgeIndex, AABB bounds)
            {
                var tri = graph.GetTriangle(edgeIndex);
                var closestPoint = tri.ClosestPoint(point);
                return distancesq(closestPoint, point);
            }
        }

        struct NearestVisitor : IOctreeNearestVisitor<int>
        {
            private NavMeshGraph graph;
            
            public bool useFlagBitMask;
            public int flagBitMask;
            public float3 origin;
            
            public bool hasResult;
            public NavMeshGraphLocation closestLocation;
            
            public NearestVisitor(NavMeshGraph graph, float3 origin, bool useFlagBitMask, int flagBitMask)
            {
                this.graph = graph;
                this.useFlagBitMask = useFlagBitMask;
                this.flagBitMask = flagBitMask;
                this.origin = origin;
                this.hasResult = false;
                this.closestLocation = default;
            }

            public bool OnVist(int triangleIndex)
            {
                var cf = graph.enterCostAndFlags[triangleIndex];
                if (useFlagBitMask && (cf.flags & flagBitMask) == 0)
                    return true; // keep iterating

                // stop at first valid visit
                var tri = graph.GetTriangle(triangleIndex);
                var closestPoint = tri.ClosestPoint(origin);
                this.closestLocation = new NavMeshGraphLocation(triangleIndex, closestPoint, closestPoint, tri.Normal, closestPoint, cf.flags);
                this.hasResult = true;
                return false;
            }
        }
        
        struct NearestVisitorFunc : IOctreeNearestVisitor<int>
        {
            private NavMeshGraph graph;

            public float3 origin;
            public ClosestNavMeshLocationPredicate predicate;
            public bool hasResult;
            public NavMeshGraphLocation closestLocation;
            
            public NearestVisitorFunc(NavMeshGraph graph, float3 origin, ClosestNavMeshLocationPredicate predicate)
            {
                this.graph = graph;
                this.predicate = predicate;
                this.origin = origin;
                this.hasResult = false;
                this.closestLocation = default;
            }

            public bool OnVist(int triangleIndex)
            {
                var cf = graph.enterCostAndFlags[triangleIndex];
                var tri = graph.GetTriangle(triangleIndex);
                var closestPoint = tri.ClosestPoint(origin);
                var location = new NavMeshGraphLocation(triangleIndex, closestPoint, closestPoint, tri.Normal, closestPoint, cf.flags);
                if (!predicate(origin, location))
                    return true; // keep iterating

                // stop at first valid visit
                this.closestLocation = location;
                this.hasResult = true;
                return false;
            }
        }
        
        struct NearestVisitorFuncPointer : IOctreeNearestVisitor<int>
        {
            private NavMeshGraph graph;

            public float3 origin;
            public FunctionPointer<ClosestNavMeshLocationPredicate> predicate;
            public bool hasResult;
            public NavMeshGraphLocation closestLocation;
            
            public NearestVisitorFuncPointer(NavMeshGraph graph, float3 origin, FunctionPointer<ClosestNavMeshLocationPredicate> predicate)
            {
                this.graph = graph;
                this.predicate = predicate;
                this.origin = origin;
                this.hasResult = false;
                this.closestLocation = default;
            }

            public bool OnVist(int triangleIndex)
            {
                var cf = graph.enterCostAndFlags[triangleIndex];
                var tri = graph.GetTriangle(triangleIndex);
                var closestPoint = tri.ClosestPoint(origin);
                var location = new NavMeshGraphLocation(triangleIndex, closestPoint, closestPoint, tri.Normal, closestPoint, cf.flags);
                if (!predicate.Invoke(origin, location))
                    return true; // keep iterating

                // stop at first valid visit
                this.closestLocation = location;
                this.hasResult = true;
                return false;
            }
        }
        

        /// <summary>
        /// Appends the indices of all triangles whose AABB's overlap with a given AABB to the supplied nativelist
        /// </summary>
        /// <param name="aabb">The AABB</param>
        /// <param name="triangleIndices">The list to append the triangle indices to, note that this list is not cleared beforehand</param>
        /// <remarks>
        /// <para>
        /// The indices that are returned are the starting indices of each triangle.
        /// E.g. an index of zero corresponds to index 0, 1 and 2 in the mesh raw indices array.
        /// The full triangle location can be obtained with <see cref="LocationFromTriangleIndex"/> or <see cref="GetTriangle"/>
        /// </para>
        /// <para>
        /// It's possible for duplicates to occur.
        /// To guarantuee no duplicates, use the <see cref="GetOverlappingTriangles(AABB,Unity.Collections.NativeHashSet{int})"/> overload.
        /// </para>
        /// </remarks>
        public void GetOverlappingTriangles(AABB aabb, NativeList<int> triangleIndices)
        {
            var collector = new AABBCollector(triangleIndices);
            octree.Range(aabb, ref collector);
        }

        struct AABBCollector : IOctreeRangeVisitor<int>
        {
            private NativeList<int> triangleIndexes;
            public AABBCollector(NativeList<int> indexes)
            {
                this.triangleIndexes = indexes;
            }
            
            public bool OnVisit(int obj, AABB objBounds, AABB queryRange)
            {
                if (objBounds.Overlaps(queryRange))
                    triangleIndexes.Add(obj);

                return true;
            }
        }
        
        /// <summary>
        /// Appends the indices of all triangles whose AABB's overlap with a given AABB to the supplied NativeHashSet, so that
        /// no duplicates can occur
        /// </summary>
        /// <param name="aabb">The AABB</param>
        /// <param name="triangleIndices">The set to append the triangle indices to, note that this set is not cleared beforehand</param>
        /// <remarks>
        /// <para>
        /// The indices that are returned are the starting indices of each triangle.
        /// E.g. an index of zero corresponds to index 0, 1 and 2 in the mesh raw indices array.
        /// The full triangle location can be obtained with <see cref="LocationFromTriangleIndex"/> or <see cref="GetTriangle"/>
        /// </para>
        /// </remarks>
        public void GetOverlappingTriangles(AABB aabb, NativeHashSet<int> triangleIndices)
        {
            var collector = new AABBSetCollector(triangleIndices);
            octree.Range(aabb, ref collector);
        }
        
        struct AABBSetCollector : IOctreeRangeVisitor<int>
        {
            private NativeHashSet<int> triangleIndexes;
            
            public AABBSetCollector(NativeHashSet<int> indexes)
            {
                this.triangleIndexes = indexes;
            }

            public bool OnVisit(int obj, AABB objBounds, AABB queryRange)
            {
                if (objBounds.Overlaps(queryRange))
                    triangleIndexes.Add(obj);

                return true;
            }
        }

        #if UNITY_EDITOR

        /// <summary>
        /// Draws a gizmo of the structure of the internal octree. This can be used to determine an optimal max depth for the octree.
        /// </summary>
        /// <remarks>This method is not included in a build, only use for debugging</remarks>
        public void DrawOctreeGizmo()
        {
            octree.DrawGizmos();
        }

        /// <summary>
        /// Draws all triangles of the mesh.
        /// </summary>
        /// <remarks>This method is not included in a build, only use for debugging</remarks>
        public void DrawTrianglesGizmo()
        {
            for (int i = 0; i < TriangleCount; i++)
            {
                var tri = GetTriangle(i);
                if (all(tri.Normal == float3.zero))
                {
                    Gizmos.color = Color.red;
                    
                    if (tri.IsDegenerate())
                        Gizmos.color = Color.magenta;
                }

                if (all(tri.Normal == float3.zero))
                {
                    Gizmos.DrawLine(tri.a, tri.b);
                    Gizmos.DrawLine(tri.b, tri.c);
                    Gizmos.DrawLine(tri.c, tri.a);
                }
            }
        }
        #endif

    }
}