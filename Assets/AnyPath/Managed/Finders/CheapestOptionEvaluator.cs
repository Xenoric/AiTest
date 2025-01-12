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
    /// Similar to <see cref="CheapestOptionFinder{TGraph,TNode,TEdge,TTarget,TMod}"/> but only evaluates the possibility of a path.
    /// </summary>
    /// <inheritdoc cref="Finder{TGraph,TNode,TEdge,TMod,TJob,TResult}"/>
    public class CheapestOptionEvaluator<TOption, TGraph, TNode, TH, TMod> : 
        Eval<TGraph, TNode, TH, TMod, CheapestOptionEvaluator<TOption, TGraph, TNode, TH, TMod>.JobOption, Eval<TOption>>,
    
        ISetFinderMaxRetries,
        /*
        IGetFinderOptions<TOption, TNode>, 
        ISetFinderOptionValidator<TOption>, 
        ISetFinderOptionReserver<TOption>,
        */
        IOptionFinder<TOption, TGraph, TNode, Eval<TOption>>,
        IRetryableFinder

        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    {
        
        private InOptions<TOption, TNode, JobOption> inOptions;

        public CheapestOptionEvaluator() : this(4)
        {
        }
        
        public CheapestOptionEvaluator(int initialCapacity)
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
            OutOptionEval.AssignContainersEval(ref jobOption);
            inOptions.AssignContainers(ref jobOption);
        }
        
        protected sealed override void ReturnContainers(ref JobOption jobOption)
        {
            base.ReturnContainers(ref jobOption);
            OutOptionEval.ReturnContainersEval(ref jobOption);
            inOptions.ReturnContainers(ref jobOption);
        }
        
        protected sealed override void DisposeContainers(ref JobOption jobOption, JobHandle inputDeps)
        {
            base.DisposeContainers(ref jobOption, inputDeps);
            OutOptionEval.DisposeContainersEval(ref jobOption, inputDeps);
            inOptions.DisposeContainers(ref jobOption, inputDeps);
        }

        protected sealed override Eval<TOption> CreateResult(ref JobOption jobOption, out bool cachedResultCreated)
        {
            return Eval<TOption>.CreateResultOption(ref jobOption, inOptions, out cachedResultCreated);
        }

        protected override void HydrateResult(ref Eval<TOption> result, ref JobOption job) =>
            result = Eval<TOption>.CreateResultOption(ref job, inOptions, out _);

        protected sealed override void OnCompletedInternal(bool sync) => inOptions.OnCompletedInternal(sync);
        void IRetryableFinder.OnRetryRun() => Run();
        void IRetryableFinder.OnRetrySchedule() => Schedule();
        void IRetryableFinder.OnNoRetry() => OnCompleted();
        
        [BurstCompile, ExcludeFromDocs]
        public struct JobOption: IJobEval<TGraph, TNode, TH, TMod>, IJobOption<TNode>, IJobOptionEvalResult
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeList<TNode> nodes;
            [ReadOnly] public NativeList<OffsetInfo> offsets;
            [ReadOnly] public TH heuristicProvider;
            [ReadOnly] public TMod mod;
        
            // no memory backbuffer is needed here since we never reconstruct a path
            public AStar<TNode> aStar;
            public NativeReference<AStarEvalOptionResult> result;

            public void Execute()
            {
                CheckIfBurstCompiled();
                result.Value = aStar.EvalCheapestTarget(
                    nodes: new NativeSlice<TNode>(nodes.AsArray()), 
                    offsets: new NativeSlice<OffsetInfo>(offsets.AsArray()),
                    graph: ref graph,
                    heuristicProvider: ref heuristicProvider, 
                    edgeMod: ref mod);
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
            public int TargetIndex => result.Value.targetIndex;
            public NativeList<TNode> Nodes { get => nodes; set => nodes = value; }
            public NativeList<OffsetInfo> Offsets { get => offsets; set => offsets = value; }
            public NativeReference<AStarEvalOptionResult> Result { get => result; set => result = value; }
        }
    }
}