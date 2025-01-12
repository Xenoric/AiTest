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
    /// Evaluates multiple targets in their added order and returns the target that is reachable and has the path with the lowest cost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For optimization reasons, this finder will only return a correct result if the graph implementation has an admissable heuristic function.
    /// That is to say, the heuristic never overestimates the cost between two nodes.
    /// </para>
    /// <para>
    /// Consider pre-sorting the targets by their distance from the start. Altough not required, this potentially excludes
    /// certain targets without having to run the algorithm.
    /// </para>
    /// </remarks>
    public class CheapestOptionFinder<TOption, TGraph, TNode, TH, TMod, TProc, TSeg> : Finder<TGraph, TNode, TH, TMod, TProc, TSeg,
            CheapestOptionFinder<TOption, TGraph, TNode, TH, TMod, TProc, TSeg>.JobOption, Path<TOption, TSeg>>,
        
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

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    
        where TProc : struct, IPathProcessor<TNode, TSeg>
        where TSeg : unmanaged

    {
        
        private InOptions<TOption, TNode, JobOption> inOptions;

        public CheapestOptionFinder() : this(4)
        {
        }

        public CheapestOptionFinder(int initialCapacity)
        {
            this.inOptions = new InOptions<TOption, TNode, JobOption>(this, initialCapacity);
        }
        
        /// <summary>
        /// <para>
        /// Set to true to reuse the same instance of the result for every request.
        /// This avoids making new allocations every time a request is finished.
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

        public IFinderOptions<TOption, TNode> Options => inOptions;
        public int MaxRetries { get => inOptions.MaxRetries; set => inOptions.MaxRetries = value; }
        public IOptionValidator<TOption> Validator { get => inOptions.OptionValidator; set => inOptions.OptionValidator = value; }
        public IOptionReserver<TOption> Reserver { get => inOptions.OptionReserver; set => inOptions.OptionReserver = value; }

        public sealed override void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
        {
            base.Clear(flags);
            inOptions.Clear(flags);
        }

        protected sealed override void AssignContainers(ref JobOption jobOption)
        {
            base.AssignContainers(ref jobOption);
            OutCheapestOption<TSeg>.AssignContainers(ref jobOption);
            inOptions.AssignContainers(ref jobOption);
        }
        
        protected sealed override void ReturnContainers(ref JobOption jobOption)
        {
            base.ReturnContainers(ref jobOption); 
            OutCheapestOption<TSeg>.ReturnContainers(ref jobOption);
            inOptions.ReturnContainers(ref jobOption);
        }
        
        protected sealed override void DisposeContainers(ref JobOption jobOption, JobHandle inputDeps)
        {
            base.DisposeContainers(ref jobOption, inputDeps);
            OutCheapestOption<TSeg>.DisposeContainers(ref jobOption, inputDeps);
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
        public struct JobOption: IJobFinder<TGraph, TNode, TH, TMod, TProc, TSeg>, IJobOption<TNode>, IJobPathBuffer<TSeg>, IJobPathBufferCheapest<TSeg>, IJobOptionPathResult
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeList<TNode> nodes;
            [ReadOnly] public NativeList<OffsetInfo> offsets;
            [ReadOnly] public TH heuristicProvider;
            [ReadOnly] public TMod mod;
            [ReadOnly] public TProc pathProcessor;

            public AStar<TNode> aStar;
            public NativeReference<AStarFindOptionResult> result;
            public NativeList<TSeg> path;
            public NativeList<TSeg> temp1;
            public NativeList<TSeg> temp2;
            
            public void Execute()
            {
                CheckIfBurstCompiled();
                result.Value = 
                    aStar.FindCheapestOption(
                        nodes: new NativeSlice<TNode>(nodes.AsArray()), 
                        offsets: new NativeSlice<OffsetInfo>(offsets.AsArray()), 
                        graph: ref graph, 
                        heuristicProvider: heuristicProvider, 
                        edgeMod: mod, 
                        tempBuffer1: temp1, 
                        tempBuffer2: temp2, 
                        pathProcessor: pathProcessor,
                        pathBuffer: path);
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
            public NativeList<TSeg> PathBuffer { get => path; set => path = value; }
            public NativeReference<AStarFindOptionResult> Result { get => result; set => result = value; }
            public NativeList<TSeg> TempBuffer1 { get => temp1; set => temp1 = value; }
            public NativeList<TSeg> TempBuffer2 { get => temp2; set => temp2 = value; }
            public TProc PathProcessor { get => pathProcessor; set => pathProcessor = value; }
        }
    }
}