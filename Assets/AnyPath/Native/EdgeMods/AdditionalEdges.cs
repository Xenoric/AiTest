using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Native.EdgeMods
{
    /// <summary>
    /// Supports adding additional edges to any graph type. (Portals, for example).
    /// This can be used to create bridges between any two locations that aren't present in the graph itself.
    /// You can supply these to individual pathfinding requests, or use this when small changes occur in the graph so you don't
    /// have to rebuild the entire graph.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    /// <remarks>
    /// <para>
    /// Note that this Edge Modifier uses a Native Container which means
    /// it needs to be disposed, just as graphs. Use the <see cref=" AnyPath.Managed.Disposal.ManagedDisposer.DisposeSafe{TGraph}"/> extension
    /// method for that.
    /// </para>
    /// <para>
    /// If your use case is creating portal "shortcuts", then you should be aware of the fact that this will probably undermine the admissability of
    /// the heuristic provider. Which can cause the algorithm to not always find the truly shortest path. This may or may not be a problem for your
    /// use case.
    /// </para>
    /// </remarks>
    public struct AdditionalEdges<TNode> : IEdgeMod<TNode>, INativeDisposable 
        where TNode : unmanaged, IEquatable<TNode>
    {
        private NativeParallelMultiHashMap<TNode, Edge<TNode>> map;

        public AdditionalEdges(Allocator allocator, int capacity = 0)
        {
            map = new NativeParallelMultiHashMap<TNode, Edge<TNode>>(capacity, allocator);
        }

        /// <summary>
        /// Adds an additional edge.
        /// </summary>
        /// <param name="from">The starting location, can be anywhere in the graph.</param>
        /// <param name="to">The end location, can be anywhere in the graph.</param>
        /// <param name="cost">
        /// <para>
        /// The cost of traversing this edge.
        /// </para>
        /// <para>
        /// For best results, use the cost that your <see cref="IHeuristicProvider{TNode}"/> gives.
        /// If you use a cost that is lower than the actual heuristic, you are at risk of not finding the truly shortest
        /// path anymore. This may or may not be a problem for your use case.
        /// </para>
        /// </param>
        /// <remarks>
        /// <para>
        /// It is allowed to add multiple edges that originate or go to the same node.
        /// </para>
        /// <para>
        /// Note that you shouldn't modify this struct when it's in use by a pathfinder.
        /// </para>
        /// </remarks>
        public void AddEdge(TNode from, TNode to, float cost) =>  map.Add(from, new Edge<TNode>(to, cost));
        
        /// <summary>
        /// Clears all edges that were added.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Note that you shouldn't modify this struct when it's in use by a pathfinder.
        /// </para>
        /// </remarks>
        public void Clear() => map.Clear();
        
        [ExcludeFromDocs]
        public bool ModifyCost(in TNode from, in TNode to, ref float cost) => true;

        [ExcludeFromDocs]
        public void ModifyEdgeBuffer(in TNode from, ref NativeList<Edge<TNode>> edgeBuffer)
        {
            if (!map.TryGetFirstValue(from, out var item, out var it))
                return;

            do
            {
                edgeBuffer.Add(item);
            } while (map.TryGetNextValue(out item, ref it));
        }

        [ExcludeFromDocs]
        public void Dispose() => map.Dispose();
        
        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps) => map.Dispose(inputDeps);
    }
}