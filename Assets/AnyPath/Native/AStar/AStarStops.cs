using System;
using Unity.Collections;

namespace AnyPath.Native
{
    /// <summary>
    /// Burst compatible methods that find a path that visits multiple stops in order.
    /// </summary>
    /// <remarks>All of the methods contained in the AnyPath.Native namespace are compatible with Unity's Burst compiler</remarks>
    public static class AStarStops
    {
       
        /// <summary>
        /// Evaluates and then reconstructs a path from start to goal.
        /// </summary>
        /// <param name="graph">The graph to perform the request on</param>
        /// <param name="stops">The stops to visit in order. The first stop is the starting location and the last stop is the goal location.
        /// If less than two stops are provided, no path will be found.</param>
        /// <param name="aStar">Working memory for the algorithm to use</param>
        /// <param name="pathProcessor">Path processor to use</param>
        /// <param name="pathBuffer">The segment buffer to append the path to. This buffer is not cleared.</param>
        /// <typeparam name="TGraph">The type of graph to find a path on</typeparam>
        /// <typeparam name="TNode">Type of nodes</typeparam>
        /// <typeparam name="TSeg">Type of segments make up the path</typeparam>
        /// <typeparam name="TProc">Type of the path processor</typeparam>
        /// <returns>A <see cref="AStarFindPathResult"/> struct indicating if a path was found and the offsets in the path buffer</returns>
        public static AStarFindPathResult FindPathStops<TGraph, TNode, TH, TMod, TProc, TSeg>(
            ref this AStar<TNode> aStar,
            ref TGraph graph, 
            NativeSlice<TNode> stops,
            
            TH heuristicProvider, 
            TMod edgeMod, 
            
            TProc pathProcessor,
            NativeList<TSeg> pathBuffer)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>

            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>
        
            where TProc : struct, IPathProcessor<TNode, TSeg>
            where TSeg : unmanaged

        {
            // we can't break this down by calling EvalPathStops because we need to reconstruct intermediate paths from the memory
            // !note! don't throw an exception, FindFirstTarget uses an 'empty' offsetinfo that results in zero stops as a measure to prune
            // invalid targets
            if (stops.Length <= 1)
                return AStarFindPathResult.NoPath;
            
            int pathEdgeStartIndex = pathBuffer.Length;
            float costSum = 0;
            
            for (int i = 0; i < stops.Length - 1; i++)
            {
                var result = aStar.FindPath(
                    graph: ref graph,
                    start: stops[i], goal: stops[i + 1],
                    heuristicProvider: heuristicProvider, 
                    edgeMod: edgeMod, 
                    pathProcessor: pathProcessor, 
                    pathBuffer: pathBuffer);
                
                if (!result.evalResult.hasPath)
                {
                    // cancel total result by resizing back to original size
                    pathBuffer.ResizeUninitialized(pathEdgeStartIndex);
                    return AStarFindPathResult.NoPath;
                }

                costSum += result.evalResult.cost;
            }
            
            return new AStarFindPathResult(new AStarEvalResult(true, costSum), pathEdgeStartIndex, pathBuffer.Length - pathEdgeStartIndex);
        }
        
        /// <summary>
        /// Similar to FindPathStops but only evaluates a path that visits the stops in order exists.
        /// </summary>
        public static AStarEvalResult EvalPathStops<TGraph, TNode, TH, TMod>(
            ref this AStar<TNode> aStar,
            ref TGraph graph, 
            NativeSlice<TNode> stops,
            ref TH heuristicProvider, 
            ref TMod edgeMod)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>

            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>

        {
            if (stops.Length <= 1)
                return AStarEvalResult.NoPath;
           
            float costSum = 0;
            
            for (int i = 0; i < stops.Length - 1; i++)
            {
                var goal = stops[i + 1];
                var result = aStar.EvalPath(
                    graph: ref graph, 
                    start: stops[i], goal: goal,
                    heuristicProvider: heuristicProvider, 
                    edgeMod: edgeMod);
                
                if (!result.hasPath)
                    return AStarEvalResult.NoPath;

                costSum += result.cost;
            }

            return new AStarEvalResult(true, costSum);
        }
    }
}