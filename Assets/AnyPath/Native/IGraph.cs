using System;
using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Native
{
    [ExcludeFromDocs]
    public interface IGraph : INativeDisposable
    {
    }
    
    /// <summary>
    /// Interface that needs to be implemented to use a structure as a graph for pathfinding.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes the graph contains. The raw path output will consist of these nodes.</typeparam>
    public interface IGraph<TNode> : IGraph
        where TNode : unmanaged, IEquatable<TNode>
    {
        /// <summary>
        /// Implement adding all the directed edges that go from the input node.
        /// </summary>
        /// <param name="node">Input node</param>
        /// <param name="edgeBuffer">Add edges to other nodes to this buffer.
        /// The buffer is automatically cleared each time before this method is called.
        /// Warning: do *not* modify the edgeBuffer itself. Only add to it. The ref keyword is only used for performance reasons.</param>
        void Collect(TNode node, ref NativeList<Edge<TNode>> edgeBuffer);
    }
}