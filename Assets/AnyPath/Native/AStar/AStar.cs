using System;
using System.Runtime.CompilerServices;
using AnyPath.Native.Util;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Core A* and Dijkstra algorithm implementation. Create an instance of this struct and use
    /// <see cref="FindPath{TGraph,TH,TMod,TProc,TSeg}"/>, <see cref="EvalPath{TGraph,TH,TMod}"/> or any of the extension methods
    /// to do burst compatible pathfinding queries.
    /// </summary>
    /// <typeparam name="TNode">The type of nodes to operate on</typeparam>
    /// <remarks>This struct can be re-used for multiple request, but it can only handle one request at a time.</remarks>
    public struct AStar<TNode> where TNode : unmanaged, IEquatable<TNode>
    {
        /// <summary>
        /// Returns wether the native containers inside this structure were allocated
        /// </summary>
        public bool IsCreated => cameFrom.IsCreated;
        
        /// <summary>
        /// Contains information about which node lead into the next one, from the last run.
        /// </summary>
        [NoAlias] public NativeHashMap<TNode, CameFrom> cameFrom;
  
        /// <summary>
        /// Priority queue for A*
        /// </summary>
        [NoAlias] private NativeRefMinHeap<Open, OpenComp> minHeap;
        
        /// <summary>
        /// Graphs temporarily store their edges in here
        /// </summary>
        [NoAlias] private NativeList<Edge<TNode>> edgeBuffer;
        
        /// <summary>
        /// Temporary buffer for path reconstruction when used via FindPath
        /// </summary>
        [NoAlias] private NativeList<TNode> pathBuffer;
        
        /// <summary>
        /// The maximum number of nodes A* may expand into before "giving up". This can provide as an upper bound for targets
        /// that are unreachable, reducing computation time because the algorithm will have to search
        /// the entire graph before knowing for certain that a target is unreachable.
        /// </summary>
        /// <remarks>This value is not used when using <see cref="Dijkstra{TGraph,TMod}"/></remarks>
        public int maxExpand;
        
        /// <summary>
        /// Creates the native A* struct
        /// </summary>
        /// <param name="maxExpand">
        /// The maximum number of nodes A* may expand into before "giving up". This can provide as an upper bound for targets
        /// that are unreachable, reducing computation time because the algorithm will have to search
        /// the entire graph before knowing for certain that a target is unreachable.
        /// </param>
        /// <param name="allocator">Unity allocator to use</param>
        public AStar(int maxExpand, Allocator allocator)
        {
            cameFrom = new NativeHashMap<TNode, CameFrom>(128, allocator);
            edgeBuffer = new NativeList<Edge<TNode>>(128, allocator);
            pathBuffer = new NativeList<TNode>(128, allocator);
            minHeap = new NativeRefMinHeap<Open, OpenComp>(default, allocator);
            this.maxExpand = maxExpand;
        }
        
        /// <summary>
        /// Creates a native A* struct with a default maxExpand of 65536
        /// </summary>
        /// <param name="allocator">Unity allocator to use</param>
        public AStar(Allocator allocator) : this(65536, allocator)
        {
        }
        
        /// <summary>
        /// Dispose this memory and all of it's native containers
        /// </summary>
        public void Dispose()
        {
            cameFrom.Dispose();
            minHeap.Dispose();
            edgeBuffer.Dispose();
            pathBuffer.Dispose();
        }
        
        /// <summary>
        /// Dispose this memory and all of it's native containers
        /// </summary>
        /// <param name="inputDeps">JobHandle to use as a dependency</param>
        public void Dispose(JobHandle inputDeps)
        {
            // TODO ??? return Jobhandle?
            cameFrom.Dispose(inputDeps);
            edgeBuffer.Dispose(inputDeps);
            minHeap.Dispose(inputDeps);
            pathBuffer.Dispose(inputDeps);
        }
        
        /// <summary>
        /// Evaluates and then reconstructs a path from start to goal.
        /// </summary>
        /// <param name="graph">The graph to perform the request on</param>
        /// <param name="start">The starting location</param>
        /// <param name="goal">The goal location</param>
        /// <param name="heuristicProvider">Heuristic provider to use</param>
        /// <param name="edgeMod">Edge modifier to use. Use <see cref="NoEdgeMod{TSeg}"/> for none.</param>
        /// <param name="pathProcessor">Path processor to use <see cref="NoProcessing{TNode}"/> for none.</param>
        /// <param name="pathBuffer">The segment buffer to append the path to. This buffer is not cleared.</param>
        /// <typeparam name="TGraph">The type of graph to find a path on</typeparam>
        /// <typeparam name="TNode">Type of nodes</typeparam>
        /// <typeparam name="TH">Type of heuristic provider</typeparam>
        /// <typeparam name="TMod">Type of edge modifier</typeparam>
        /// <typeparam name="TProc">Type of the path processor</typeparam>
        /// <typeparam name="TSeg">Type of segments make up the path</typeparam>
        /// <returns>A <see cref="AStarFindPathResult"/> struct indicating if a path was found and the offsets in the path buffer</returns>
        public AStarFindPathResult FindPath<TGraph, TH, TMod, TProc, TSeg>(
            ref TGraph graph, 
            
            TNode start, TNode goal,
            TH heuristicProvider, 
            TMod edgeMod,
            TProc pathProcessor,
            
            NativeList<TSeg> pathBuffer)
        
            where TGraph : struct, IGraph<TNode>
            where TProc : struct, IPathProcessor<TNode, TSeg>
            where TSeg : unmanaged 
            
            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>
        {
            
            int pathStartIndex = pathBuffer.Length;
            var evalResult = EvalPath(ref graph, start, goal, heuristicProvider, edgeMod);
            
            if (!evalResult.hasPath)
                return AStarFindPathResult.NoPath;
            
            this.pathBuffer.Clear();
            ReconstructPath(start, goal, pathProcessor.InsertQueryStart, this.pathBuffer);
            pathProcessor.ProcessPath(start, goal, this.pathBuffer, pathBuffer);
            
            return new AStarFindPathResult(evalResult, pathStartIndex, pathBuffer.Length - pathStartIndex);
        }
        
        /// <summary>
        /// Evaluates if a path exists between a start and goal. The path can later be reconstructed with <see cref="ReconstructPath"/>.
        /// Alternatively, use <see cref="FindPath{TGraph,TH,TMod,TProc,TSeg}"/>
        /// </summary>
        /// <param name="graph">The graph to perform the request on</param>
        /// <param name="start">The starting location</param>
        /// <param name="goal">The goal location</param>
        /// <param name="heuristicProvider">Heuristic provider to use</param>
        /// <param name="edgeMod">Edge modifier to use. Use <see cref="NoEdgeMod{TSeg}"/> for none.</param>
        /// <typeparam name="TGraph">The type of graph to find a path on</typeparam>
        /// <typeparam name="TNode">Type of nodes</typeparam>
        /// <typeparam name="TH">Type of heuristic provider</typeparam>
        /// <typeparam name="TMod">Type of edge modifier</typeparam>
        /// <returns>A <see cref="AStarFindPathResult"/> struct indicating if a path was found</returns>
        public AStarEvalResult EvalPath<TGraph, TH, TMod>(ref TGraph graph,
            TNode start, TNode goal,
            TH heuristicProvider,
            TMod edgeMod)

            where TGraph : struct, IGraph<TNode>
            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>

        {
            int expansion = 0;
            float costSoFar;
            
            Reset(start);
            heuristicProvider.SetGoal(goal); // init goal once
            minHeap.Push(new Open(start, 0, 0));

            while (
                expansion <= maxExpand &&
                TryPop(out TNode current, out costSoFar))
            {
                #if UNITY_EDITOR
                if (float.IsNaN(costSoFar))
                    throw new Exception("NaN cost detected. Job aborted.");
                #endif
                
                // goal reached?
                // note: current will *always* be the most recent node that was yielded by the graph
                // except for when it's the first
                if (current.Equals(goal))
                {
                    return new AStarEvalResult(true, costSoFar);
                }

                edgeBuffer.Clear();
                graph.Collect(current, ref edgeBuffer);
                edgeMod.ModifyEdgeBuffer(current, ref edgeBuffer);
               
                for (int i = 0; i < edgeBuffer.Length; i++)
                {
                    ref var e = ref edgeBuffer.ElementAt(i);
                    float edgeCost = e.Cost;
                    
                    if (!edgeMod.ModifyCost(current, e.Next, ref edgeCost))
                        continue; // if modifier returns false, we discard the edge
                    
                    float g = edgeCost + costSoFar;
                    
                    bool isExamined = TryGetCostSoFar(e.Next, ref expansion, out float nextCostSoFar);
                    if (!isExamined || g < nextCostSoFar)
                    {
                        cameFrom[e.Next] = new CameFrom(current, e.Next, g);
                        float f = g + heuristicProvider.Heuristic(e.Next); //* graph.Heuristic(next, goal);
                        
                        // No removing, we just insert the better value onto the queue as it will be processed earlier
                        // https://www3.cs.stonybrook.edu/~rezaul/papers/TR-07-54.pdf
                        minHeap.Push(new Open(e.Next, f, g));
                    }
                }
            }

            return AStarEvalResult.NoPath;
        }

        /// <summary>
        /// <para>
        /// Performs the Dijkstra algorithm on the graph. The result of which can be obtained via the cameFrom set.
        /// Every location that is present in <see cref="cameFrom"/> was reachable from the start of this query.
        /// </para>
        /// <para>
        /// You can obtain the cost of the path from g property of cameFrom.
        /// You can reconstruct a path to any reachable destination using <see cref="ReconstructPath"/> after this query has ran.
        /// Be careful though and supply the same starting node to that method as was used in this query. If you don't do this, that call may result
        /// into an infinite loop or return an invalid result.
        /// </para>
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="start"></param>
        /// <param name="maxCost">Cost budget for expanding. The algorithm will not traverse further if the cost exceeds this value. Nodes beyond this cost will be considered as not reachable</param>
        /// <typeparam name="TGraph"></typeparam>
        /// <typeparam name="TNode"></typeparam>
        /// <remarks>
        /// <para>
        ///  *Warning* If no max cost is specified and your graph has no boundary (which could be the case for an infinite grid)
        ///  then running this algorithm will result in an infinite loop, as the algorithm will keep expandig.
        ///  All of the included graphs with AnyPath have a set boundary, so if you're using any of these you don't have to worry about this.
        ///  It will however still benefit performance to set a maximum cost.
        /// </para>
        /// <para>
        /// The maxExpand setting on this object is not used.
        /// </para>
        /// </remarks>
        public void Dijkstra<TGraph, TMod>(ref TGraph graph, TNode start, TMod edgeMod, float maxCost = float.PositiveInfinity)

            where TGraph : struct, IGraph<TNode>
            where TMod : struct, IEdgeMod<TNode>

        {
            float costSoFar;
            int expansion = 0;
            Reset(start);
            minHeap.Push(new Open(start, 0, 0));
            
            while (TryPop(out TNode current, out costSoFar))
            {
#if UNITY_EDITOR
                if (float.IsNaN(costSoFar))
                    throw new Exception("NaN cost detected. Job aborted.");
#endif
                
                if (costSoFar > maxCost)
                    return;
                
                edgeBuffer.Clear();
                graph.Collect(current, ref edgeBuffer);
                edgeMod.ModifyEdgeBuffer(current, ref edgeBuffer);
                
                for (int i = 0; i < edgeBuffer.Length; i++)
                {
                    ref var e = ref edgeBuffer.ElementAt(i);
                    float edgeCost = e.Cost;
                    
                    if (!edgeMod.ModifyCost(current, e.Next, ref edgeCost))
                        continue; // if modifier returns false, we discard the edge

                    float g = edgeCost + costSoFar;
                            
                    bool isExamined = TryGetCostSoFar(e.Next, ref expansion, out float nextCostSoFar);
                    if (!isExamined || g < nextCostSoFar)
                    {
                        cameFrom[e.Next] = new CameFrom(current, e.Next, g);
                        minHeap.Push(new Open(e.Next, g, g));
                    }
                }
            }
        }
        
        /// <summary>
        /// Appends the last found path from start to goal into pathBuffer
        /// </summary>
        /// <param name="start">The starting node of the original request</param>
        /// <param name="goal">The goal node of the original request</param>
        /// <param name="insertQueryStart">Should the starting node be part of the path?</param>
        /// <param name="pathBuffer">The buffer to append the path to. This list is not cleared.</param>
        /// <remarks>
        /// Only call this method when certain a path existed from start to goal, otherwise, this may result in an infinite loop.
        /// </remarks>
        public void ReconstructPath(TNode start, TNode goal, bool insertQueryStart, NativeList<TNode> pathBuffer)
        {
            int startIndex = pathBuffer.Length;
            TNode current = goal;

            while (!current.Equals(start))
            {
                if (!cameFrom.TryGetValue(current, out var cf))
                    break;

                // Note that we always add the next that was set on the cameFrom struct itself
                // because we must always use the latest version of "Next" that was added by the Collect() method and was lowest on the heap.
                // my previous attempt reconstructed only by the Prev nodes, but this could go wrong because that value wasn't necessary
                // equal to the lowest heap inserted node. The coarse path would be OK, but the exact node struct values could differ
                // (on implicit graphs, the node equality operator is overridden to match only the core ID, but some values in it may be different)
                pathBuffer.Add(cf.next);
                current = cf.prev;
            }
            
            if (insertQueryStart)
                pathBuffer.Add(start);

            // Path needs to be reversed in order to be forward
            int length = pathBuffer.Length - startIndex;
            int jEnd = pathBuffer.Length - 1;
            for (int i = 0; i < length / 2; i++)
            {
                int j = startIndex + i;
                var tmp = pathBuffer[j];
                pathBuffer[j] = pathBuffer[jEnd - i];
                pathBuffer[jEnd - i] = tmp;
            }
        }
        
        /// <summary>
        /// Returns the key-value pairs of all nodes and segments A* expanded too since the last usage.
        /// </summary>
        public NativeKeyValueArrays<TNode, CameFrom> DebugGetAllExpansion(Allocator allocator)
        {
            return cameFrom.GetKeyValueArrays(allocator);
        }
        
        /// <summary>
        /// Called by the algorithm when a new evaluation takes place
        /// </summary>
        private void Reset(TNode start)
        {
            pathBuffer.Clear();
            minHeap.Clear();
            cameFrom.Clear();
            cameFrom[start] = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryPop(out TNode node, out float g)
        {
            if (!minHeap.TryPop(out var value))
            {
                node = default;
                g = 0;
                return false;
            }

            node = value.node;
            g = value.g;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetCostSoFar(in TNode node, ref int expansion, out float costSoFar)
        {
            if (this.cameFrom.TryGetValue(node, out var cf))
            {
                costSoFar = cf.g;
                return true;
            }

            expansion++;
            costSoFar = 0;
            return false;
        }
        
        [ExcludeFromDocs]
        public readonly struct CameFrom
        {
            /// <summary>
            /// The node that lead into next
            /// </summary>
            public readonly TNode prev;
            
            /// <summary>
            /// The node that came from prev
            /// </summary>
            public readonly TNode next;
            
            /// <summary>
            /// Cost from the beginning of the path
            /// </summary>
            public readonly float g;

            public CameFrom(TNode prev, TNode next, float g)
            {
                this.g = g;
                this.prev = prev;
                this.next = next;
            }
        }
        
        private readonly struct Open 
        {
            public readonly TNode node;
            public readonly float f;
            public readonly float g;

            public Open(TNode node, float f, float g)
            {
                this.node = node;
                this.f = f;
                this.g = g;
            }
        }
        
        
        private struct OpenComp : IRefComparer<Open>
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Compare(ref Open x, ref Open y) => x.f.CompareTo(y.f);
        }
    }
}