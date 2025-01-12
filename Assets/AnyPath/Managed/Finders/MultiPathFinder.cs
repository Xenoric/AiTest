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
    /// Batches multiple pathfinding queries into one job.
    /// </summary>
    public class MultiPathFinder<TGraph, TNode, TH, TMod, TProc, TSeg> : Finder<TGraph, TNode, TH, TMod, TProc, TSeg,
            MultiPathFinder<TGraph, TNode, TH, TMod, TProc, TSeg>.Job, MultiPathResult<TSeg>>,
        
        //IGetFinderMultiRequests<TNode>,
        IMultiFinder<TGraph, TNode, MultiPathResult<TSeg>> 
        
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    
        where TProc : struct, IPathProcessor<TNode, TSeg>
        where TSeg : unmanaged

    {
        private InMulti<TNode, Job> requestMulti;

        public MultiPathFinder() : this(4)
        {
        }
        
        public MultiPathFinder(int initialCapacity)
        {
            this.requestMulti = new InMulti<TNode, Job>(this, initialCapacity);
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

        public IAddMulti<TNode> Requests => requestMulti;

        public override void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
        {
            base.Clear(flags);
            requestMulti.Clear(flags);
        }

        protected override void AssignContainers(ref Job job)
        {
            base.AssignContainers(ref job);
            OutMultiPath<TSeg>.AssignContainersMulti(ref job);
            requestMulti.AssignContainers(ref job);
        }

        protected override void DisposeContainers(ref Job job, JobHandle inputDeps)
        {
            base.DisposeContainers(ref job, inputDeps);
            OutMultiPath<TSeg>.DisposeContainersMulti(ref job, inputDeps);
            requestMulti.DisposeContainers(ref job, inputDeps);
        }

        protected override void ReturnContainers(ref Job job)
        {
            base.ReturnContainers(ref job);
            OutMultiPath<TSeg>.ReturnContainersMulti(ref job);
            requestMulti.ReturnContainers(ref job);
        }

        protected override MultiPathResult<TSeg> CreateResult(ref Job job, out bool resultCreated)
        {
            resultCreated = true;
            return MultiPathResult<TSeg>.Create(ref job);
        }

        protected override void HydrateResult(ref MultiPathResult<TSeg> result, ref Job job) => MultiPathResult<TSeg>.Hydrate(result, ref job);

        [BurstCompile, ExcludeFromDocs]
        public struct Job : IJobFinder<TGraph, TNode, TH, TMod, TProc, TSeg>, IJobMulti<TNode>, IJobPathBuffer<TSeg>, IJobMultiPathResult
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeList<TNode> nodes;
            [ReadOnly] public NativeList<OffsetInfo> offsets;
            [ReadOnly] public TH heuristicProvider;
            [ReadOnly] public TMod mod;
            [ReadOnly] public TProc pathProcessor;

            public AStar<TNode> aStar;
            public NativeList<AStarFindPathResult> results;
            public NativeList<TSeg> pathBuffer;

            public void Execute()
            {
                CheckIfBurstCompiled();
                
                results.Clear();
                for (int i = 0; i < offsets.Length; i++)
                    results.Add(aStar.FindPathStops(
                        stops: offsets[i].Slice(nodes), 
                        graph: ref graph, 
                        heuristicProvider: heuristicProvider, 
                        edgeMod: mod, 
                        pathProcessor: pathProcessor,
                        pathBuffer: pathBuffer));
            }
            
            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled!");
#endif
            }
            
            public TGraph Graph { get => graph; set => graph = value; }
            public TH HeuristicProvider { get => heuristicProvider; set => heuristicProvider = value; }
            public TMod EdgeMod { get => mod; set => mod = value; }
            public AStar<TNode> AStar { get => aStar; set => aStar = value; }
            public NativeList<TNode> Nodes { get => nodes; set => nodes = value; }
            public NativeList<OffsetInfo> Offsets { get => offsets; set => offsets = value; }
            public NativeList<TSeg> PathBuffer { get => pathBuffer; set => pathBuffer = value; }
            public NativeList<AStarFindPathResult> Result { get => results; set => results = value; }
            public TProc PathProcessor { get => pathProcessor; set => pathProcessor = value; }
        }
    }
}