using System;
using AnyPath.Managed.Finders.Common;
using AnyPath.Managed.Results;
using AnyPath.Native;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders
{
    /// <summary>
    /// Performs Dijkstra's algorithm on a graph.
    /// This can be used to obtain the shortest paths from a starting location to every other location in the graph (with a given maximum cost limit)
    /// </summary>
    /// <typeparam name="TGraph">The type of graph this finder operates on</typeparam>
    /// <typeparam name="TNode">The node type associated with the type of graph</typeparam>
    /// <typeparam name="TMod">Type of edge modifier</typeparam>
    public class DijkstraFinder<TGraph, TNode, TMod> : ManagedGraphJobWrapper<TGraph, TNode, DijkstraFinder<TGraph, TNode, TMod>.Job, DijkstraResult<TNode>>
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
        where TMod : struct, IEdgeMod<TNode>
    {
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
        
        /// <summary>
        /// The starting node for the Dijkstra algorithm
        /// </summary>
        public TNode Start { get; set; }

        /// <summary>
        /// Cost budget for expanding.
        /// The algorithm will not traverse further if the cost exceeds this value. Nodes beyond this cost will be considered as not reachable
        /// </summary>
        /// <remarks>If your graph has no boundary (e.g. an infinite grid). You *must* provide a value that limits how for Dijkstra will expand. Otherwise
        /// the algorithm will run forever. All of the included graphs with AnyPath have a set boundary, so if you're using any of these you don't have to worry about this.
        /// It will however still benefit performance to set a maximum cost.</remarks>
        public float MaxCost { get; set; } = float.PositiveInfinity;
        
        /// <summary>
        /// Optional edge modifier to use with the request.
        /// </summary>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        public TMod EdgeMod
        {
            get => job.EdgeMod;
            set
            {
                if (!IsMutable) throw new ImmutableFinderException();
                job.EdgeMod = value;
            }
        }
        
        protected override void AssignContainers(ref Job job)
        {
            base.AssignContainers(ref job);
            
            job.start = Start;
            job.maxCost = MaxCost;
        }

        protected override DijkstraResult<TNode> CreateResult(ref Job job, out bool resultCreated)
        {
            resultCreated = true; // Dijkstra always returns an instance
            return DijkstraResult<TNode>.CreateOrHydrate(null, job.start, job.maxCost, job.aStar);
        }

        protected override void HydrateResult(ref DijkstraResult<TNode> result, ref Job job) =>
            DijkstraResult<TNode>.CreateOrHydrate(result, job.start, job.maxCost, job.aStar);
       
        [BurstCompile, ExcludeFromDocs]
        public struct Job : IJobGraphAStar<TGraph, TNode>
        {
            [ReadOnly] public TGraph graph;

            public TMod mod;
            public TNode start;
            public float maxCost;
            public AStar<TNode> aStar;

            public void Execute()
            {
                CheckIfBurstCompiled();
                aStar.Dijkstra(ref graph, start, mod, maxCost);
            }
            
            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled!");
#endif
            }
            
            public TGraph Graph { get => graph; set => graph = value; }
            public AStar<TNode> AStar { get => aStar; set => aStar = value; }
            public TMod EdgeMod { get => mod; set => mod = value; }
        }
    }
}