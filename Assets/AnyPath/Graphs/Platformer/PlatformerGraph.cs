using System;
using System.Collections;
using System.Collections.Generic;
using AnyPath.Graphs.Extra;
using AnyPath.Graphs.Line;
using AnyPath.Native;
using AnyPath.Native.Util;
using AnyPath.NativeTrees;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

namespace AnyPath.Graphs.PlatformerGraph
{
    public delegate bool ClosestPlatformerGraphLocationPredicate(float2 origin, PlatformerGraphLocation location);
    
    /// <summary>
    /// A graph specifically designed for 2D platformer types of games, but can also be used as an advanced waypointing system.
    /// What makes this graph unique is that the edges themselves play the main part, not the nodes they connect. This allows for fluid positions
    /// anywhere on an edge.
    /// <para>
    /// Added functionality since v1.1 is that the graph can now be pre-allocated and then populated from within jobs.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Since v1.1, any edge that has an enterCost of infinity is considered unwalkable.
    /// </para>
    /// <para>
    /// The <see cref="LineGraph"/> provides similar functionality but in 3D.
    /// </para>
    /// </remarks>
    public struct PlatformerGraph : IGraph<PlatformerGraphLocation>
    {
        [Serializable, ExcludeFromDocs]
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
            /// Extra cost associated with traversing this edge. This is added to the length of the edge.
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
        private NativeList<Edge> edges;
        private NativeHashMap<int, int> edgeIdToIndex;
        private NativeQuadtree<int> quadTree;
        private NativeList<float2> vertices;
        private NativeReference<int> undirectedEdgeCount;
        private readonly bool directedEdgesRaycastable;

        /// <summary>
        /// Read only access to the edges that make up the graph
        /// </summary>
        public NativeArray<Edge>.ReadOnly Edges => edges.AsParallelReader();

        /// <summary>
        /// Read only access to the vertices that make up the graph
        /// </summary>
        public NativeArray<float2>.ReadOnly Vertices => vertices.AsParallelReader();
        
        
        /// <summary>
        /// Indicates if a raycast or any overlap query will return directed edges
        /// </summary>
        public bool DirectedEdgesRaycastable => directedEdgesRaycastable;

        /// <summary>
        /// Access to the internal quadtree of the graph for advanced location queries.
        /// </summary>
        /// <remarks>Warning: do not modify the quadtree as this will corrupt the state of the graph.</remarks>
        public NativeQuadtree<int> Quadtree => quadTree;
        
        /// <summary>
        /// Returns an enumerator that enumerates all edges/locations in the graph. This can be used to construct ALT heuristics.
        /// </summary>
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <summary>
        /// Structure that enumerates all edges/locations in the graph. This can be used to construct ALT heuristics.
        /// </summary>
        public struct Enumerator : IEnumerator<PlatformerGraphLocation>
        {
            private int index;
            [ReadOnly] private PlatformerGraph graph;

            public Enumerator(PlatformerGraph graph)
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
                    Current = new PlatformerGraphLocation(index, edge.id, edge.flags, graph.GetLine(index));
                    return true;
                }

                return false;
            }

            public void Reset()
            {
                index = -1;
            }

