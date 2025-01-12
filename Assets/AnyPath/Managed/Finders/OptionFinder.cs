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
    /// Evaluates multiple targets in their added order and returns the first target that yielded a valid path.
    /// </summary>
    /// <remarks>
    /// Since the order of evaluation is preserved, you can pre sort the targets in any order of preference.
    /// For example, sorting the targets by euclidian distance will prefer targets that are closer to the starting position.
    /// This doesn't guaruantee that the target that was chosen actually has the shortest path, since the graph may contain obstructions.
    /// To find the target that has the shortest path from the start, use <see cref="CheapestOptionFinder{TGraph,TNode,TEdge,TTarget,TMod}"/>
    /// </remarks>
    public class OptionFinder<TOption, TGraph, TNode, TH, TMod, TProc, TSeg> : 
        Finder<TGraph, TNode, TH, TMod, TProc, TSeg, OptionFinder<TOption, TGraph, TNode, TH, TMod, TProc, TSeg>.JobOption, Path<TOption, TSeg>>,
        
        ISetFinderMaxRetries,
        /*
        IGetFinderOptions<TOption, TNode>,
        ISetFinderOptionValidator<TOption>,
        ISetFinderOptionReserver<TOption>,
        */
        IOptionFinder<TOption, TGraph, TNode, Path<TOption, TSeg>>,
        IRetryableFinder
        
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
        
        where TProc : struct, IPathProcessor<TNode, TSeg>
        where TSeg : unmanaged

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>

    {
        private InOptions<TOption, TNode, JobOption> inOptions;

        public OptionFinder() : this(4)
        {
        }
        
        public OptionFinder(int initialCapacity)
        {
            this.inOptions = new InOptions<TOption, TNode, JobOption>(this, initialCapacity);
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
        
        public IOptionValidator<TOption> Validator { get => inOptions.OptionValidator; set => inOptions.OptionValidator = value; }
        public IOptionReserver<TOption> Reserver { get => inOptions.OptionReserver; set => inOptions.OptionReserver = value; }
        public int MaxRetries { get => inOptions.MaxRetries; set => inOptions.MaxRetries = value; }
        public IFinderOptions<TOption, TNode> Options => inOptions;
        
        public sealed override void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
        {
            base.Clear(flags);
            inOptions.Clear(flags);
        }
        
        protected sealed override void AssignContainers(ref JobOption jobOption)
        {
            base.AssignContainers(ref jobOption);
            OutOptionPath<TSeg>.AssignContainers(ref jobOption);
            inOptions.AssignContainers(ref jobOption);
        }

        protected sealed override void ReturnContainers(ref JobOption jobOption)
        {
            base.ReturnContainers(ref jobOption); 
            OutOptionPath<TSeg>.ReturnContainers(ref jobOption);
            inOptions.ReturnContainers(ref jobOption);
        }
        
        protected sealed override void DisposeContainers(ref JobOption jobOption, JobHandle inputDeps)
        {
            base.DisposeContainers(ref jobOption, inputDeps);
            OutOptionPath<TSeg>.DisposeContainers(ref jobOption, inputDeps);
            inOptions.DisposeContainers(ref jobOption, inputDeps);
        }

        protected sealed override Path<TOption, TSeg> CreateResult(ref JobOption jobOption, out bool resultCreated) => 
            Path<TOption, TSeg>.CreateResultOption(ref jobOption, inOptions, out resultCreated);
        protected override void HydrateResult(ref Path<TOption, TSeg> result, ref JobOption job) =>
            Path<TOption, TSeg>.Hydrate(result, ref job, inOptions);

        // finderTargets evaluates if a retry run/schedule should take place and calls back into the methods below
        protected sealed override void OnCompletedInternal(bool sync) => inOptions.OnCompletedInternal(sync);
        void IRetryableFinder.OnRetryRun() => Run();
        void IRetryableFinder.OnRetrySchedule() => Schedule();
        void IRetryableFinder.OnNoRetry() => OnCompleted();

        [BurstCompile, ExcludeFromDocs]
        public struct JobOption: IJobFinder<TGraph, TNode, TH, TMod, TProc, TSeg>, IJobOption<TNode>, IJobPathBuffer<TSeg>, IJobOptionPathResult
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeList<TNode> nodes;
            [ReadOnly] public NativeList<OffsetInfo> offsets;
            [ReadOnly] public TH heuristicProvider;
            [ReadOnly] public TMod mod;
            [ReadOnly] public TProc pathProcessor;

            public AStar<TNode> aStar;
            public NativeReference<AStarFindOptionResult> result;
            public NativeList<TSeg> pathBuffer;

            public void Execute()
            {
                CheckIfBurstCompiled();
                result.Value = 
                    aStar.FindOption(
                        nodes: new NativeSlice<TNode>(nodes.AsArray()), 
                        offsets: new NativeSlice<OffsetInfo>(offsets.AsArray()), 
                        graph: ref graph, 
                        heuristicProvider: heuristicProvider, 
                        edgeMod: mod, 
                        pathProcessor: pathProcessor,
                        pathBuffer: pathBuffer);
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
            public int TargetIndex => result.Value.optionIndex;
            public NativeList<TSeg> PathBuffer { get => pathBuffer; set => pathBuffer = value; }
            public NativeReference<AStarFindOptionResult> Result { get => result; set => result = value; }
            public TProc PathProcessor { get => pathProcessor; set => pathProcessor = value; }
        }
    }
}