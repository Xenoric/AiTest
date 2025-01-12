using System;

namespace AnyPath.Native
{
    /// <summary>
    /// Implement this interface to create your own custom heuristic provider for a given node type.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes to provide a heuristic for</typeparam>
    public interface IHeuristicProvider<TNode> 
        where TNode : unmanaged, IEquatable<TNode>
    {
        /// <summary>
        /// Gets called by A* before the algorithm begins, keep track of the goal internally.
        /// </summary>
        void SetGoal(TNode goal);
        
        /// <summary>
        /// Return a cost estimate from this node to the goal that was set by SetGoal
        /// </summary>
        float Heuristic(TNode x);
    }
}