            public PlatformerGraphLocation Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }
        }
        
        /// <summary>
        /// Construct a Platformer Graph from a set of vertices and edge definitions
        /// </summary>
        /// <param name="vertices">Vertices to use</param>
        /// <param name="undirectedEdges">Indices connecting vertices together in an undirected fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i and vertex i + 1 in both directions</param>
        /// <param name="directedEdges">Indices connecting vertices together in a directed fashion. Every pair of indices (i, i+1)
        /// describes an edge that connectets vertex i to vertex i + 1</param>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="directedEdgesRaycastable">If true, directed edges can be returned by a raycast query. This comes
        /// with the caveat that when you start a path from a directed edge and the goal is on the same edge but behind the starting location, a path
        /// will be found that goes in the opposite direction of that edge.</param>
        /// <param name="edgesPerQuadrant">Max edges per quadrant for the internal quadtree</param>
        /// <param name="maxQuadTreeDepth">Max depth of the internal quadtree for raycast accellerating. If your graph is very large it may be benificial to
        /// increase this value. Finding a good balance is key to having optimal raycast and closest location performance.</param>
        public PlatformerGraph(IReadOnlyList<float2> vertices, 
            IReadOnlyList<Edge> undirectedEdges, 
            IReadOnlyList<Edge> directedEdges, 
            Allocator allocator, bool directedEdgesRaycastable = false, int edgesPerQuadrant = 16, int maxQuadTreeDepth = 5)
            : this(allocator, directedEdgesRaycastable, edgesPerQuadrant, maxQuadTreeDepth, vertices.Count, undirectedEdges.Count + directedEdges.Count)
        {
            Populate(vertices, undirectedEdges, directedEdges);
        }
        
        /// <summary>
        /// Preallocates an empty graph, to be populated later using any of the <see cref="Populate"/> methods
        /// </summary>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="edgesPerQuadrant">Max edges per quadrant for the internal quadtree</param>
        /// <param name="maxQuadTreeDepth">Max depth of the internal quadtree for raycast accellerating. If your graph is very large it may be benificial to
        /// increase this value. Finding a good balance is key to having optimal raycast and closest location performance.</param>
        /// <param name="directedEdgesRaycastable">If true, directed edges can be returned by a raycast query. This comes
        /// with the caveat that when you start a path from a directed edge and the goal is on the same edge but behind the starting location, a path
        /// will be found that goes in the opposite direction of that edge.</param>
        /// <param name="initialVertexCapacity">Hint to how many vertices are going to be added later, which can boost performance</param>
        /// <param name="intialEdgeCapacity">Hint to how many edges are going to be added later, which can boost performance</param>
        public PlatformerGraph(Allocator allocator, bool directedEdgesRaycastable = false, int edgesPerQuadrant = 16, int maxQuadTreeDepth = 5,
            int initialVertexCapacity = 0, int intialEdgeCapacity = 0)
        {
            this.directedEdgesRaycastable = directedEdgesRaycastable;
            this.vertices = new NativeList<float2>(initialVertexCapacity, allocator);
            this.edges = new NativeList<Edge>(intialEdgeCapacity, allocator);
            this.adjecency = new NativeParallelMultiHashMap<int, int>(32, allocator);
            this.quadTree = new NativeQuadtree<int>(new AABB2D(float2.zero, new float2(1,1)), edgesPerQuadrant, maxQuadTreeDepth, allocator);
            this.undirectedEdgeCount = new NativeReference<int>(allocator);
            this.edgeIdToIndex = new NativeHashMap<int, int>(intialEdgeCapacity, allocator);
        }
        
        [ExcludeFromDocs]
        public void Dispose()
        {
            edges.Dispose();
            vertices.Dispose();
            adjecency.Dispose();
            quadTree.Dispose();
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
            tmp[3] = quadTree.Dispose(inputDeps);
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
        /// describes an edge that connectets vertex i to vertex i + 1</param>
        /// <remarks>Do not use this method when the graph is in use for pathfinding, as it writes to the internals of the graph</remarks>
        public void Populate(
            IReadOnlyList<float2> vertices,
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
            NativeArray<float2> vertices,
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
        /// <exception cref="InvalidOperationException">Throws when the value of <see cref="DirectedEdgesRaycastable"/> is different than that of the source</exception>
        public void Populate(PlatformerGraph source)
        {
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (this.directedEdgesRaycastable != source.directedEdgesRaycastable)
                throw new InvalidOperationException("Both source and target need to have the same value for directEdgesRaycastable");
            #endif
            
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

            this.quadTree.CopyFrom(source.quadTree);
        }

        /// <summary>
        /// Overwrites the enter cost and flags for an edge at index. The index can be obtained
        /// via <see cref="GetOverlappingEdgeIndices"/>,
        /// <see cref="Raycast(UnityEngine.Ray2D,int,out PlatformerGraphLocation)"/> or
        /// <see cref="ClosestLocation(Unity.Mathematics.float2,float,int,out PlatformerGraphLocation)"/>
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
        /// Makes an edge unwalkable, assigning it a cost of infinity. The edge index can be obtained
        /// via <see cref="GetOverlappingEdgeIndices"/>,
        /// <see cref="Raycast(UnityEngine.Ray2D,int,out PlatformerGraphLocation)"/> or
        /// <see cref="ClosestLocation(Unity.Mathematics.float2,float,int,out PlatformerGraphLocation)"/>
        /// </summary>
        /// <param name="edgeIndex">index of the edge to overwrite</param>
        /// <remarks>This method writes to the graph and as such it cannot be used while pathfinding queries are active on it</remarks>
        public void SetUnwalkable(int edgeIndex) => SetEnterCostAndFlags(edgeIndex, float.PositiveInfinity, edges[edgeIndex].flags);

        private void Calculate(int undirectedEdgeCount)
        {
            this.undirectedEdgeCount.Value = undirectedEdgeCount;
            
            float2 boundsMin = new float2(float.PositiveInfinity, float.PositiveInfinity);
            float2 boundsMax = new float2(float.NegativeInfinity, float.NegativeInfinity);
            
            int edgeCount = edges.Length;
            var aabbs = new NativeArray<AABB2D>(directedEdgesRaycastable ? edgeCount : undirectedEdgeCount, Allocator.Temp);
            var edgeIndexes = new NativeArray<int>(directedEdgesRaycastable ? edgeCount : undirectedEdgeCount, Allocator.Temp);
            
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

                if (directedEdgesRaycastable)
                    InsertIntoTree(i, edge, ref boundsMin, ref boundsMax, ref edgeIndexes, ref aabbs);
                
                adjecency.Add(edge.vertexIndexA, i);
            }
            
            // build our tree
            this.quadTree.Clear(new AABB2D(boundsMin, boundsMax));
            for (int i = 0; i < edgeIndexes.Length; i++)
                quadTree.Insert(edgeIndexes[i], aabbs[i]);
          
            
            aabbs.Dispose();
            edgeIndexes.Dispose();
        }
        
        /// <summary>
        /// Inserts an edge into the quad tree for raycasting, well, almost anyways
        /// inserts into two temp arrays that are later used to insert into the tree
        /// </summary>
        void InsertIntoTree(int edgeIndex, Edge edge, ref float2 boundsMin, ref float2 boundsMax, ref NativeArray<int> edgeIndexes, ref NativeArray<AABB2D> aabbs)
        {
            float2 a = vertices[edge.vertexIndexA];
            float2 b = vertices[edge.vertexIndexB];
            var aabb = new AABB2D(min(a, b), max(a, b));
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
        
        public void Collect(PlatformerGraphLocation location, ref NativeList<Edge<PlatformerGraphLocation>> edgeBuffer)
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
                        ? new Line2D(vertices[otherEdge.vertexIndexA], vertices[otherEdge.vertexIndexB])
                        : new Line2D(vertices[otherEdge.vertexIndexB], vertices[otherEdge.vertexIndexA]);

                    var newNode = new PlatformerGraphLocation(otherEdgeIndex, otherEdge.id, otherEdge.flags, line);
                    edgeBuffer.Add(new Edge<PlatformerGraphLocation>(newNode, location.line.GetLength() + otherEdge.enterCost));

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
                        ? new Line2D(vertices[otherEdge.vertexIndexA], vertices[otherEdge.vertexIndexB])
                        : new Line2D(vertices[otherEdge.vertexIndexB], vertices[otherEdge.vertexIndexA]);

                    var newNode = new PlatformerGraphLocation(otherEdgeIndex, otherEdge.id, otherEdge.flags, line);
                    edgeBuffer.Add(new Edge<PlatformerGraphLocation>(newNode, location.line.GetLength() + otherEdge.enterCost));
                    
                } while (adjecency.TryGetNextValue(out otherEdgeIndex, ref it));
            }
        }
        
        private Line2D GetLine(int index)
        {
            var edge = edges[index];
            return new Line2D(vertices[edge.vertexIndexA], vertices[edge.vertexIndexB]);
        }
        
        #region Location Queries
        
        /// <summary>
        /// Returns the index of an edge from it's Id. Note that Id's are optional so if you didn't supply Id's upon creation, this
        /// method will not work. Also note that if more edges share the same Id, this method will also not work.
        /// </summary>
        /// <param name="edgeId">The Id of the edge</param>
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
        /// endpoints using <see cref="PlatformerGraphLocation.PositionT"/>.
        /// </para>
        /// </summary>
        /// <param name="edgeId">The id of the edge</param>
        /// <remarks>Throws an error if the edgeId isn't present</remarks>
        public PlatformerGraphLocation GetLocationFromEdgeId(int edgeId)
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
        public PlatformerGraphLocation GetLocationFromEdgeIndex(int edgeIndex)
        {
            var edge = edges[edgeIndex];
            return new PlatformerGraphLocation(edgeIndex, edge.id, edge.flags, GetLine(edgeIndex));
        }
    
        
        /// <summary>
        /// Performs a raycast against the graph.
        /// This can then be used as the starting node for a pathfinding request.
        /// The location contains information about the edge as well as the exact location the path should start or end.
        /// </summary>
        /// <param name="ray">The ray to cast</param>
        /// <param name="flagBitMask">A bitwise AND is performed on the edge candidates and if any bit is true, the edge is considered.</param>
        /// <param name="location">The location that can be used as part of a path finding request</param>
        /// <returns>True if there was a hit</returns>
        public bool Raycast(Ray2D ray, int flagBitMask, out PlatformerGraphLocation location)
        {
            return Raycast(ray, true, flagBitMask, out location);
        }
        
        /// <summary>
        /// Performs a raycast against the graph.
        /// This can then be used as the starting node for a pathfinding request.
        /// The location contains information about the edge as well as the exact location the path should start or end.
        /// </summary>
        /// <param name="ray">The ray to cast</param>
        /// <param name="location">The location that can be used as part of a path finding request</param>
        /// <returns>True if there was a hit</returns>
        public bool Raycast(Ray2D ray, out PlatformerGraphLocation location)
        {
            return Raycast(ray, false, 0, out location);
        }

        private bool Raycast(Ray2D ray, bool useFlagBitMask, int flagBitMask, out PlatformerGraphLocation location)
        {
            var intersecter = new RayIntersecter(this, useFlagBitMask, flagBitMask);

            if (quadTree.Raycast(ray, out var hit, intersecter))
            {
                var edge = edges[hit.obj];
                var line = GetLine(hit.obj);
                location = new PlatformerGraphLocation(hit.obj, edge.id, edge.flags, line, line.GetClosestPositionT(hit.point));
                return true;
            }

            location = default;
            return false;
        }
        
        /// <summary>
        /// Appends all edge indices whose AABB's overlap with a rectangle to a hashset, so that no duplicates can ocur.
        /// </summary>
        /// <param name="aabb">Rectangle to test against</param>
        /// <param name="indices">The results are appended to this set. The set is not cleared beforehand</param>
        public void GetOverlappingEdgeIndices(AABB2D aabb, NativeHashSet<int> indices)
        {
            var col = new OverlapCollector(indices);
            quadTree.Range(aabb, ref col);
        }

        
        struct OverlapCollector : IQuadtreeRangeVisitor<int>
        {
            public NativeHashSet<int> set;
            public OverlapCollector(NativeHashSet<int> set) => this.set = set;
            public bool OnVisit(int obj, AABB2D objBounds, AABB2D queryRange)
            {
                if (objBounds.Overlaps(queryRange))
                    set.Add(obj);

                return true;
            }
        }

        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxRadius">The max search radius</param>
        /// <param name="flagBitMask">A bitwise AND is performed on the edge candidates and if any bit is true, the edge is considered.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float2 position, float maxRadius, int flagBitMask, out PlatformerGraphLocation location)
        {
            var cache = new NativeQuadtree<int>.NearestNeighbourQuery(Allocator.Temp);
            bool result = ClosestLocation(cache, position, maxRadius, true, flagBitMask, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxRadius">The max search radius</param>
        /// <param name="flagBitMask">A bitwise AND is performed on the edge candidates and if any bit is true, the edge is considered.</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeQuadtree<int>.NearestNeighbourQuery cache, float2 position, float maxRadius, int flagBitMask, out PlatformerGraphLocation location)
        {
            return ClosestLocation(cache, position, maxRadius, true, flagBitMask, out location);
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxRadius">The max search radius</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(float2 position, float maxRadius, out PlatformerGraphLocation location)
        {
            var cache = new NativeQuadtree<int>.NearestNeighbourQuery(Allocator.Temp);
            bool result = ClosestLocation(cache, position, maxRadius, false, 0, out location);
            cache.Dispose();
            return result;
        }
        
        /// <summary>
        /// Returns the closest location on the graph from a point.
        /// </summary>
        /// <param name="cache">Re-usable cache for the algorithm. If you need to perform a lot of closest location queries in a row, it is faster to allocate this
        /// cache once and re-use it for every query.</param>
        /// <param name="position">The center position to search from</param>
        /// <param name="maxRadius">The max search radius</param>
        /// <param name="location">Returns the closest location on the graph</param>
        /// <returns>Wether a location was found within the given radius</returns>
        public bool ClosestLocation(NativeQuadtree<int>.NearestNeighbourQuery cache, float2 position, float maxRadius, out PlatformerGraphLocation location)
        {
            return ClosestLocation(cache, position, maxRadius, false, 0, out location);
        }
        
        private bool ClosestLocation(NativeQuadtree<int>.NearestNeighbourQuery cache, float2 position, float maxDistance, bool useFlagBitMask, int flagBitMask, out PlatformerGraphLocation location)
        {
            var visitor = new NearestVisitor(this, position, useFlagBitMask, flagBitMask);
            cache.Nearest(ref quadTree, position, maxDistance, ref visitor, new DistanceProvider(this));
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
        public bool ClosestLocation(float2 position, float maxDistance, ClosestPlatformerGraphLocationPredicate predicate, out PlatformerGraphLocation location)
        {
            var cache = new NativeQuadtree<int>.NearestNeighbourQuery(Allocator.Temp);
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
        public bool ClosestLocation(NativeQuadtree<int>.NearestNeighbourQuery cache, float2 position, float maxDistance, ClosestPlatformerGraphLocationPredicate predicate, out PlatformerGraphLocation location)
        {
            var visitor = new NearestVisitorFunc(this, position, predicate);
            cache.Nearest(ref quadTree, position, maxDistance, ref visitor, new DistanceProvider(this));
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
        public bool ClosestLocation(float2 position, float maxDistance, FunctionPointer<ClosestPlatformerGraphLocationPredicate> predicate, out PlatformerGraphLocation location)
        {
            var cache = new NativeQuadtree<int>.NearestNeighbourQuery(Allocator.Temp);
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
        public bool ClosestLocation(NativeQuadtree<int>.NearestNeighbourQuery cache, float2 position, float maxDistance, FunctionPointer<ClosestPlatformerGraphLocationPredicate> predicate, out PlatformerGraphLocation location)
        {
            var visitor = new NearestVisitorFuncPointer(this, position, predicate);
            cache.Nearest(ref quadTree, position, maxDistance, ref visitor, new DistanceProvider(this));
            location = visitor.closestLocation;
            return visitor.hasResult;
        }
        
        struct DistanceProvider : IQuadtreeDistanceProvider<int>
        {
            private PlatformerGraph graph;

            public DistanceProvider(PlatformerGraph graph)
            {
                this.graph = graph;
            }
            
            public float DistanceSquared(float2 point, int edgeIndex, AABB2D bounds)
            {
                var line = graph.GetLine(edgeIndex);
                var closestPoint = line.GetClosestPoint(point);
                return distancesq(closestPoint, point);
            }
        }
        
        struct NearestVisitor : IQuadtreeNearestVisitor<int>
        {
            private PlatformerGraph graph;
            
            public bool useFlagBitMask;
            public int flagBitMask;
            public float2 origin;
            
            public bool hasResult;
            public PlatformerGraphLocation closestLocation;
            
            public NearestVisitor(PlatformerGraph graph, float2 origin, bool useFlagBitMask, int flagBitMask)
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
                this.closestLocation = new PlatformerGraphLocation(edgeIndex, edge.id, edge.flags, line, line.GetClosestPositionT(origin));
                this.hasResult = true;
                return false;
            }
        }
        
        struct NearestVisitorFunc : IQuadtreeNearestVisitor<int>
        {
            private PlatformerGraph graph;

            public float2 origin;
            public ClosestPlatformerGraphLocationPredicate predicate;
            public bool hasResult;
            public PlatformerGraphLocation closestLocation;
            
            public NearestVisitorFunc(PlatformerGraph graph, float2 origin, ClosestPlatformerGraphLocationPredicate predicate)
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
                var location = new PlatformerGraphLocation(edgeIndex, edge.id, edge.flags, line, line.GetClosestPositionT(origin));
                if (!predicate(origin, location))
                    return true; // keep iterating

                // stop at first valid visit
                this.closestLocation = location;
                this.hasResult = true;
                return false;
            }
        }
        
        struct NearestVisitorFuncPointer : IQuadtreeNearestVisitor<int>
        {
            private PlatformerGraph graph;

            public float2 origin;
            public FunctionPointer<ClosestPlatformerGraphLocationPredicate> predicate;
            public bool hasResult;
            public PlatformerGraphLocation closestLocation;
            
            public NearestVisitorFuncPointer(PlatformerGraph graph, float2 origin, FunctionPointer<ClosestPlatformerGraphLocationPredicate> predicate)
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
                var location = new PlatformerGraphLocation(edgeIndex, edge.id, edge.flags, line, line.GetClosestPositionT(origin));
                if (!predicate.Invoke(origin, location))
                    return true; // keep iterating

                // stop at first valid visit
                this.closestLocation = location;
                this.hasResult = true;
                return false;
            }
        }

        struct RayIntersecter : IQuadtreeRayIntersecter<int>
        {
            private PlatformerGraph graph;
            public bool useFlagBitMask;
            public int flagBitMask;

            public RayIntersecter(PlatformerGraph graph,  bool useFlagBitMask, int flagBitMask)
            {
                this.graph = graph;
                this.useFlagBitMask = useFlagBitMask;
                this.flagBitMask = flagBitMask;
            }
            
            public bool IntersectRay(in PrecomputedRay2D ray, int edgeIndex, AABB2D objBounds, out float distance)
            {
                var edge = graph.edges[edgeIndex];
                if (useFlagBitMask && (edge.flags & flagBitMask) == 0)
                {
                    distance = default;
                    return false;
                }
                
                var line = graph.GetLine(edgeIndex);
                if (line.IntersectsRay((Ray2D)ray, out float2 point))
                {
                    distance = math.distance(point, ray.origin);
                    return true;
                }

                distance = default;
                return false;
            }
        }
        
        #endregion

#if UNITY_EDITOR
        [ExcludeFromDocs]
        public void DrawGizmos()
        {
            DrawEdges();
            //quadTree.DrawGizmo();
        }

        [ExcludeFromDocs]
        public void DrawEdges()
        {
            Gizmos.color = Color.black;
            for (int i = 0; i < EdgeCount; i++)
            {
                var line = GetLine(i);
                Gizmos.DrawLine(line.a.ToVec3(), line.b.ToVec3());
            }
        }
#endif

    }
}