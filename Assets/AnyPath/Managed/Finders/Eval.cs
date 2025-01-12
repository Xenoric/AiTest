using System;
using System.Collections;
using AnyPath.Managed.Finders.Common;
using AnyPath.Native;

namespace AnyPath.Managed.Finders
{
    /// <summary>
    /// A reusable "PathFinder" that encapsulates scheduling pathfinding Jobs.
    /// Handles basic functionality shared by all Finders like assigning the graph, settings and memory to a job.
    /// </summary>
    /// <remarks>
    /// You shouldn't have to use this class directly unless you want to implement your own specialized finder.
    /// Instead, use the concrete finder types that can be generated for each graph type. See the documentation for details.
    /// </remarks>
    /// <typeparam name="TGraph">The type of graph this finder operates on</typeparam>
    /// <typeparam name="TNode">The node type associated with the type of graph</typeparam>
    /// <typeparam name="TH">Type of heuristic provider</typeparam>
    /// <typeparam name="TMod">Type of edge modifier</typeparam>
    /// <typeparam name="TJob">The job that this finder uses internally to perform the path finding</typeparam>
    /// <typeparam name="TResult">The type of result this finder gives</typeparam>
    public abstract class Eval<TGraph, TNode, TH, TMod, TJob, TResult> : ManagedGraphJobWrapper<TGraph, TNode, TJob, TResult>,

        IEnumerator,
        ISetFinderHeuristicProvider<TH>,
        IFinder<TGraph, TMod, TResult>
    
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
        
        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>

        where TJob : struct, IJobEval<TGraph, TNode, TH, TMod>
    
    {
        /// <summary>
        /// The maximum number of nodes A* may expand into before "giving up". This can provide as an upper bound for targets
        /// that are unreachable, reducing computation time because the algorithm doesn't have to search
        /// the entire graph before knowing for certain that a target is unreachable.
        /// </summary>
        /// <remarks>See <see cref="AnyPath.Native.AStar{TNode}.maxExpand"/></remarks>
        public int MaxExpand
        {
            get => maxExpand;
            set
            {
                if (!IsMutable) throw new ImmutableFinderException();
                this.maxExpand = value;
            }
        }
        
        /// <summary>
        /// Optional edge modifier to use with the request.
        /// </summary>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        public TMod EdgeMod
        {
            get => job.EdgeMod;
            set
            {
                if (!IsMutable) throw new ImmutableFinderException();
                job.EdgeMod = value;
            }
        }
        
        /// <summary>
        /// Optional edge modifier to use with the request.
        /// </summary>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        public TH HeuristicProvider
        {
            get => job.HeuristicProvider;
            set
            {
                if (!IsMutable) throw new ImmutableFinderException();
                job.HeuristicProvider = value;
            }
        }

        /// <summary>
        /// Clear this finder allowing for a new request to be made.
        /// </summary>
        /// <remarks>If the previous request was not completed yet, that request will be aborted.</remarks>
        /// <param name="flags">Flags that can be used to preserve certain settings on this finder</param>
        public override void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
        {
            base.Clear(flags);
         
            if ((flags & ClearFinderFlags.KeepHeuristicProvider) == 0)
                job.HeuristicProvider = default;
            if ((flags & ClearFinderFlags.KeepEdgeMod) == 0)
                job.EdgeMod = default;
        }
    }
}