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
    /// Similar to the <see cref="PathFinder{TGraph,TNode,TH,TMod,TProc,TSeg}"/>, but stores extra information about which nodes the A* algorithm
    /// expanded into. This can be useful for debugging your pathfinding and testing how optimized your heuristic is. 
    /// </summary>
    /// <remarks>Do not use this finder in production, as it is much slower then the regular PathFinder</remarks>
    public class DebugFinder<TGraph, TNode, TH, TMod, TProc, TSeg> :
        Finder<TGraph, TNode, TH, TMod, TProc, TSeg, DebugFinder<TGraph, TNode, TH, TMod, TProc, TSeg>.Job, DebugPath<TNode, TSeg>>,
        
        IGetFinderStops<TNode>
    
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>

        where TH : struct, IHeuristicProvider<TNode>
        where TMod : struct, IEdgeMod<TNode>
    
        where TProc : struct, IPathProcessor<TNode, TSeg>
        where TSeg : unmanaged

    {
        private InStops<TNode, Job> stops;

        public DebugFinder() : this(1)
        {
        }

        public DebugFinder(int initialCapacity)
        {
            stops = new InStops<TNode, Job>(this, initialCapacity);
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
            
            // not pooling this
            job.allExpanded = new NativeList<AStar<TNode>.CameFrom>(Allocator.Persistent);
        }
        
        protected override void ReturnContainers(ref Job job)
        {
            base.ReturnContainers(ref job);
            OutPath<TSeg>.ReturnContainers(ref job);
            stops.ReturnContainers(ref job);
            
            job.allExpanded.Dispose();
        }
        
        protected override void DisposeContainers(ref Job job, JobHandle inputDeps)
        {
            base.DisposeContainers(ref job, inputDeps);
            OutPath<TSeg>.DisposeContainers(ref job, inputDeps);
            stops.DisposeContainers(ref job, inputDeps);
            
            job.allExpanded.Dispose(inputDeps);
        }

        protected override DebugPath<TNode, TSeg> CreateResult(ref Job job, out bool resultCreated)
        {
            resultCreated = true; // doesn't matter here as its not a setting for debug finder
            return new DebugPath<TNode, TSeg>(job.result.Value, job.path, ref job.aStar);
        }

        protected override void HydrateResult(ref DebugPath<TNode, TSeg> result, ref Job job) =>
            result = new DebugPath<TNode, TSeg>(job.result.Value, job.path, ref job.aStar);

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
            public NativeList<AStar<TNode>.CameFrom> allExpanded;

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

                allExpanded.Clear();
                var tmp = aStar.DebugGetAllExpansion(Allocator.Temp);
                for (int i = 0; i < tmp.Length; i++)
                {
                    var exp = tmp.Values[i];
                    allExpanded.Add(exp);
                }
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
            public NativeList<TSeg> PathBuffer { get => path; set => path = value; }
            public NativeReference<AStarFindPathResult> Result { get => result; set => result = value; }
            public TProc PathProcessor { get => pathProcessor; set => pathProcessor = value; }
        }
    }
}