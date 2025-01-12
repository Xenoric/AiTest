using System;
using System.Collections.Generic;
using AnyPath.Native;
using UnityEngine.Internal;

namespace AnyPath.Managed.Results
{
    /// <summary>
    /// Managed result of a <see cref="AnyPath.Managed.Finders.DijkstraFinder{TGraph, TNode, TMod}"/>.
    /// This contains information about all nodes that are reachable from the starting location within a maximum cost budget.
    /// You can also obtain the shortest path from the start to every reachable node from this result.
    /// </summary>
    /// <typeparam name="TNode">Node type of the graph this ran on</typeparam>
    /// <remarks>
    /// <para>
    /// While you can obtain every path to a reachable destination, these paths are not stored directly for memory and performance reasons.
    /// When you call <see cref="GetPath(TNode,bool)"/>, the path is reconstructed from the information contained within this object. Still however,
    /// this class can be roughly the same size as your graph, as every location that has been reached needs to be stored. If you use this often,
    /// it may be befinicial to set <see cref="AnyPath.Managed.Finders.DijkstraFinder{TGraph, TNode, TMod}.ReuseResult"/> to true, to prevent unneccessary allocations.
    /// </para>
    /// <para>
    /// Reconstructing a path via a Dijkstra result is slower than finding a single path using a regular PathFinder, as the reconstruction is not burst compiled.
    /// If you don't need to evaluate all possibilities but rather only a few, consider using an OptionFinder instead.
    /// </para>
    /// </remarks>
    public class DijkstraResult<TNode>
        where TNode : unmanaged, IEquatable<TNode>
    {
        private static Queue<List<TNode>> listPool = new Queue<List<TNode>>();
        private Dictionary<TNode, AStar<TNode>.CameFrom> map = new Dictionary<TNode, AStar<TNode>.CameFrom>();

        /// <summary>
        /// Create or hydrate a dijkstra result
        /// </summary>
        /// <param name="original">When null, a new instance is created. When set, the instance is hydrated</param>
        /// <returns>The re-hydrated original or a new instance if original wasn't supplied.</returns>
        [ExcludeFromDocs]
        public static DijkstraResult<TNode> CreateOrHydrate(DijkstraResult<TNode> original, TNode start, float maxCost, AStar<TNode> result)
        {
            if (original == null) 
                original = new DijkstraResult<TNode>();
            
            original.Start = start;
            original.MaxCost = maxCost;
            original.map.Clear();

            foreach (var kv in result.cameFrom)
                original.map.Add(kv.Key, kv.Value);

            return original;
        }

        /// <summary>
        /// The starting node that was used for the dijkstra algorithm
        /// </summary>
        public TNode Start { get; private set; }

        /// <summary>
        /// The maximum cost budget that was used for the dijkstra algorithm
        /// </summary>
        public float MaxCost { get; private set; }

        /// <summary>
        /// All possible goal/destination nodes that were reachable from the start within the max cost budget
        /// </summary>
        public IEnumerable<TNode> Goals => map.Keys;

        /// <summary>
        /// Returns the cost of going from the start to the goal, if reachable
        /// </summary>
        /// <param name="goal">The destination to get the cost for</param>
        /// <param name="cost">The cost of travelling from the start to the specified goal node. Zero if the destination is not reachable</param>
        /// <returns>Wether the destination is reachable</returns>
        public bool TryGetCost(TNode goal, out float cost)
        {
            if (map.TryGetValue(goal, out var cf))
            {
                cost = cf.g;
                return true;
            }

            cost = 0;
            return false;
        }

        /// <summary>
        /// Returns wether a given destination was reachable from the starting node, within the max cost budget
        /// </summary>
        /// <param name="goal">The destination node</param>
        /// <returns>Wether the destination is reachable within the max cost budget</returns>
        public bool HasPath(TNode goal) => map.ContainsKey(goal);

        /// <summary>
        /// Returns the path from the starting node to a goal node
        /// </summary>
        /// <param name="goal"></param>
        /// <param name="includeStart"></param>
        /// <returns>The path from the start to the goal.</returns>
        /// <remarks>
        /// <para>
        /// If no path exists from start to goal, a path is returned with <see cref="Path{TSeg}.HasPath"/> set to false
        /// </para>
        /// <para>
        /// It is only safe to call this method from the main thread.
        /// </para>
        /// </remarks>
        public Path<TNode> GetPath(TNode goal, bool includeStart)
        {
            var path = new Path<TNode>();
            GetPath(path, goal, includeStart);
            return path;
        }

        /// <summary>
        /// Returns the path from the starting node to a goal node. Supply a result container to hydrate, so no
        /// new memory allocations have to be made.
        /// </summary>
        /// <param name="destResult">The result to hydrate.</param>
        /// <param name="goal"></param>
        /// <param name="includeStart"></param>
        /// <returns>The path from the start to the goal.</returns>
        /// <remarks>
        /// <para>
        /// If no path exists from start to goal, a path is returned with <see cref="Path{TSeg}.HasPath"/> set to false
        /// </para>
        /// <para>
        /// It is only safe to call this method from the main thread.
        /// </para>
        /// </remarks>
        public void GetPath(Path<TNode> destResult, TNode goal, bool includeStart)
        {
            if (!map.TryGetValue(goal, out var cf))
            {
                Path<TNode>.Hydrate(destResult, false, null, 0);
                return;
            }

            float pathCost = cf.g;
            List<TNode> pathBuffer = listPool.Count > 0 ? listPool.Dequeue() : new List<TNode>();
            TNode current = goal;

            while (!current.Equals(Start))
            {
                if (!map.TryGetValue(current, out cf))
                    break;

                pathBuffer.Add(cf.next);
                current = cf.prev;
            }
            
            if (includeStart)
                pathBuffer.Add(Start);

            // Path needs to be reversed in order to be forward
            int length = pathBuffer.Count;
            int end = pathBuffer.Count - 1;
            for (int i = 0; i < length / 2; i++)
            {
                var tmp = pathBuffer[i];
                pathBuffer[i] = pathBuffer[end - i];
                pathBuffer[end - i] = tmp;
            }

            Path<TNode>.Hydrate(destResult, true, pathBuffer, pathCost);
            pathBuffer.Clear();
            listPool.Enqueue(pathBuffer);
        }
    }
}