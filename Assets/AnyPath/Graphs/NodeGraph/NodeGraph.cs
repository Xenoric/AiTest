using AnyPath.Graphs.Line;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Graphs.Node
{
    /// <summary>
    /// A simple graph structure that connects vertices/nodes together in 3D space. Nodes can have additional cost and flags associated
    /// with them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is a good option if your usecase only requires agents to go from point to point, and they always know themselves
    /// at which node (id) they are. If your usecase is more complicated, such as being able to travel arbitrary distances along the edges
    /// between points, have a look at <see cref="LineGraph"/>
    /// </para>
    /// <para>
    /// This type of graph can also used in hierarichal pathfinding. If you have a very large world, you could have
    /// a NodeGraph where the Id's point to more detailed graphs. At first, you would run a query on the NodeGraph to see
    /// if two locations are connected, and if so, you run more detailed queries on the graphs that match these Id's.
    /// </para>
    /// </remarks>
    public struct NodeGraph : IGraph<NodeGraphNode>
    {
        private NativeParallelMultiHashMap<int, int> edges;
        private NativeHashMap<int, NodeGraphNode> idToNode;
        
        /// <summary>
        /// Constructs an empty node graph
        /// </summary>
        /// <param name="allocator"></param>
        public NodeGraph(Allocator allocator)
        {
            this.edges = new NativeParallelMultiHashMap<int, int>(16, allocator);
            this.idToNode = new NativeHashMap<int, NodeGraphNode>(16, allocator);
        }

        /// <summary>
        /// Clears the graph
        /// </summary>
        public void Clear()
        {
            this.edges.Clear();
            this.idToNode.Clear();
        }
        
        [ExcludeFromDocs]
        public void Collect(NodeGraphNode node, ref NativeList<Edge<NodeGraphNode>> edgeBuffer)
        {
            if (!edges.TryGetFirstValue(node.id, out int nextNodeId, out var it))
                return;
            
            do
            {
                var nextNode = idToNode[nextNodeId];
                if (math.isinf(nextNode.enterCost))
                    continue;
                
                edgeBuffer.Add(
                    new Edge<NodeGraphNode>(
                        nextNode, 
                        math.distance(node.position, nextNode.position) + nextNode.enterCost));

            } while (edges.TryGetNextValue(out nextNodeId, ref it));
        }

        /// <summary>
        /// Adds or sets the position of a vertex/node
        /// </summary>
        /// <param name="id">The id of the node</param>
        /// <param name="position">The position of the node</param>
        public void SetNode(int id, float3 position)
        {
            idToNode[id] = new NodeGraphNode(id, position);
        }

        /// <summary>
        /// Adds or sets the position of a vertex/node, as well as additional cost and flags
        /// </summary>
        /// <param name="id">The id of the node</param>
        /// <param name="position">The position of the node</param>
        /// <param name="enterCost">Additional cost associated with visiting this node.</param>
        /// <param name="flags">Custom flags associated with the node</param>
        public void SetNode(int id, float3 position, float enterCost, int flags)
        {
            idToNode[id] = new NodeGraphNode(id, position, flags, enterCost);
        }

        /// <summary>
        /// Returns the node for a given Id
        /// </summary>
        public NodeGraphNode GetNode(int id) => idToNode[id];

        /// <summary>
        /// Returns the node for a given Id
        /// </summary>
        public bool TryGetNode(int id, out NodeGraphNode node) => idToNode.TryGetValue(id, out node);

        /// <summary>
        /// Make the edge between two nodes traversable in both directions
        /// </summary>
        public void ConnectNodes(int id1, int id2)
        {
            edges.Add(id1, id2);
            edges.Add(id2, id1);
        }
        
        /// <summary>
        /// Make the edge between from and to traversable only in a forward direction
        /// </summary>
        public void ConnectNodesDirected(int fromId, int toId)
        {
            edges.Add(fromId, toId);
        }

        [ExcludeFromDocs]
        public void Dispose()
        {
            edges.Dispose();
            idToNode.Dispose();
        }

        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return JobHandle.CombineDependencies(edges.Dispose(inputDeps), idToNode.Dispose(inputDeps));
        }
    }
}