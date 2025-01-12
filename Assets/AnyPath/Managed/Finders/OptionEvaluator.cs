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
    /// Similar to <see cref="OptionFinder{TGraph,TNode,TEdge,TTarget,TMod}"/> but only evaluates the possibility of a path.
    /// </summary>
    public class OptionEvaluator<TOption, TGraph, TNode, TH, TMod> : Eval<TGraph, TNode, TH, TMod, 
            OptionEvaluator<TOption, TGraph, TNode, TH, TMod>.JobOption, Eval<TOption>>,
    
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

        public OptionEvaluator() : this(4)
        {
        }
        
        public OptionEvaluator(int initialCapacity)
        {
            this.inOptions = new InOptions<TOption, TNode, JobOption>(this, initialCapacity);
        }
        
        public IFinderOptions<TOption, TNode> Options => inOptions;
        public int MaxRetries { get => inOptions.MaxRetries; set => inOptions.MaxRetries = value; }
        public IOptionValidator<TOption> Validator { get => inOptions.OptionValidator; set => inOptions.OptionValidator = value; }
        public IOptionReserver<TOption> Reserver { get => inOptions.OptionReserver; set => inOptions.OptionReserver = value; }

        public override void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
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

        protected sealed override Eval<TOption> CreateResult(ref JobOption jobOption, out bool resultCreated)
        {
            return Eval<TOption>.CreateResultOption(ref jobOption, inOptions, out resultCreated);
        }

        protected override void HydrateResult(ref Eval<TOption> result, ref JobOption job) =>
            result = Eval<TOption>.CreateResultOption(ref job, inOptions, out _);

        // finderTargets evaluates if a retry run/schedule should take place and calls back into the methods below
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
        
            public AStar<TNode> aStar;
            public NativeReference<AStarEvalOptionResult> result;

            public void Execute()
            {
                CheckIfBurstCompiled();
                result.Value = aStar.Evaloption( 
                    nodes: new NativeSlice<TNode>(nodes.AsArray()), 
                    offsets: new NativeSlice<OffsetInfo>(offsets.AsArray()), 
                    graph: ref graph, 
                    heuristicProvider: heuristicProvider, 
                    edgeMod: mod);
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
            public int TargetIndex => result.Value.targetIndex;
            public NativeReference<AStarEvalOptionResult> Result { get => result; set => result = value; }
        }
    }
}