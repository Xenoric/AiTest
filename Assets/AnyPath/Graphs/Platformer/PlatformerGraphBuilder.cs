using System;
using System.Collections.Generic;
using AnyPath.Graphs.PlatformerGraph.SceneGraph;
using Unity.Mathematics;
using UnityEngine;

namespace AnyPath.Graphs.PlatformerGraph
{
    /// <summary>
    /// Utility to construct a platformer graph by connecting vertices by Id instead of index, which is far more convenient when building a custom editor.
    /// As an example use case, this class is used to construct the Platformer Graph by the <see cref="PlatformerSceneGraph"/>.
    /// </summary>
    /// <remarks>Also have a look at <see cref="PlatformerGraphDrawer"/> which is simpler to use</remarks>
    public class PlatformerGraphBuilder
    {
        /// <summary>
        /// Prototype of an edge.
        /// </summary>
        [Serializable]
        public struct ProtoEdge
        {
            /// <summary>
            /// Id of node A
            /// </summary>
            public readonly int a;
            
            /// <summary>
            /// Id of node B
            /// </summary>
            public readonly int b;
            
            /// <summary>
            /// Optional Id given to the edge in the graph
            /// </summary>
            public readonly int id;
            
            /// <summary>
            /// Is the edge directed? That is, does it only go from A to B.
            /// </summary>
            public readonly bool directed;
            
            /// <summary>
            /// Flags associated with this edge
            /// </summary>
            public readonly int flags;
            
            /// <summary>
            /// Extra cost associated with this edge
            /// </summary>
            public readonly float enterCost;

            public ProtoEdge(int a, int b, float enterCost, int flags, bool directed = false, int id = 0)
            {
                this.a = a;
                this.b = b;
                this.enterCost = enterCost;
                this.flags = flags;
                this.directed = directed;
                this.id = id;
            }
        }

        private Dictionary<int, Vector2> vertices = new Dictionary<int, Vector2>();
        private List<ProtoEdge> edges = new List<ProtoEdge>();

        /// <summary>
        /// Clear the builder.
        /// </summary>
        public void Clear()
        {
            vertices.Clear();
            edges.Clear();
        }
        
        /// <summary>
        /// Assigns a position to a vertex Id. If the vertex Id doesn't exist yet, it will be created. Otherwise it will be overwritten.
        /// </summary>
        public void SetVertex(int id, float2 pos)
        {
            vertices[id] = pos;
        }

        /// <summary>
        /// Check if a vertex has been added
        /// </summary>
        public bool ContainsVertex(int id) => vertices.ContainsKey(id);

        /// <summary>
        /// Returns the position of the vertex with a given Id
        /// </summary>
        public float2 GetVertex(int id) => vertices[id];

        /// <summary>
        /// Links two vertices together creating a traversable edge in both directions
        /// </summary>
        /// <returns>The index of the edge in the internal list of edges</returns>
        public int LinkUndirected(int a, int b) => LinkUndirected(a, b, 0, 0);

        /// <summary>
        /// Links two vertices together creating a traversable edge in both directions
        /// </summary>
        /// <param name="a">Id of vertex A</param>
        /// <param name="b">Id of vertex B</param>
        /// <param name="enterCost">Additional cost associated with entering this edge in a path</param>
        /// <param name="flags">Flags that can be used to filter the edge being traversable</param>
        /// <param name="id">Optional user defined Id that is given to the edge. This Id can be used for obtaining the edge from the <see cref="PlatformerGraph"/> that is constructed</param>
        /// <returns>The index of the edge in the internal list of edges</returns>
        public int LinkUndirected(int a, int b, float enterCost, int flags, int id = 0)
        {
            var edge = new ProtoEdge(a, b, enterCost, flags, directed: false, id: id);
            edges.Add(edge);
            return edges.Count - 1;
        }
        
        /// <summary>
        /// Links two vertices together creating a traversable edge only from a to b
        /// </summary>
        public void LinkDirected(int a, int b) => LinkDirected(a, b, 0, 0);
        
        /// <summary>
        /// Links two vertices together creating a traversable edge only from a to b
        /// </summary>
        /// <param name="enterCost">Additional cost associated with entering this edge in a path</param>
        /// <param name="a">Id of vertex A</param>
        /// <param name="b">Id of vertex B</param>
        /// <param name="flags">Flags that can be used to filter the edge being traversable</param>
        /// <param name="id">Optional user defined Id that is given to the edge. This Id can be used for obtaining the edge from the <see cref="PlatformerGraph"/> that is constructed</param>
        public void LinkDirected(int a, int b, float enterCost, int flags, int id = 0)
        {
            var edge = new ProtoEdge(a, b, enterCost, flags, directed: true, id: id);
            edges.Add(edge);
        }

        /// <summary>
        /// Convert this representation to a representation suitable to construct the platformer graph.
        /// </summary>
        public void GetData(out List<float2> verts, out List<PlatformerGraph.Edge> undirectedIndices, out List<PlatformerGraph.Edge> directedIndices)
        {
            HashSet<int2> duplicateSet = new HashSet<int2>();
            Dictionary<int, int> vertIdToIndex = new Dictionary<int, int>();
            verts = new List<float2>();
            undirectedIndices = new List<PlatformerGraph.Edge>();
            directedIndices = new List<PlatformerGraph.Edge>();

            foreach (var kv in vertices)
            {
                vertIdToIndex[kv.Key] = verts.Count;
                verts.Add(kv.Value);
            }

            foreach (var protoEdge in edges)
            {
                int2 orderedIds = new int2(math.min(protoEdge.a, protoEdge.b), math.max(protoEdge.a, protoEdge.b));
                if (!duplicateSet.Add(orderedIds))
                {
                    Debug.LogError($"Duplicate edge found {protoEdge.a} - {protoEdge.b}. Only add a single edge that connects vertices.");
                }

                if (!vertIdToIndex.TryGetValue(protoEdge.a, out int vertexIndexA))
                {
                    Debug.LogError($"Vertex with id {protoEdge.a} was not found!");
                    continue;
                }
                
                if (!vertIdToIndex.TryGetValue(protoEdge.b, out int vertexIndexB))
                {
                    Debug.LogError($"Vertex with id {protoEdge.b} was not found!");
                    continue;
                }
                
                var edge = new PlatformerGraph.Edge(vertexIndexA, vertexIndexB, protoEdge.enterCost, protoEdge.flags, protoEdge.id);
                if (protoEdge.directed)
                    directedIndices.Add(edge);
                else
                    undirectedIndices.Add(edge);
            }
        }
    }
}