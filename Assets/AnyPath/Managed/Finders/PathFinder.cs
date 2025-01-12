using System;
using AnyPath.Managed.Finders.Common;
using AnyPath.Managed.Results;
using AnyPath.Native;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders
{
    /// <summary>
    /// Basic finder that finds a path between two nodes on a graph.
    /// </summary>
    public class PathFinder<TGraph, TNode, TH, TMod, TProc, TSeg> : 
        Finder<TGraph, TNode, TH, TMod, TProc, TSeg, PathFinder<TGraph, TNode, TH, TMod, TProc, TSeg>.Job, Path<TSeg>>,
        
        IGetFinderStops<TNode>
    
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
        
        where TProc : struct, IPathProcessor<TNode, TSeg>
        where TSeg : unmanaged

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>

    {
        private InStops<TNode, Job> stops;

        public PathFinder() : this(1)
        {
        }

        public PathFinder(int initialCapacity)
        {
            stops = new InStops<TNode, Job>(this, initialCapacity);
        }
        
        /// <summary>
        /// <para>
        /// Set to true to reuse the same instance of the result for every request.
        /// This minimizes or totally removes any heap allocations.
        /// </para>
        /// <remarks>
        /// <para>
        /// This can benefit performance when used frequently, but be careful about using the result of the pathfinding
        /// query. Because the same instance of the result is used and modified when a new query finishes, anything else
        /// referencing <see cref="ManagedGraphJobWrapper{TGraph,TNode,TJob,TResult}.Result"/> may see it's contents change
        /// and this could lead to unexpected behaviour.
        /// </para>
        /// <para>
        /// When this finder is cleared, <see cref="ManagedGraphJobWrapper{TGraph,TNode,TJob,TResult}.Result"/> is null. However,
        /// when a new query finishes, it points to the same underlying instance again (which now holds the latest data).
        /// </para>
        /// </remarks>
        /// </summary>
        public bool ReuseResult
        {
            get => reuseResult;
            set => reuseResult = value;
        }

        /// <summary>
        /// The starting location and stops for the request
        /// </summary>
        public IFinderStops<TNode> Stops => stops;
        
        public override void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
        {
            base.Clear(flags);
            stops.Clear(flags);
        }

        protected override void AssignContainers(ref Job job)
        {
            base.AssignContainers(ref job);
            OutPath<TSeg>.AssignContainers(ref job);
            stops.AssignContainers(ref job);
        }
        
        protected override void ReturnContainers(ref Job job)
        {
            base.ReturnContainers(ref job);
            OutPath<TSeg>.ReturnContainers(ref job);
            stops.ReturnContainers(ref job);
        }
        
        protected override void DisposeContainers(ref Job job, JobHandle inputDeps)
        {
            base.DisposeContainers(ref job, inputDeps);
            OutPath<TSeg>.DisposeContainers(ref job, inputDeps);
            stops.DisposeContainers(ref job, inputDeps);
        }

        protected override Path<TSeg> CreateResult(ref Job job, out bool resultCreated)
        {
            resultCreated = true;
            return new Path<TSeg>(job.result.Value, job.path);
        }

        protected override void HydrateResult(ref Path<TSeg> result, ref Job job) => Path<TSeg>.Hydrate(result, job.Result.Value, job.path);

        [BurstCompile, ExcludeFromDocs]
        public struct Job : IJobFinder<TGraph, TNode, TH, TMod, TProc, TSeg>, IJobStops<TNode>, IJobPathBuffer<TSeg>, IJobFindPathResult
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeList<TNode> stops;
            [ReadOnly] public TH heuristicProvider;
            [ReadOnly] public TMod mod;
            [ReadOnly] public TProc pathProcessor;
        
            public AStar<TNode> aStar;
            public NativeReference<AStarFindPathResult> result;
            public NativeList<TSeg> path;

            public void Execute()
            {
                CheckIfBurstCompiled();
                result.Value = aStar.FindPathStops(
                    stops: new NativeSlice<TNode>(stops.AsArray()),
                    graph: ref graph, 
                    heuristicProvider: heuristicProvider, 
                    edgeMod: mod, 
                    pathProcessor: pathProcessor, 
                    pathBuffer: path);
            }

            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled! Please check if you have a concrete definition of this class using the AnyPath code generator");
#endif
            }

            public TGraph Graph { get => graph; set => graph = value; }
            public TH HeuristicProvider { get => heuristicProvider; set => heuristicProvider = value; }
            public TMod EdgeMod { get => mod; set => mod = value; }
            public AStar<TNode> AStar { get => aStar; set => aStar = value; }
            public NativeList<TNode> Stops { get => stops; set => stops = value; }
            public NativeList<TSeg> PathBuffer { get => path; set => path = value; }
            public NativeReference<AStarFindPathResult> Result { get => result; set => result = value; }
            public TProc PathProcessor { get => pathProcessor; set => pathProcessor = value; }
        }
    }
}