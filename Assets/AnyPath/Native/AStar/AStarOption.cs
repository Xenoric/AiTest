using System;
using Unity.Collections;

namespace AnyPath.Native
{
    /// <summary>
    /// Burst compatible methods to find the first option for which a path exists.
    /// </summary>
    /// <remarks>All of the methods contained in the AnyPath.Native namespace are compatible with Unity's Burst compiler</remarks>
    public static class AStarOption
    {
        /// <summary>
        /// Finds the path of the first option that is encountered that has a valid path.
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
        /// <param name="pathBuffer">The edge buffer to append the path to</param>
        /// <typeparam name="TGraph">The type of graph to find a path on</typeparam>
        /// <typeparam name="TNode">Type of nodes</typeparam>
        /// <typeparam name="TSeg">Type of segments make up the path</typeparam>
        /// <typeparam name="TProc">Type of the path processor</typeparam>
        /// <returns>A <see cref="AStarFindOptionResult"/> struct indicating if a path was found and the offsets in the path buffer</returns>
        public static AStarFindOptionResult FindOption<TGraph, TNode, TH, TMod, TProc, TSeg>(
            this ref AStar<TNode> aStar, 
            ref TGraph graph, 
            NativeSlice<TNode> nodes, NativeSlice<OffsetInfo> offsets,
            
            TH heuristicProvider, 
            TMod edgeMod, 
            
            TProc pathProcessor,
            NativeList<TSeg> pathBuffer)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>
            
            where TProc : struct, IPathProcessor<TNode, TSeg>
            where TSeg : unmanaged

            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>

        {
            // can't break this down by calling EvalFirstTarget because intermediate paths will be lost
            // int origLength = pathBuffer.Length;

            for (int i = 0; i < offsets.Length; i++)
            {
                var offset = offsets[i];
                var result = aStar.FindPathStops(
                    graph: ref graph,
                    stops: offset.Slice(nodes),
                    heuristicProvider: heuristicProvider, 
                    edgeMod: edgeMod,
                    pathProcessor: pathProcessor, 
                    pathBuffer: pathBuffer);
                
                if (result.evalResult.hasPath)
                    return new AStarFindOptionResult(i, result);
                
                // discard old result
                // pathBuffer.ResizeUninitialized(origLength); <- already done in FindPathStops
            }
            
            return AStarFindOptionResult.NoPath;
        }
        
        /// <summary>
        /// Similar to FindOption but only evaluates the first index of the option for which a path exists.
        /// </summary>
        public static AStarEvalOptionResult Evaloption<TGraph, TNode, TH, TMod>(
            ref this AStar<TNode> aStar,
            ref TGraph graph, 
            NativeSlice<TNode> nodes, NativeSlice<OffsetInfo> offsets, TH heuristicProvider, TMod edgeMod)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>
          
            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>

        {
            for (int i = 0; i < offsets.Length; i++)
            {
                var offset = offsets[i];
                var result = aStar.EvalPathStops(
                    graph: ref graph, 
                    stops: offset.Slice(nodes),
                    heuristicProvider: ref heuristicProvider, 
                    edgeMod: ref edgeMod);
                
                if (result.hasPath)
                    return new AStarEvalOptionResult(i, result);
            }
            
            return AStarEvalOptionResult.NoPath;
        }
        
        /// <summary>
        /// Finds the path of the first option that is encountered that has a valid path. But uses the indicices in the
        /// remap array to determine the order of evaluation.
        /// </summary>
        /// <param name="graph">The graph to perform the request on</param>
        /// <param name="remap">
        /// Array containing the order of evaluation for the options.
        /// If for example you want to evaluate the options in reverse order, a remap array for 2 options would look like:
        /// remap[0] = 1, remap[1] = 0
        /// </param>
        /// <param name="nodes">A flattened representation of all the stops to visit in order, per option</param>
        /// <param name="offsets">
        /// Describes the mapping of stops from the nodes array per option using a starting index and length.
        /// For example, if you have two options that both only have a start and goal stop, this would look as follows:
        /// offets[0] = { start: 0, length: 2 }
        /// offsets[1] = { start: 2 : length 2 }
        /// </param>
        /// <param name="aStar">Working memory for the algorithm to use</param>
        /// <param name="pathProcessor">Path processor to use</param>
        /// <param name="pathBuffer">The segment buffer to append the path to. This buffer is not cleared.</param>
        /// <typeparam name="TGraph">The type of graph to find a path on</typeparam>
        /// <typeparam name="TNode">Type of nodes</typeparam>
        /// <typeparam name="TSeg">Type of segments make up the path</typeparam>
        /// <typeparam name="TProc">Type of the path processor</typeparam>
        /// <returns>A <see cref="AStarFindOptionResult"/> struct indicating if a path was found and the offsets in the path buffer. Note that
        /// the index that is returned is the original index of the option in the offsets array.</returns>
        /// <remarks>This method is used by the Priority Finder.</remarks>
        public static AStarFindOptionResult FindOptionRemap<TGraph, TNode, TH, TMod, TProc, TSeg>(
            ref this AStar<TNode> aStar,
            ref TGraph graph, 
            
            NativeSlice<int> remap, NativeSlice<TNode> nodes, NativeSlice<OffsetInfo> offsets,
            TH heuristicProvider, 
            TMod edgeMod,
            TProc pathProcessor,
            NativeList<TSeg> pathBuffer)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>
            
            where TProc : struct, IPathProcessor<TNode, TSeg>
            where TSeg : unmanaged
            
            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>

        {
            for (int j = 0; j < remap.Length; j++)
            {
                int i = remap[j];
                var offset = offsets[i];
                var result = aStar.FindPathStops(stops: offset.Slice(nodes), 
                    graph: ref graph, 
                    heuristicProvider: heuristicProvider, 
                    edgeMod: edgeMod, 
                    pathProcessor : pathProcessor,
                    pathBuffer: pathBuffer);
                
                if (result.evalResult.hasPath)
                    return new AStarFindOptionResult(i, result);
            }
            
            return AStarFindOptionResult.NoPath;
        }
        
        /// <summary>
        /// Similar to FindOptionRemap but only returns the first option for which a path exists.
        /// </summary>
        public static AStarEvalOptionResult EvalOptionRemap<TGraph, TNode, TH, TMod>(
            ref this AStar<TNode> aStar,
            ref TGraph graph, 
            NativeSlice<int> remap, NativeSlice<TNode> nodes, NativeSlice<OffsetInfo> offsets,
            
            TH heuristicProvider, 
            TMod edgeMod)

            where TGraph : struct, IGraph<TNode>
            where TNode : unmanaged, IEquatable<TNode>
            
            where TH : struct, IHeuristicProvider<TNode>
            where TMod : struct, IEdgeMod<TNode>
        
        {
            for (int j = 0; j < offsets.Length; j++)
            {
                int i = remap[j];
                var offset = offsets[i];
                var result = aStar.EvalPathStops(
                    graph: ref graph,
                    stops: offset.Slice(nodes),
                    heuristicProvider: ref heuristicProvider,
                    edgeMod: ref edgeMod);
                
                if (result.hasPath)
                    return new AStarEvalOptionResult(i, result);
            }
            
            return AStarEvalOptionResult.NoPath;
        }
    }
}