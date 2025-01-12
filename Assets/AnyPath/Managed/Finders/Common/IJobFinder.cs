using System;
using AnyPath.Native;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    /// <summary>
    /// Shared interface for all jobs used internally by finders. This allows for directly setting the structs on the job.
    /// </summary>
    [ExcludeFromDocs]
    public interface IJobFinder<TGraph, TNode, TH, TMod, TProc, TSeg> : IJobEval<TGraph, TNode, TH, TMod>
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    
        where TProc : struct, IPathProcessor<TNode, TSeg>
        where TSeg : unmanaged
    {
        TProc PathProcessor { get; set; }
    }
    
    public interface IJobEval<TGraph, TNode, TH, TMod> : IJobGraphAStar<TGraph, TNode>
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    {
        TMod EdgeMod { get; set; }
        TH HeuristicProvider { get; set; }
    }

    public interface IJobGraphAStar<TGraph, TNode> : IJob
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
    {
        TGraph Graph { get; set; }
        AStar<TNode> AStar { get; set; }
    }
}