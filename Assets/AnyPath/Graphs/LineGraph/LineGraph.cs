using System;
using System.Collections;
using System.Collections.Generic;
using AnyPath.Graphs.Extra;
using AnyPath.Native;
using AnyPath.NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace AnyPath.Graphs.Line
{
    /// <summary>
    /// Describes a function that validates if a location on a line/edge is valid as a return value for the Closest queries.
    /// This can be used for instance to check if there are objects obstructing the line of sight between the origin and the location.
    /// </summary>
    public delegate bool ClosestLineLocationPredicate(float3 origin, LineGraphLocation location);
    
    /// <summary>
    /// A graph that describes 3D edges/lines between points.
    /// Pathfinding can be done from any location on these edges to any location on another edge. Making it more intuitive for
    /// agents that can move smoothly along these edges.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This graph is very similar to <see cref="PlatformerGraph"/>, but in 3D.
    /// Note that the words edge and line are used interchangeably throughout the documentation.
    /// </para>
    /// <para>
    /// Note that this graph does not support Raycast queries
    /// to obtain a location. However, you can obtain the closest location on the graph from any point in space by using <see cref="ClosestLocation(Unity.Mathematics.float3,float,int,out AnyPath.Graphs.Line.LineGraphLocation)"/>
    /// </para>
    /// </remarks>
    public struct LineGraph : IGraph<LineGraphLocation>
    {
        [Serializable]
        public struct Edge
        {
            /// <summary>
            /// An optional Id that can be used to map back to something else, like a MonoBehaviour script
            /// </summary>
            public int id;
            
            /// <summary>
            /// Index of vertex A
            /// </summary>
            public int vertexIndexA;
            
            /// <summary>
            /// Index of vertex B
            /// </summary>
            public int vertexIndexB;
            
            /// <summary>
            /// Optional extra cost associated with traversing this edge. This is added to the length of the edge.
            /// A value of infinity makes this edge "unwalkable", essentially removing it from the graph.
            /// </summary>
            public float enterCost;
            
            /// <summary>
            /// Optional user defined flags for this edge.
            /// </summary>
            public int flags;

            public Edge(int vertexIndexA, int vertexIndexB, float enterCost, int flags, int id = 0)
            {
                this.vertexIndexA = vertexIndexA;
                this.vertexIndexB = vertexIndexB;
                this.enterCost = enterCost;
                this.flags = flags;
                this.id = id;
            }

            public Edge(int vertexIndexA, int vertexIndexB) : this(vertexIndexA, vertexIndexB, 0, 0)
            {
            }
        }

        private NativeParallelMultiHashMap<int, int> adjecency;
        private NativeHashMap<int, int> edgeIdToIndex;
        private NativeList<Edge> edges;
        private NativeOctree<int> octree;
        private NativeList<float3> vertices;
        private NativeReference<int> undirectedEdgeCount;
        private readonly bool directedEdgesQueryable;

        /// <summary>
        /// Read only access to the edges that make up the graph
        /// </summary>
        public NativeArray<Edge>.ReadOnly Edges => edges.AsParallelReader();

        /// <summary>
        /// Read only access to the vertices that make up the graph
        /// </summary>
        public NativeArray<float3>.ReadOnly Vertices => vertices.AsParallelReader();
        
        
        /// <summary>
        /// Indicates if a raycast or any overlap query will return directed edges
        /// </summary>
        public bool DirectedEdgesQueryable => directedEdgesQueryable;

        /// <summary>
        /// Access to the internal octree of the graph for advanced location queries.
        /// </summary>
        /// <remarks>Warning: do not modify the octree as this will corrupt the state of the graph.</remarks>
        public NativeOctree<int> Octree => octree;
        
        /// <summary>
        /// Returns an enumerator that enumerates all edges/locations in the graph. This can be used to construct ALT heuristics.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Structure that enumerates all edges/locations in the graph. This can be used to construct ALT heuristics.
        /// </summary>
        public struct Enumerator : IEnumerator<LineGraphLocation>
        {
            private int index;
            [ReadOnly] private LineGraph graph;

            public Enumerator(LineGraph graph)
            {
                this.graph = graph;
                index = -1;
                Current = default;
            }
            
            public bool MoveNext()
            {
                if (++index < graph.edges.Length)
                {
                    var edge = graph.edges[index];
                    Current = new LineGraphLocation(index, edge.id, edge.flags, graph.GetLine(index));
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            public LineGraphLocation Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
        
        /// <summary>
        /// Construct a LineGraph from a set of vertices and edge definitions
        /// </summary>
        /// <param name="vertices">Vertices to use</param>
        /// <param name="undirectedEdges">Indices connecting vertices together in an undirected fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i and vertex i + 1 in both directions</param>
        /// <param name="directedEdges">Indices connecting vertices together in a directed fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i to vertex i + 1</param>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="directedEdgesRaycastable">If true, directed edges can be returned by a closest edge query. This comes
        /// with the caveat that when you start a path from a directed edge and the goal is on the same edge but behind the starting location, a path
        /// will be found that goes in the opposite direction of that edge.</param>
        /// <param name="edgesPerOctant">Max edges per octant for the internal octree</param>
        /// <param name="maxOctreeDepth">Max depth of the internal octree for raycast/closest location accellerating. If your graph is very large it may be benificial to
        /// increase this value. Finding a good balance is key to having optimal raycast and closest location performance.</param>
        public LineGraph(IReadOnlyList<float3> vertices, 
            IReadOnlyList<Edge> undirectedEdges, 
            IReadOnlyList<Edge> directedEdges, 
            Allocator allocator, bool directedEdgesRaycastable = false, int edgesPerOctant = 16, int maxOctreeDepth = 5)
            : this(allocator, directedEdgesRaycastable, edgesPerOctant, maxOctreeDepth, vertices.Count, undirectedEdges.Count + directedEdges.Count)
        {
            Populate(vertices, undirectedEdges, directedEdges);
        }
        
        /// <summary>
        /// Preallocates an empty graph, to be populated later using any of the <see cref="Populate"/> methods
        /// </summary>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="edgesPerOctant">Max edges per octant for the internal octree</param>
        /// <param name="maxOctreeDepth">Max depth of the internal octree for raycast/closest location accellerating. If your graph is very large it may be benificial to
        /// increase this value. Finding a good balance is key to having optimal raycast and closest location performance.</param>
        /// <param name="directedEdgesQueryable">If true, directed edges can be returned by a closest/overlap query. This comes
        /// with the caveat that when you start a path from a directed edge and the goal is on the same edge but behind the starting location, a path
        /// will be found that goes in the opposite direction of that edge.</param>
        /// <param name="initialVertexCapacity">Hint to how many vertices are going to be added later, which can boost performance</param>
        /// <param name="intialEdgeCapacity">Hint to how many edges are going to be added later, which can boost performance</param>
        public LineGraph(Allocator allocator, bool directedEdgesQueryable = false, int edgesPerOctant = 16, int maxOctreeDepth = 5,
            int initialVertexCapacity = 0, int intialEdgeCapacity = 0)
        {
            this.directedEdgesQueryable = directedEdgesQueryable;
            this.vertices = new NativeList<float3>(initialVertexCapacity, allocator);
            this.edgeIdToIndex = new NativeHashMap<int, int>(32, allocator);
            this.edges = new NativeList<Edge>(intialEdgeCapacity, allocator);
            this.adjecency = new NativeParallelMultiHashMap<int, int>(32, allocator);
            this.octree = new NativeOctree<int>(new AABB(float3.zero, new float3(1,1, 1)), edgesPerOctant, maxOctreeDepth, allocator);
            this.undirectedEdgeCount = new NativeReference<int>(allocator);
        }

        /// <summary>
        /// Populate the graph with vertices and edges
        /// </summary>
        /// <param name="vertices">Vertices to use</param>
        /// <param name="undirectedEdges">Indices connecting vertices together in an undirected fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i and vertex i + 1 in both directions</param>
        /// <param name="directedEdges">Indices connecting vertices together in a directed fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i to vertex i + 1</param>
        /// <remarks>Do not use this method when the graph is in use for pathfinding, as it writes to the internals of the graph</remarks>
        public void Populate(
            IReadOnlyList<float3> vertices,
            IReadOnlyList<Edge> undirectedEdges,
            IReadOnlyList<Edge> directedEdges = null)
        {
            this.edges.Clear();
            this.vertices.Clear();
            this.edgeIdToIndex.Clear();

            for (int i = 0; i < vertices.Count; i++)
            {
                this.vertices.Add(vertices[i]);
            }
            
            for (int i = 0; i < undirectedEdges.Count; i++)
            {
                var e = undirectedEdges[i];
                this.edges.Add(e);
                this.edgeIdToIndex.TryAdd(e.id, i);
            }

            if (directedEdges != null)
            {
                for (int i = 0; i < directedEdges.Count; i++)
                {
                    var e = directedEdges[i];
                    this.edges.Add(e);
                    this.edgeIdToIndex.TryAdd(e.id, i);
                }
            }
            
            Calculate(undirectedEdges.Count);
        }
        
        [ExcludeFromDocs]
        public void Dispose()
        {
            edges.Dispose();
            vertices.Dispose();
            adjecency.Dispose();
            octree.Dispose();
            undirectedEdgeCount.Dispose();
            edgeIdToIndex.Dispose();
        }

        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            NativeArray<JobHandle> tmp = new NativeArray<JobHandle>(6, Allocator.Temp);
            tmp[0] = vertices.Dispose(inputDeps);
            tmp[1] = adjecency.Dispose(inputDeps);
            tmp[2] = edges.Dispose(inputDeps);
            tmp[3] = octree.Dispose(inputDeps);
            tmp[4] = undirectedEdgeCount.Dispose(inputDeps);
            tmp[5] = edgeIdToIndex.Dispose(inputDeps);
            var handle = JobHandle.CombineDependencies(tmp);
            tmp.Dispose();
            return handle;
        }

        /// <summary>
        /// Populate the graph with vertices and edges
        /// </summary>
        /// <param name="vertices">Vertices to use</param>
        /// <param name="undirectedEdges">Indices connecting vertices together in an undirected fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i and vertex i + 1 in both directions</param>
        /// <param name="directedEdges">Indices connecting vertices together in a directed fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i to vertex i + 1. Leave default to not use any directed edges</param>
        /// <remarks>
        /// <para>
        /// This method is burst compatible, meaning it can be used inside a job to populate the graph with high performance, if you
        /// need frequent updates.
        /// </para>
        /// <para>
        /// Do not use this method when the graph is in use for pathfinding, as it writes to the internals of the graph.
        /// If you need frequent updates, a common technique is to use a double buffer and swap two graphs as one is updated.
        /// </para>
        /// </remarks>
        public void Populate(
            NativeArray<float3> vertices,
            NativeArray<Edge> undirectedEdges, 
            NativeArray<Edge> directedEdges = default)
        {
            this.vertices.CopyFrom(vertices);
            
            this.edgeIdToIndex.Clear();
            this.edges.Clear();
            
            for (int i = 0; i < undirectedEdges.Length; i++)
            {
                var e = undirectedEdges[i];
                this.edges.Add(e);
                this.edgeIdToIndex.TryAdd(e.id, i);
            }

            if (directedEdges.IsCreated)
            {
                for (int i = 0; i < directedEdges.Length; i++)
                {
                    var e = directedEdges[i];
                    this.edges.Add(e);
                    this.edgeIdToIndex.TryAdd(e.id, i);
                }
            }

            Calculate(undirectedEdges.Length);
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
        /// <para>Source and destination settings should match</para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Throws when the value of <see cref="DirectedEdgesQueryable"/> is different than that of the source</exception>
        public void Populate(LineGraph source)
        {
            if (this.directedEdgesQueryable != source.directedEdgesQueryable)
                throw new InvalidOperationException("Both source and target need to have the same value for directEdgesRaycastable");
            
            this.vertices.Resize(source.Vertices.Length, NativeArrayOptions.UninitializedMemory);
            source.Vertices.CopyTo(this.vertices.AsArray());

            this.edges.Resize(source.Edges.Length, NativeArrayOptions.UninitializedMemory);
            source.Edges.CopyTo(this.edges.AsArray());
            
            this.adjecency.Clear();
            var kvs = source.adjecency.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < kvs.Length; i++)
                this.adjecency.Add(kvs.Keys[i], kvs.Values[i]);
            kvs.Dispose();
            
            this.edgeIdToIndex.Clear();
            var kvs2 = source.edgeIdToIndex.GetKeyValueArrays(Allocator.Temp);
            for (int i = 0; i < kvs2.Length; i++)
                this.edgeIdToIndex.Add(kvs2.Keys[i], kvs2.Values[i]);
            kvs2.Dispose();
            
            this.octree.CopyFrom(source.octree);
        }

        /// <summary>
        /// Overwrites the enter cost and flags for an edge at index. The index can be obtained
        /// via <see cref="GetOverlappingEdgeIndices"/>, <see cref="GetEdgeIndex"/> or <see cref="ClosestLocation(Unity.Mathematics.float3,float,int,out AnyPath.Graphs.Line.LineGraphLocation)"/>
        /// </summary>
        /// <param name="edgeIndex">index of the edge to overwrite</param>
        /// <param name="enterCost">new enter cost for the edge. A value of infinity makes the edge unwalkable, essentialy removing
        /// it from the graph. Note that raycasting can still return this edge.</param>
        /// <param name="flags">New flags for this edge</param>
        /// <remarks>This method writes to the graph and as such it cannot be used while pathfinding queries are active on it</remarks>
        public void SetEnterCostAndFlags(int edgeIndex, float enterCost, int flags)
        {
            var e = edges[edgeIndex];
            e.enterCost = enterCost;
            e.flags = flags;
            edges[edgeIndex] = e;
        }
        
        /// <summary>
        /// Makes an edge unwalkable, assigning it a cost of infinity.
        /// </summary>
        /// <param name="edgeIndex">index of the edge to overwrite</param>
        /// <remarks>This method writes to the graph and as such it cannot be used while pathfinding queries are active on it</remarks>
        public void SetUnwalkable(int edgeIndex) => SetEnterCostAndFlags(edgeIndex, float.PositiveInfinity, edges[edgeIndex].flags);

        private void Calculate(int undirectedEdgeCount)
        {
            this.undirectedEdgeCount.Value = undirectedEdgeCount;
            
            float3 boundsMin = new float3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            float3 boundsMax = new float3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            
            int edgeCount = edges.Length;
            var aabbs = new NativeArray<AABB>(directedEdgesQueryable ? edgeCount : undirectedEdgeCount, Allocator.Temp);
            var edgeIndexes = new NativeArray<int>(directedEdgesQueryable ? edgeCount : undirectedEdgeCount, Allocator.Temp);
            
            adjecency.Clear();
            
            // undirected
            for (int i = 0; i < undirectedEdgeCount; i++)
            {
                var edge = edges[i];
                InsertIntoTree(i, edge, ref boundsMin, ref boundsMax, ref edgeIndexes, ref aabbs);
                adjecency.Add(edge.vertexIndexA, i);
                adjecency.Add(edge.vertexIndexB, i);
            }
            
            // directed
            for (int i = undirectedEdgeCount; i < edges.Length; i++)
            {
                var edge = edges[i];

                if (directedEdgesQueryable)
                    InsertIntoTree(i, edge, ref boundsMin, ref boundsMax, ref edgeIndexes, ref aabbs);
                
                adjecency.Add(edge.vertexIndexA, i);
            }
            
            // build our tree
            this.octree.Clear(new AABB(boundsMin, boundsMax));
            for (int i = 0; i < edgeIndexes.Length; i++)
                octree.Insert(edgeIndexes[i], aabbs[i]);
          
            
            aabbs.Dispose();
            edgeIndexes.Dispose();
        }
        
        /// <summary>
        /// Inserts an edge into the quad tree for raycasting, well, almost anyways
        /// inserts into two temp arrays that are later used to insert into the tree
        /// </summary>
        void InsertIntoTree(int edgeIndex, Edge edge, ref float3 boundsMin, ref float3 boundsMax, ref NativeArray<int> edgeIndexes, ref NativeArray<AABB> aabbs)
        {
            float3 a = vertices[edge.vertexIndexA];
            float3 b = vertices[edge.vertexIndexB];
            var aabb = new AABB(min(a, b), max(a, b));
            boundsMin = min(boundsMin, aabb.min);
            boundsMax = max(boundsMax, aabb.max);
            edgeIndexes[edgeIndex] = edgeIndex;
            aabbs[edgeIndex] = aabb;
        }

        /// <summary>
        /// Amount of edges (directed+undirected) contained in the graph. 
        /// </summary>
        public int EdgeCount => edges.Length;

        /// <summary>
        /// Amount of undirected edges. Undirected edges appear first in the raw <see cref="Edges"/> array.
        /// So any undirected edges start from this index.
        /// </summary>
        public int UndirectedEdgeCount => undirectedEdgeCount.Value;
        
        public void Collect(LineGraphLocation location, ref NativeList<Edge<LineGraphLocation>> edgeBuffer)
        {
            // catch invalid index in case the start/goal was queried with RaycastNode and no node was found
            if (location.edgeIndex < 0 || location.edgeIndex >= EdgeCount)
                return;
            
            var edge = edges[location.edgeIndex];

            if (adjecency.TryGetFirstValue(edge.vertexIndexA, out int otherEdgeIndex, out var it))
            {
                do
                {
                    // skip undirected edges self connection
                    if (otherEdgeIndex == location.edgeIndex)
                        continue;
                    
                    var otherEdge = edges[otherEdgeIndex];
                    if (isinf(otherEdge.enterCost))
                        continue;
                    
                    var line = (otherEdge.vertexIndexA == edge.vertexIndexA)
                        ? new Line3D(vertices[otherEdge.vertexIndexA], vertices[otherEdge.vertexIndexB])
                        : new Line3D(vertices[otherEdge.vertexIndexB], vertices[otherEdge.vertexIndexA]);
                    
                    var newNode = new LineGraphLocation(otherEdgeIndex, otherEdge.id, otherEdge.flags, line);
                    edgeBuffer.Add(new Edge<LineGraphLocation>(newNode, location.line.GetLength() + otherEdge.enterCost));

                } while (adjecency.TryGetNextValue(out otherEdgeIndex, ref it));
            }

            if (adjecency.TryGetFirstValue(edge.vertexIndexB, out otherEdgeIndex, out it))
            {
                do
                {
                    if (otherEdgeIndex == location.edgeIndex)
                        continue;
                    
                    var otherEdge = edges[otherEdgeIndex];
                    if (isinf(otherEdge.enterCost))
                        continue;
                    
                    var line = (otherEdge.vertexIndexA == edge.vertexIndexB)
                        ? new Line3D(vertices[otherEdge.vertexIndexA], vertices[otherEdge.vertexIndexB])
                        : new Line3D(vertices[otherEdge.vertexIndexB], vertices[otherEdge.vertexIndexA]);

                    var newNode = new LineGraphLocation(otherEdgeIndex, otherEdge.id, otherEdge.flags, line);
                    edgeBuffer.Add(new Edge<LineGraphLocation>(newNode, location.line.GetLength() + otherEdge.enterCost));
                    
                } while (adjecency.TryGetNextValue(out otherEdgeIndex, ref it));
            }
        }
        
        private Line3D GetLine(int edgeIndex)
        {
            var edge = edges[edgeIndex];
            return new Line3D(vertices[edge.vertexIndexA], vertices[edge.vertexIndexB]);
        }
        
        #region Location Queries
        
        /// <summary>
        /// Returns the index of an edge from it's Id. Note that Id's are optional so if you didn't supply Id's upon creation, this
        /// method will not work. Also note that if more edges share the same Id, this method will also not work.
        /// </summary>
        /// <param name="edgeId">The Id of the edge</param>
        /// <remarks>Throws an error of the Id is not present</remarks>
        public int GetEdgeIndex(int edgeId)
        {
            return edgeIdToIndex[edgeId];
        }

        /// <summary>
        /// Returns the index of an edge from it's Id. Note that Id's are optional so if you didn't supply Id's upon creation, this
        /// method will not work. Also note that if more edges share the same Id, this method will also not work.
        /// </summary>
        /// <param name="edgeId">The Id of the edge</param>
        /// <param name="edgeIndex">If successful, set to the index of the edge that carries the Id</param>
        public bool TryGetEdgeIndex(int edgeId, out int edgeIndex) => edgeIdToIndex.TryGetValue(edgeId, out edgeIndex);

        /// <summary>
        /// Does an edge with a given Id exist?
        /// </summary>
        public bool ContainsEdgeId(int edgeId) => edgeIdToIndex.ContainsKey(edgeId);
        
        /// <summary>
        /// <para>
        /// Returns a location for pathfinding queries from an edge's Id. Note that this Id must have been
        /// manually assigned to the edge's upon creation. <see cref="Edge.id"/>
        /// </para>
        /// <para>
        /// The exact position on the edge is set at the center by default. But this value can be modified to be anywhere between or on the
        /// endpoints using <see cref="LineGraphLocation.PositionT"/>.
        /// </para>
        /// </summary>
        /// <param name="edgeId">The id of the edge</param>
        /// <remarks>Throws an error if the edgeId isn't present</remarks>
        public LineGraphLocation GetLocationFromEdgeId(int edgeId)
        {
            int edgeIndex = GetEdgeIndex(edgeId);
            return GetLocationFromEdgeIndex(edgeIndex);
        }

        /// <summary>
        /// <para>
        /// Returns a location for pathfinding queries from an edge index.
        /// </para>
        /// <para>
        /// The exact position on the edge is set at the center by default. But this value can be modified to be anywhere between or on the
        /// endpoints using <see cref="LineGraphLocation.PositionT"/>.
        /// </para>
        /// </summary>
        public LineGraphLocation GetLocationFromEdgeIndex(int edgeIndex)
        {
            var edge = edges[edgeIndex];
            return new LineGraphLocation(edgeIndex, edge.id, edge.flags, GetLine(edgeIndex));
        }

        
        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="flagBitMask">A bitwise AND is performed on the edge candidates and if any bit is true, the edge is considered.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float3 position, float maxDistance, int flagBitMask, out LineGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            bool result = ClosestLocation(cache, position, maxDistance, true, flagBitMask, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="flagBitMask">A bitwise AND is performed on the edge candidates and if any bit is true, the edge is considered.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, int flagBitMask, out LineGraphLocation location)
        {
            return ClosestLocation(cache, position, maxDistance, true, flagBitMask, out location);
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float3 position, float maxDistance, out LineGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            bool result = ClosestLocation(cache, position, maxDistance, false, 0, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, out LineGraphLocation location)
        {
            return ClosestLocation(cache, position, maxDistance, false, 0, out location);
        }
        
        private bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, bool useFlagBitMask, int flagBitMask, out LineGraphLocation location)
        {
            var visitor = new NearestVisitor(this, position, useFlagBitMask, flagBitMask);
            cache.Nearest(ref octree, position, maxDistance, ref visitor, new DistanceProvider(this));
            location = visitor.closestLocation;
            return visitor.hasResult;
        }

        /// <summary>
        /// Returns the closest location on the graph from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        /// <remarks>This function cannot be used in a burst compiled job context. Use the overload that accepts a <see cref="FunctionPointer{T}"/> instead.</remarks>
        public bool ClosestLocation(float3 position, float maxDistance, ClosestLineLocationPredicate predicate, out LineGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            bool result = ClosestLocation(cache, position, maxDistance, predicate, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        /// <remarks>This function cannot be used in a burst compiled job context. Use the overload that accepts a <see cref="FunctionPointer{T}"/> instead.</remarks>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, ClosestLineLocationPredicate predicate, out LineGraphLocation location)
        {
            var visitor = new NearestVisitorFunc(this, position, predicate);
            cache.Nearest(ref octree, position, maxDistance, ref visitor, new DistanceProvider(this));
            location = visitor.closestLocation;
            return visitor.hasResult;
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float3 position, float maxDistance, FunctionPointer<ClosestLineLocationPredicate> predicate, out LineGraphLocation location)
        {
            var cache = new NativeOctree<int>.NearestNeighbourCache(Allocator.Temp);
            bool result = ClosestLocation(cache, position, maxDistance, predicate, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point, with a custom 'filter' predicate. This predicate
        /// can be used for instance to check if there is a clear line of sight between the position and the location that is returned.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxDistance">The max search radius. Locations beyond this distance will not be visited.</param>
        /// <param name="predicate">A custom function to determine wether a location is valid as a closest location. If false is returned, the
        /// next closest location after that is attempted, and so on.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeOctree<int>.NearestNeighbourCache cache, float3 position, float maxDistance, FunctionPointer<ClosestLineLocationPredicate> predicate, out LineGraphLocation location)
        {
            var visitor = new NearestVisitorFuncPointer(this, position, predicate);
            cache.Nearest(ref octree, position, maxDistance, ref visitor, new DistanceProvider(this));
            location = visitor.closestLocation;
            return visitor.hasResult;
        }

        
        struct DistanceProvider : IOctreeDistanceProvider<int>
        {
            private LineGraph graph;

            public DistanceProvider(LineGraph graph)
            {
                this.graph = graph;
            }
            
            public float DistanceSquared(float3 point, int edgeIndex, AABB bounds)
            {
                var line = graph.GetLine(edgeIndex);
                var closestPoint = line.GetClosestPoint(point);
                return distancesq(closestPoint, point);
            }
        }

        struct NearestVisitor : IOctreeNearestVisitor<int>
        {
            private LineGraph graph;
            
            public bool useFlagBitMask;
            public int flagBitMask;
            public float3 origin;
            
            public bool hasResult;
            public LineGraphLocation closestLocation;
            
            public NearestVisitor(LineGraph graph, float3 origin, bool useFlagBitMask, int flagBitMask)
            {
                this.graph = graph;
                this.useFlagBitMask = useFlagBitMask;
                this.flagBitMask = flagBitMask;
                this.origin = origin;
                this.hasResult = false;
                this.closestLocation = default;
            }

            public bool OnVist(int edgeIndex)
            {
                var edge = graph.edges[edgeIndex];
                if (useFlagBitMask && (edge.flags & flagBitMask) == 0)
                    return true; // keep iterating

                // stop at first valid visit
                var line = graph.GetLine(edgeIndex);
                this.closestLocation = new LineGraphLocation(edgeIndex, edge.id, edge.flags, line, line.GetClosestPositionT(origin));
                this.hasResult = true;
                return false;
            }
        }
        
        struct NearestVisitorFunc : IOctreeNearestVisitor<int>
        {
            private LineGraph graph;

            public float3 origin;
            public ClosestLineLocationPredicate predicate;
            public bool hasResult;
            public LineGraphLocation closestLocation;
            
            public NearestVisitorFunc(LineGraph graph, float3 origin, ClosestLineLocationPredicate predicate)
            {
                this.graph = graph;
                this.predicate = predicate;
                this.origin = origin;
                this.hasResult = false;
                this.closestLocation = default;
            }

            public bool OnVist(int edgeIndex)
            {
                var edge = graph.edges[edgeIndex];
                var line = graph.GetLine(edgeIndex);
                var location = new LineGraphLocation(edgeIndex, edge.id, edge.flags, line, line.GetClosestPositionT(origin));
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
            private LineGraph graph;

            public float3 origin;
            public FunctionPointer<ClosestLineLocationPredicate> predicate;
            public bool hasResult;
            public LineGraphLocation closestLocation;
            
            public NearestVisitorFuncPointer(LineGraph graph, float3 origin, FunctionPointer<ClosestLineLocationPredicate> predicate)
            {
                this.graph = graph;
                this.predicate = predicate;
                this.origin = origin;
                this.hasResult = false;
                this.closestLocation = default;
            }

            public bool OnVist(int edgeIndex)
            {
                var edge = graph.edges[edgeIndex];
                var line = graph.GetLine(edgeIndex);
                var location = new LineGraphLocation(edgeIndex, edge.id, edge.flags, line, line.GetClosestPositionT(origin));
                if (!predicate.Invoke(origin, location))
                    return true; // keep iterating

                // stop at first valid visit
                this.closestLocation = location;
                this.hasResult = true;
                return false;
            }
        }
        
        
        /// <summary>
        /// Appends all edge indices whose AABB's overlap with a rectangle to a hashset, so that no duplicates can ocur.
        /// </summary>
        /// <param name="aabb">Rectangle to test against</param>
        /// <param name="indices">The results are appended to this set. The set is not cleared beforehand</param>
        public void GetOverlappingEdgeIndices(AABB aabb, NativeHashSet<int> indices)
        {
            var col = new OverlapCollector(indices);
            octree.Range(aabb, ref col);
        }
        
        struct OverlapCollector : IOctreeRangeVisitor<int>
        {
            public NativeHashSet<int> set;
            public OverlapCollector(NativeHashSet<int> set) => this.set = set;
            public bool OnVisit(int obj, AABB objBounds, AABB queryRange)
            {
                if (objBounds.Overlaps(queryRange))
                    set.Add(obj);

                return true;
            }
        }
        
        #endregion


#if UNITY_EDITOR
        [ExcludeFromDocs]
        public void DrawGizmos()
        {
            DrawEdges();
        }

        [ExcludeFromDocs]
        public void DrawEdges()
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < EdgeCount; i++)
            {
                var line = GetLine(i);
                Gizmos.DrawLine(line.a, line.b);
            }
        }
#endif

    }
}