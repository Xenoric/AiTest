using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// A reversed edge representation of a graph. Used by the ALT heuristic provider for directed graphs.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    public struct ReversedGraph<TNode> : IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
    {
        [ExcludeFromDocs]
        public struct ReversedEdge
        {
            public TNode prev;
            public float cost;
        }

        private NativeParallelMultiHashMap<TNode, ReversedEdge> edges;

        /// <summary>
        /// Computes the reversed graph of a source graph. That is, all the edges will be reversed. This is useful for directed graphs
        /// where you want to run Dijkstra's algorithm to know the shortest path from every node in the graph *to* a certain location.
        /// The ALT heuristic for directed graph needs this information for preprocessing.
        /// </summary>
        /// <param name="source">The source graph</param>
        /// <param name="nodeEnumerator">Enumerator that yields all nodes in the graph</param>
        /// <param name="allocator">Allocator for the reversed graph</param>
        /// <typeparam name="TGraph">Type of graph to construct a reverse graph for</typeparam>
        /// <typeparam name="TEnumerator">The type of enumerator, this can be a struct enumerator used on Unity's native collections</typeparam>
        /// <returns>A reversed edge representation of the source graph</returns>
        /// <remarks>This method is not burst compiled. If your source graph is large, it might be benificial to pre-allocate the
        /// ReversedGraph structure and populate it using <see cref="ScheduleCompute{TGraph,TEnumerator}"/> or <see cref="Compute{TGraph,TEnumerator}(TGraph,TEnumerator,Unity.Collections.Allocator)"/></remarks>
        public static ReversedGraph<TNode> Compute<TGraph, TEnumerator>(TGraph source, TEnumerator nodeEnumerator, Allocator allocator)
            where TGraph : struct, IGraph<TNode>
            where TEnumerator : struct, IEnumerator<TNode>
        {
            
            var edgeBuffer = new NativeList<Edge<TNode>>(Allocator.Temp);
            var edges = new NativeParallelMultiHashMap<TNode, ReversedEdge>(128, allocator);
         
            nodeEnumerator.Reset();
            while (nodeEnumerator.MoveNext())
            {
                var node = nodeEnumerator.Current;
                
                edgeBuffer.Clear();
                source.Collect(node, ref edgeBuffer);
                foreach (var edge in edgeBuffer)
                {
                    edges.Add(edge.Next, new ReversedEdge()
                    {
                        prev = node,
                        cost = edge.Cost
                    });
                }
            }
            
            edgeBuffer.Dispose();

            var graph = new ReversedGraph<TNode>(edges);
            return graph;
        }
        
        /// <summary>
        /// Schedules a job that computes the reversed graph of a source graph. That is, all the edges will be reversed. This is useful for directed graphs
        /// where you want to run Dijkstra's algorithm to know the shortest path from every node in the graph *to* a certain location.
        /// The ALT heuristic for directed graph needs this information for preprocessing.
        /// </summary>
        /// <param name="source">The source graph</param>
        /// <param name="nodeEnumerator">Enumerator that yields all nodes in the graph</param>
        /// <typeparam name="TGraph">Type of graph to construct a reverse graph for</typeparam>
        /// <typeparam name="TEnumerator">The type of enumerator, this can be a struct enumerator used on Unity's native collections</typeparam>
        /// <returns></returns>
        public JobHandle ScheduleCompute<TGraph, TEnumerator>(TGraph source, TEnumerator nodeEnumerator, JobHandle dependsOn = default)
            where TGraph : struct, IGraph<TNode>
            where TEnumerator : struct, IEnumerator<TNode>
        {
            var job = new ComputeJob<TGraph, TEnumerator>()
            {
                edges = this.edges,
                source = source,
                nodeEnumerator = nodeEnumerator
            };

            return job.Schedule(dependsOn);
        }
        
        /// <summary>
        /// Computes the reversed graph of a source graph immediately
        /// </summary>
        /// <param name="source">The source graph</param>
        /// <param name="nodeEnumerator">Enumerator that yields all nodes in the graph</param>
        /// <typeparam name="TGraph">Type of graph to construct a reverse graph for</typeparam>
        /// <typeparam name="TEnumerator">The type of enumerator, this can be a struct enumerator used on Unity's native collections</typeparam>
        public void Compute<TGraph, TEnumerator>(TGraph source, TEnumerator nodeEnumerator)
            where TGraph : struct, IGraph<TNode>
            where TEnumerator : struct, IEnumerator<TNode>
        {
            new ComputeJob<TGraph, TEnumerator>()
            {
                edges = this.edges,
                source = source,
                nodeEnumerator = nodeEnumerator
            }.Run();
        }

        private struct ComputeJob<TGraph, TEnumerator> : IJob
            where TGraph : struct, IGraph<TNode>
            where TEnumerator : struct, IEnumerator<TNode>
        {
            [ReadOnly] public TGraph source;
            [ReadOnly] public TEnumerator nodeEnumerator;
            
            public NativeParallelMultiHashMap<TNode, ReversedEdge> edges;

            public void Execute()
            {
                edges.Clear();
                var edgeBuffer = new NativeList<Edge<TNode>>(Allocator.Temp);
           
                nodeEnumerator.Reset();
                while (nodeEnumerator.MoveNext())
                {
                    var node = nodeEnumerator.Current;
                
                    edgeBuffer.Clear();
                    source.Collect(node, ref edgeBuffer);
                    foreach (var edge in edgeBuffer)
                    {
                        edges.Add(edge.Next, new ReversedEdge()
                        {
                            prev = node,
                            cost = edge.Cost
                        });
                    }
                }
            
                edgeBuffer.Dispose();
            }
        }

        private ReversedGraph(NativeParallelMultiHashMap<TNode, ReversedEdge> map)
        {
            this.edges = map;
        }

        /// <summary>
        /// Construct a container for the reversed graph. Which can then be populated using <see cref="ScheduleCompute{TGraph,TEnumerator}"/>
        /// or <see cref="Compute{TGraph,TEnumerator}(TGraph,TEnumerator,Unity.Collections.Allocator)"/>.
        /// </summary>
        /// <param name="allocator"></param>
        public ReversedGraph(Allocator allocator)
        {
            this.edges = new NativeParallelMultiHashMap<TNode, ReversedEdge>(32, allocator);
        }

        [ExcludeFromDocs]
        public void Collect(TNode node, ref NativeList<Edge<TNode>> edgeBuffer)
        {
            if (edges.TryGetFirstValue(node, out var prev, out var it))
            {
                do
                {
                    edgeBuffer.Add(new Edge<TNode>(prev.prev, prev.cost));
                } while (edges.TryGetNextValue(out prev, ref it));
            }
        }
        
        [ExcludeFromDocs]
        public void Dispose()
        {
            edges.Dispose();
        }

        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return edges.Dispose(inputDeps);
        }
    }
}