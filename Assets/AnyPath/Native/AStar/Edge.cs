using System;

namespace AnyPath.Native
{
    /// <summary>
    /// Directed edge used by the pathfinding algorithms. 
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public readonly struct Edge<TNode> where TNode : unmanaged, IEquatable<TNode>
    {
        public readonly TNode Next;
        public readonly float Cost;

        public Edge(TNode next, float cost)
        {
            this.Next = next;
            this.Cost = cost;
        }
    }
}