using System;
using Unity.Collections;

namespace AnyPath.Native
{
    /// <summary>
    /// Defines an operation that converts a raw pathfinding result into another format.
    /// </summary>
    /// <typeparam name="TNode">The raw node/location type that the graph's path is built with</typeparam>
    /// <typeparam name="TSeg">The type of segments this processor creates</typeparam>
    public interface IPathProcessor<TNode, TSeg>
        where TNode : unmanaged, IEquatable<TNode>
        where TSeg : unmanaged
    {
        /// <summary>
        /// Process the raw path to another format.
        /// </summary>
        /// <param name="queryStart">The original start node that was used for the query. This can be used to derive an exact location in the processed path.</param>
        /// <param name="queryGoal">The original goal node that was used for the query. This can be used to derive an exact location in the processed path.</param>
        /// <param name="pathNodes">
        /// All of the nodes that make up the path, including the goal as was yielded by the graph.
        /// Depending on <see cref="InsertQueryStart"/>, the query's starting node will be the first element.
        /// If the query's start was equal to the goal, this list will be empty, or if <see cref="InsertQueryStart"/> is true, will only contain the query's starting node.
        /// </param>
        /// <param name="appendTo"></param>
        void ProcessPath(TNode queryStart, TNode queryGoal, NativeList<TNode> pathNodes, NativeList<TSeg> appendTo);
        
        /// <summary>
        /// Should the original query's starting node be inserted as the first node? In some cases, this can be helpful for processing
        /// and prevent an insert operation requiring every element to be shifted.
        /// </summary>
        bool InsertQueryStart { get; }
    }
}