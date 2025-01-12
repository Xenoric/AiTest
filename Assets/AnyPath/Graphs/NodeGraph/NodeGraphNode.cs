using System;
using AnyPath.Native;
using Unity.Mathematics;

namespace AnyPath.Graphs.Node
{
    /// <summary>
    /// Node type of the <see cref="NodeGraph"/>
    /// </summary>
    public readonly struct NodeGraphNode : IEquatable<NodeGraphNode>, INodeFlags
    {
        /// <summary>
        /// Unique identifier of the node
        /// </summary>
        public readonly int id;
        
        /// <summary>
        /// Position of the node
        /// </summary>
        public readonly float3 position;
        
        /// <summary>
        /// User defined flags of this node.
        /// </summary>
        public int Flags { get; }
        
        /// <summary>
        /// Extra cost associated with traversing this node
        /// </summary>
        public readonly float enterCost;

        public NodeGraphNode(int id, float3 position, int flags, float enterCost)
        {
            this.id = id;
            this.position = position;
            this.Flags = flags;
            this.enterCost = enterCost;
        }

        public NodeGraphNode(int id, float3 position) : this(id, position, 0, 0)
        {
        }

        public bool Equals(NodeGraphNode other) => id == other.id;
        public override int GetHashCode() => id.GetHashCode();
    }
}