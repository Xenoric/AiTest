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
    /// Basic finder that evaluates the possibility of a path between two nodes on a graph.
    /// </summary>
    public class PathEvaluator<TGraph, TNode, TH, TMod> : Eval<TGraph, TNode, TH, TMod, PathEvaluator<TGraph, TNode, TH, TMod>.Job, Eval>,
        
        IGetFinderStops<TNode>
    
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    
    {
        
        private InStops<TNode, Job> stops;

        public PathEvaluator() : this(1)
        {
        }

        public PathEvaluator(int initialCapacity)
        {
            stops = new InStops<TNode, Job>(this, initialCapacity);
        }
        
        /// <summary>
        /// <para>
        /// Set to true to reuse the same instance of the result for every request.
        /// This avoids making new allocations every time a request is finished.
        /// </para>
        /// <remarks>
        /// <para>
        /// After a call to Clear, this.Result will be null. However, the result instance is still cached in the background and rehydrated
        /// after a new request. Be aware of this fact if you keep a separate reference around to the result, as its contents may change.
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
            JobEvalUtil.AssignContainers(ref job);
            stops.AssignContainers(ref job);
        }

        protected override void ReturnContainers(ref Job job)
        {
            base.ReturnContainers(ref job);
            JobEvalUtil.ReturnContainers(ref job);
            stops.ReturnContainers(ref job);
        }
        
        protected override void DisposeContainers(ref Job job, JobHandle inputDeps)
        {
            base.DisposeContainers(ref job, inputDeps);
            JobEvalUtil.DisposeContainers(ref job, inputDeps);
            stops.DisposeContainers(ref job, inputDeps);
        }

        protected override Eval CreateResult(ref Job job, out bool resultCreated)
        {
            resultCreated = true;
            return new Eval(job.result.Value);
        }

        // Just re-assign, result is a value type so it doesn't matter.
        protected override void HydrateResult(ref Eval result, ref Job job) => result = new Eval(job.result.Value);

        [BurstCompile, ExcludeFromDocs]
        public struct Job : IJobEval<TGraph, TNode, TH, TMod>, IJobStops<TNode>, IJobEvalResult
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeList<TNode> stops;
            [ReadOnly] public TH heuristicProvider;
            [ReadOnly] public TMod mod;
        
            public AStar<TNode> aStar;
            public NativeReference<AStarEvalResult> result;

            public void Execute()
            {
                result.Value = aStar.EvalPathStops(
                    stops: new NativeSlice<TNode>(stops.AsArray()), 
                    graph: ref graph, 
                    heuristicProvider: ref heuristicProvider, 
                    edgeMod: ref mod);
                
                CheckIfBurstCompiled();
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
            public NativeList<TNode> Stops { get => stops; set => stops = value; }
            public NativeReference<AStarEvalResult> Result { get => result; set => result = value; }
        }
    }
}