using System;
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
    /// <typeparam name="TSeg">The path segment type associated with the type of graph</typeparam>
    /// <typeparam name="TProc">The path processor used by this finder. See <see cref="EdgeMod"/> for details</typeparam>
    /// <typeparam name="TJob">The job that this finder uses internally to perform the path finding</typeparam>
    /// <typeparam name="TResult">The type of result this finder gives</typeparam>
    public abstract class Finder<TGraph, TNode, TH, TMod, TProc, TSeg, TJob, TResult> : Eval<TGraph, TNode, TH, TMod, TJob, TResult>, 
        ISetFinderPathProcessor<TProc>

        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
        where TSeg : unmanaged
        
        where TProc : struct, IPathProcessor<TNode, TSeg>
        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>

        where TJob : struct, IJobFinder<TGraph, TNode, TH, TMod, TProc, TSeg>
    
    {
        /// <summary>
        /// Optional edge modifier to use with the request.
        /// </summary>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        public TProc PathProcessor
        {
            get => job.PathProcessor;
            set
            {
                if (!IsMutable) throw new ImmutableFinderException();
                job.PathProcessor = value;
            }
        }
    }
}