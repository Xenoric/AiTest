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
    public class MultiPathEvaluator<TGraph, TNode, TH, TMod> : 
        Eval<TGraph, TNode, TH, TMod, MultiPathEvaluator<TGraph, TNode, TH, TMod>.Job, MultiEvalResult>,
        
        //IGetFinderMultiRequests<TNode>,
        IMultiFinder<TGraph, TNode, MultiEvalResult> 
        
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    
    {

        private InMulti<TNode, Job> requestMulti;

        public MultiPathEvaluator() : this(4)
        {
        }
        
        public MultiPathEvaluator(int initialCapacity)
        {
            this.requestMulti = new InMulti<TNode, Job>(this, initialCapacity);
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
            OutMultiEval.AssignContainers(ref job);
            requestMulti.AssignContainers(ref job);
        }
        
        protected override void ReturnContainers(ref Job job)
        {
            base.ReturnContainers(ref job);
            OutMultiEval.ReturnContainers(ref job);
            requestMulti.ReturnContainers(ref job);
        }

        protected override void DisposeContainers(ref Job job, JobHandle inputDeps)
        {
            base.DisposeContainers(ref job, inputDeps);
            OutMultiEval.DisposeContainers(ref job, inputDeps);
            requestMulti.DisposeContainers(ref job, inputDeps);
        }

        protected override MultiEvalResult CreateResult(ref Job job, out bool resultCreated)
        {
            resultCreated = true; // Instance is always created
            return MultiEvalResult.Create(ref job);
        }

        protected override void HydrateResult(ref MultiEvalResult result, ref Job job) => MultiEvalResult.Hydrate(result, ref job);

        [BurstCompile, ExcludeFromDocs]
        public struct Job : IJobEval<TGraph, TNode, TH, TMod>, IJobMulti<TNode>, IJobMultiEvalResult
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeList<TNode> nodes;
            [ReadOnly] public NativeList<OffsetInfo> offsets;
            [ReadOnly] public TH heuristicProvider;
            [ReadOnly] public TMod mod;
        
            public AStar<TNode> aStar;
            public NativeList<AStarEvalResult> results;

            public void Execute()
            {
                CheckIfBurstCompiled();
                results.Clear();

                for (int i = 0; i < offsets.Length; i++)
                    results.Add(aStar.EvalPathStops(
                        stops: offsets[i].Slice(nodes),
                        graph: ref graph,
                        heuristicProvider: ref heuristicProvider, 
                        edgeMod: ref mod));
            }
            
            public TGraph Graph { get => graph; set => graph = value; }
            public TH HeuristicProvider { get => heuristicProvider; set => heuristicProvider = value; }
            public TMod EdgeMod { get => mod; set => mod = value; }
            public AStar<TNode> AStar { get => aStar; set => aStar = value; }
            public NativeList<TNode> Nodes { get => nodes; set => nodes = value; }
            public NativeList<OffsetInfo> Offsets { get => offsets; set => offsets = value; }
            public NativeList<AStarEvalResult> Result { get => results; set => results = value; }
            
            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled!");
#endif
            }
        }
    }
}