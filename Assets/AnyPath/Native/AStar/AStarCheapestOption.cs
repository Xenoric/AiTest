using System;
using Unity.Collections;

namespace AnyPath.Native
{
    /// <summary>
    /// Burst compatible methods to find the cheapest path between a starting node and a set of possible targets.
    /// </summary>
    /// <remarks>All of the methods contained in the AnyPath.Native namespace are compatible with Unity's Burst compiler</remarks>
    public static class AStarCheapestOption
    {
        /// <summary>
        /// Finds the path of the "cheapest" option that is encountered that has a valid path.
        /// This method works by comparing the heuristic value for an option against the cost of the currently known cheapest option.
        /// If that heuristic value is larger than the cost of the current cheapest path, that option is discarded before trying to find a path.
        /// Because of this, it might pay off to pre-sort the options provided by their start to goal heuristic value, lowering the chance
        /// of succcesive options being evaluated. This method does *not* pre-sort automatically though.
        /// </summary>
        /// <param name="graph">The graph to perform the request on</param>
        /// <param name="nodes">A flattened representation of all the stops to visit in order, per option</param>
        /// <param name="offsets">
        /// Describes the mapping of stops from the nodes array per option using a starting index and length.
        /// For example, if you have two options that both only have a start and goal stop, this would look as follows:
        /// offets[0] = { start: 0, length: 2 }
        /// offsets[1] = { start: 2 : length 2 }
        /// </param>
        /// <param name="aStar">The memory container for the algorithm to use</param>
        /// <param name="pathProcessor">Path processor to use</param>
        /// <param name="tempBuffer1">A temporary buffer to an intermediate path in</param>
        /// <param name="tempBuffer2">Another temporary buffer to store an intermediate path</param>
        /// <param name="pathBuffer">The edge buffer to append the path to</param>
        /// <typeparam name="TGraph">The type of graph to find a path on</typeparam>
        /// <typeparam name="TNode">Type of nodes</typeparam>
        /// <typeparam name="TSeg">Type of segments make up the path</typeparam>
        /// <typeparam name="TProc">Type of the path processor</typeparam>
        /// <returns>A <see cref="AStarFindOptionResult"/> struct indicating if a path was found and the offsets in the path buffer</returns>
        /// <remarks>The temporary buffers are neccessary for the algorithm but won't contain a meaningful value afterwards. You can
        /// preallocate these and reuse them for each call. The temporary buffers are cleared before usage in this method.</remarks>
        public static AStarFindOptionResult FindCheapestOption<TGraph, TNode, TH, TMod, TProc, TSeg>(
            ref this AStar<TNode> aStar, 
            ref TGraph graph, 
            NativeSlice<TNode> nodes, 
            NativeSlice<OffsetInfo> offsets,
            
            TH heuristicProvider,
            TMod edgeMod, 
            NativeList<TSeg> tempBuffer1, 
            NativeList<TSeg> tempBuffer2, 
            
            TProc pathProcessor,
            NativeList<TSeg> pathBuffer)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>
         
            
            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>
        
            where TProc : struct, IPathProcessor<TNode, TSeg>
            where TSeg : unmanaged

        {

            tempBuffer1.Clear();
            tempBuffer2.Clear();

            float minCost = float.PositiveInfinity;
            int origLength = pathBuffer.Length;
            bool swapState = false;
            int minCostIndex = -1;
            
            for (int i = 0; i < offsets.Length; i++)
            {
                
                var offset = offsets[i];
                var stops = offset.Slice(nodes);
                if (GetTotalHeuristic(ref heuristicProvider, stops) > minCost)
                    continue;

                var buffer = swapState ? tempBuffer2 : tempBuffer1;
                buffer.Clear();
                var result = aStar.FindPathStops(
                    graph: ref graph, 
                    stops: stops,
                    heuristicProvider: heuristicProvider, 
                    edgeMod: edgeMod, 
                    pathProcessor: pathProcessor, 
                    pathBuffer: buffer);
                
                if (!result.evalResult.hasPath)
                    continue;
                
#if UNITY_EDITOR
                // if somehow the path cost was infinite, this will cause problems because cost will never be smaller than
                // so we wont return a result. can't use <= because we want the first encounter.
                if (float.IsPositiveInfinity(result.evalResult.cost))
                    throw new Exception("Result has an infinite cost. This is not supported for shortest path evaluation");
#endif
                
                if (result.evalResult.cost < minCost)
                {
                    swapState = !swapState;
                    minCost = result.evalResult.cost;
                    minCostIndex = i;
                }
            }

            if (minCostIndex < 0)
            {
                return AStarFindOptionResult.NoPath;
            }
            
            pathBuffer.AddRange(swapState ? tempBuffer1.AsArray() : tempBuffer2.AsArray());

            return 
                new AStarFindOptionResult(minCostIndex, new AStarFindPathResult(new AStarEvalResult(true, minCost), origLength, pathBuffer.Length - origLength));
        }
        
        /// <summary>
        /// Similar to FindCheapestOption but only returns the index of the option that was the cheapest.
        /// </summary>
        public static AStarEvalOptionResult EvalCheapestTarget<TGraph, TNode, TH, TMod>(
            ref this AStar<TNode> aStar, 
            ref TGraph graph, 
            NativeSlice<TNode> nodes, NativeSlice<OffsetInfo> offsets,
            
            ref TH heuristicProvider, 
            ref TMod edgeMod)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>
            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>

        {
            
            float minCost = float.PositiveInfinity;
            int minCostIndex = -1;
            
            for (int i = 0; i < offsets.Length; i++)
            {
                var offset = offsets[i];
                var stops = offset.Slice(nodes);
                if (GetTotalHeuristic(ref heuristicProvider, stops) > minCost)
                    continue;

                var result = aStar.EvalPathStops(
                    graph: ref graph, 
                    stops: stops,
                    heuristicProvider: ref heuristicProvider, 
                    edgeMod: ref edgeMod);
                
#if UNITY_EDITOR
                // if somehow the path cost was infinite, this will cause problems because cost will never be smaller than
                // so we wont return a result. can't use <= because we want the first encounter.
                if (float.IsPositiveInfinity(result.cost))
                    throw new Exception("Result has an infinite cost. This is not supported for shortest path evaluation");
#endif
                
                if (result.hasPath && result.cost < minCost)
                {
                    minCost = result.cost;
                    minCostIndex = i;
                }
            }

            return minCostIndex < 0
                ? AStarEvalOptionResult.NoPath
                : new AStarEvalOptionResult(minCostIndex, new AStarEvalResult(true, minCost));
        }
        
        /// <summary>
        /// Calculates the heuristic for a series of stops.
        /// E.g. h(stops[0], stops[1]) + h(stops[1], stops[2]) + ...
        /// </summary>
        /// <param name="provider">The graph that provides the heuristic function</param>
        /// <param name="stops">The stops to calculate a combined heurisic value for</param>
        /// <typeparam name="TGraph">Type of graph</typeparam>
        /// <typeparam name="TNode">Type of nodes</typeparam>
        /// <typeparam name="TSeg">Type of segments</typeparam>
        /// <returns>Heurstic for a series of stops</returns>
        public static float GetTotalHeuristic<TNode, TH>(ref TH provider, NativeSlice<TNode> stops)
            where TH : struct, IHeuristicProvider<TNode>
            where TNode : unmanaged, IEquatable<TNode>
        {
            float h = 0;
            for (int i = 0; i < stops.Length - 1; i++)
            {
                provider.SetGoal(stops[i + 1]);
                h += provider.Heuristic(stops[i]);
            }

            return h;
        }
    }
}