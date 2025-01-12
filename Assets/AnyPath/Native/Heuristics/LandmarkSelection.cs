using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace AnyPath.Native.Heuristics
{
    /// <summary>
    /// <para>
    /// Contains utility functions for selecting landmarks to use with <see cref="ALT{TNode}"/> heuristics.
    /// These provide a general approach to selecting landmarks.
    /// </para>
    /// <para>
    /// If you know that your requests will often have the same start or goal locations, it may be benificial to manually place landmarks
    /// "behind" these locations. Strategic placement of landmarks can vastly improve the efficiency of the algorithm.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>All of these methods use Unity's job system internally to speed up the process.</para>
    /// <para>
    /// Because of a limitation of the burst compiler, you need to explicity provide the types to this class when calling
    /// any of the static methods it contains.
    /// See https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/compilation-generic-jobs.html for more information.
    /// </para>
    /// </remarks>
    public class LandmarkSelection<TGraph, TNode, TEnumerator>
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
        where TEnumerator : struct, IEnumerator<TNode>
    {
        /// <summary>
        /// Schedules a job that selects a set of unique, random landmarks from a graph
        /// </summary>
        /// <param name="nodeEnumerator">The node enumerator of the graph. Note that this algorithm does not validate if random
        /// nodes picked from this enumerator are reachable at all.</param>
        /// <param name="destLandmarks">Array to fill with the landmarks. The length of this array determines the amount of landmarks that will
        /// be selected. If there are less nodes in the graph than the supplied array, the array will be filled with the amount of nodes available.</param>
        /// <param name="dependsOn">Optional jobhandle that must be completed before this job gets scheduled.</param>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <typeparam name="TEnumerator">Type of node enumerator</typeparam>
        public static JobHandle ScheduleSelectRandomLandmarks(TEnumerator nodeEnumerator, NativeArray<TNode> destLandmarks, JobHandle dependsOn = default)
        {
            var job = new SelectRandomLandmarksJob()
            {
                randomSeed = (uint)DateTime.UtcNow.TimeOfDay.Ticks,
                nodeEnumerator = nodeEnumerator,
                outputLandmarks = destLandmarks
            };

            return job.Schedule(dependsOn);
        }

        /// <summary>
        /// Selects a number of random landmarks from a graph.
        /// </summary>
        /// <param name="nodeEnumerator">The node enumerator of the graph. Note that this algorithm does not validate if random
        /// nodes picked from this enumerator are reachable at all.</param>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <typeparam name="TEnumerator">Type of node enumerator</typeparam>
        /// <returns>An array containing the selected landmarks</returns>
        /// <remarks>This method is much faster to compute than the Farthest Landmarks selection, but may not produce landmarks that
        /// are placed as optimal.</remarks>
        public static void SelectRandomLandmarks(TEnumerator nodeEnumerator, TNode[] selectedLandmarks)

        {
            NativeArray<TNode> landmarks = new NativeArray<TNode>(selectedLandmarks.Length, Allocator.TempJob);
            var handle = ScheduleSelectRandomLandmarks(nodeEnumerator, landmarks);
            handle.Complete();
            landmarks.CopyTo(selectedLandmarks);
            landmarks.Dispose();
        }

        /// <summary>
        /// Schedules a job that selects a set of landmarks that are spaced apart evenly.
        /// This method is sufficient for undirected graphs and less computationally expensive than the
        /// <see cref="ScheduleSelectFarthestLandmarksDirected"/> method.
        /// </summary>
        /// <param name="graph">The graph to select landmarks for</param>
        /// <param name="nodeEnumerator">Enumerator that yields all nodes contained in the graph</param>
        /// <param name="destLandmarks">The array to fill with the selected landmarks.
        /// The amount of landmarks that will be chosen is equal to the length of this array</param>
        /// <param name="dependsOn">Optional jobhandle that must be completed before this job gets scheduled.</param>
        /// <typeparam name="TGraph">The type of graph</typeparam>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TEnumerator"></typeparam>
        /// <returns>A jobhandle for the scheduled job</returns>
        /// <remarks>
        /// This gives better results than <see cref="SelectRandomLandmarks"/>, but requires more processing time.
        /// It is recommended to enable burst compilation because this can take a long time for large graphs. Consider serializing your landmarks
        /// if you use a static world.
        /// </remarks>
        public static JobHandle ScheduleSelectFarthestLandmarksUndirected(ref TGraph graph, TEnumerator nodeEnumerator, NativeArray<TNode> destLandmarks, JobHandle dependsOn = default)
        {
            // create a dummy reversed graph, we don't use it but job needs all native containers assigned
            var revGraph = new ReversedGraph<TNode>(Allocator.TempJob);
            
            var job = new SelectFarthestLandmarksJob()
            {
                randomSeed = (uint)DateTime.UtcNow.TimeOfDay.Ticks,
                nodeEnumerator = nodeEnumerator,
                outputLandmarks = destLandmarks,
                graph = graph,
                reversedGraph = revGraph,
                directed = false
            };
            
            var handle = job.Schedule(dependsOn);
            revGraph.Dispose(handle);
            return handle;
        }
        
        /// <summary>
        /// Schedules a job that selects a set of landmarks that are spaced apart evenly.
        /// Use this method if your graph contains directed edges. This method requires more computation than
        /// <see cref="ScheduleSelectFarthestLandmarksUndirected"/>, since paths from and to every node need to be computed.
        /// </summary>
        /// <param name="graph">The graph to select landmarks for</param>
        /// <param name="nodeEnumerator">Enumerator that yields all nodes contained in the graph</param>
        /// <param name="destLandmarks">The array to fill with the selected landmarks.
        /// The amount of landmarks that will be chosen is equal to the length of this array</param>
        /// <param name="dependsOn">Optional jobhandle that must be completed before this job gets scheduled.</param>
        /// <typeparam name="TGraph">The type of graph</typeparam>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TEnumerator"></typeparam>
        /// <returns>A jobhandle for the scheduled job</returns>
        /// <remarks>
        /// This gives better results than <see cref="SelectRandomLandmarks"/>, but requires more processing time.
        /// It is recommended to enable burst compilation because this can take a long time for large graphs. Consider serializing your landmarks
        /// if you use a static world.
        /// </remarks>
        public static JobHandle ScheduleSelectFarthestLandmarksDirected(ref TGraph graph, ref ReversedGraph<TNode> reversedGraph, TEnumerator nodeEnumerator, NativeArray<TNode> destLandmarks, JobHandle dependsOn = default)
        {
            var job = new SelectFarthestLandmarksJob()
            {
                randomSeed = (uint)DateTime.UtcNow.TimeOfDay.Ticks,
                nodeEnumerator = nodeEnumerator,
                outputLandmarks = destLandmarks,
                graph = graph,
                reversedGraph = reversedGraph,
                directed = true
            };
            
            var handle = job.Schedule(dependsOn);
            return handle;
        }

        /// <summary>
        /// Runs a job on the main thread that selects a set of landmarks that are spaced apart evenly.
        /// This method is sufficient for undirected graphs and less computationally expensive than the directed version, since from and to path
        /// lengths will be equal.
        /// </summary>
        /// <param name="graph">The graph to select landmarks for</param>
        /// <param name="nodeEnumerator">Enumerator that yields all nodes contained in the graph</param>
        /// <param name="amount">The numer of landmarks to select. Note that the <see cref="ALT{TNode}"/> implementation currently supports a maximum of 32.</param>
        /// <typeparam name="TGraph">The type of graph</typeparam>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TEnumerator"></typeparam>
        /// <returns>An array containing the selected landmarks.</returns>
        /// <remarks>
        /// This gives better results than <see cref="SelectRandomLandmarks"/>, but requires more processing time.
        /// It is recommended to enable burst compilation because this can take a long time for large graphs. Consider serializing your landmarks
        /// if you use a static world.
        /// </remarks>
        public static void SelectFarthestLandmarksUndirected(ref TGraph graph, TEnumerator nodeEnumerator, TNode[] selectedLandmarks)
        {
            NativeArray<TNode> landmarks = new NativeArray<TNode>(selectedLandmarks.Length, Allocator.TempJob);
            var handle = ScheduleSelectFarthestLandmarksUndirected(ref graph, nodeEnumerator, landmarks);
            handle.Complete();
            landmarks.CopyTo(selectedLandmarks);
            landmarks.Dispose();
        }
        
        /// <summary>
        /// Runs a job on the main thread that selects a set of landmarks that are spaced apart evenly.
        /// Use this method if your graph contains directed edges. This method requires more computation than
        /// <see cref="SelectFarthestLandmarksUndirected"/>, since paths from and to every node need to be computed.
        /// </summary>
        /// <param name="graph">The graph to select landmarks for</param>
        /// <param name="reversedGraph">The reversed graph of the graph.</param>
        /// <param name="nodeEnumerator">Enumerator that yields all nodes contained in the graph</param>
        /// <param name="selectedLandmarks">
        /// Array that will be filled with the selected landmarks. The length of this array determines the amount of landmarks that will be selected.
        /// Note that the <see cref="ALT{TNode}"/> implementation currently supports a maximum of 32.</param>
        /// <typeparam name="TGraph">The type of graph</typeparam>
        /// <typeparam name="TNode"></typeparam>
        /// <typeparam name="TEnumerator"></typeparam>
        /// <returns>An array containing the selected landmarks.</returns>
        /// <remarks>
        /// This gives better results than <see cref="SelectRandomLandmarks"/>, but requires more processing time.
        /// It is recommended to enable burst compilation because this can take a long time for large graphs. Consider serializing your landmarks
        /// if you use a static world.
        /// </remarks>
        public static void SelectFarthestLandmarkDirected(ref TGraph graph, ref ReversedGraph<TNode> reversedGraph, TEnumerator nodeEnumerator, TNode[] selectedLandmarks)
        {
            NativeArray<TNode> landmarks = new NativeArray<TNode>(selectedLandmarks.Length, Allocator.TempJob);
            var handle = ScheduleSelectFarthestLandmarksDirected(ref graph, ref reversedGraph, nodeEnumerator, landmarks);
            handle.Complete();
            landmarks.CopyTo(selectedLandmarks);
            landmarks.Dispose();
        }
        
        /// <summary>
        /// Selects a set of unique random landmarks
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        private struct SelectRandomLandmarksJob : IJob
        {
            public uint randomSeed;
            public TEnumerator nodeEnumerator;
            public NativeArray<TNode> outputLandmarks;
            
            public void Execute()
            {
                CheckIfBurstCompiled();
                NativeList<TNode> nodes = new NativeList<TNode>(Allocator.Temp);
                nodeEnumerator.Reset();
                while (nodeEnumerator.MoveNext())
                    nodes.Add(nodeEnumerator.Current);

                if (nodes.Length == 0)
                    return;

                NativeHashSet<int> unique = new NativeHashSet<int>(outputLandmarks.Length, Allocator.Temp);
                
                // account for the case where there are less nodes than landmarks
                int landmarkCount = math.min(nodes.Length, outputLandmarks.Length);
                var r = new Random(randomSeed);
                while (landmarkCount > 0)
                {
                    int index = r.NextInt(0, nodes.Length - 1);
                    if (unique.Add(index))
                    {
                        landmarkCount--;
                        outputLandmarks[landmarkCount] = nodes[index];
                    }
                }
            }
            
            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled!");
#endif
            }
        }
        
        /// <summary>
        /// Selects a set of landmarks that are evenly spaced apart
        /// </summary>
        [BurstCompile(CompileSynchronously = true)]
        private struct SelectFarthestLandmarksJob : IJob
        {
            public uint randomSeed;
            public TEnumerator nodeEnumerator;
            public NativeArray<TNode> outputLandmarks;
            public bool directed;
            
            // getting:
            // InvalidOperationException: The Unity.Collections.NativeHashMap`2[Unity.Mathematics.int2,AnyPath.Native.Util.Memory`1+CameFrom[Unity.Mathematics.int2]] has been declared as [WriteOnly] in the job, but you are reading from it.
            // when allocating inside the job... bug?

            [ReadOnly] public TGraph graph;
            [ReadOnly] public ReversedGraph<TNode> reversedGraph;
            
            public void Execute()
            {
                CheckIfBurstCompiled();
                NativeList<TNode> nodes = new NativeList<TNode>(Allocator.Temp);
                NativeHashMap<TNode, float> fromLandmark = new NativeHashMap<TNode, float>(256, Allocator.Temp);
                NativeHashMap<TNode, float> toLandmark = directed ? new NativeHashMap<TNode, float>(256, Allocator.Temp) : default;
                AStar<TNode> mem1 = new AStar<TNode>(Allocator.Temp);
                AStar<TNode> mem2 = directed ? new AStar<TNode>(Allocator.Temp) : default;
                
                nodeEnumerator.Reset();
                while (nodeEnumerator.MoveNext())
                    nodes.Add(nodeEnumerator.Current);

                if (nodes.Length == 0 || outputLandmarks.Length == 0)
                    return;

                var r = new Random(randomSeed);
                // account for the case where there are less nodes than landmarks
                int landmarkCount = math.min(nodes.Length, outputLandmarks.Length);
                
                
                // pick a random first node
                TNode maxNode = nodes[r.NextInt(0, nodes.Length - 1)];
                outputLandmarks[0] = maxNode;
                
                for (int i = 1; i < landmarkCount; i++)
                {
                    float maxDist = 0;
                    
                    // add distances from prev selected landmark to total distances
                    mem1.Dijkstra(ref graph, outputLandmarks[i - 1], default(NoEdgeMod<TNode>));
                    if (directed)
                        mem2.Dijkstra(ref reversedGraph, outputLandmarks[i - 1], default(NoEdgeMod<TNode>));

                    for (int j = 0; j < nodes.Length; j++)
                    {
                        var node = nodes[j];
                        float fromMinValue;
                        float toMinValue;

                        if (mem1.cameFrom.TryGetValue(nodes[j], out var cf))
                        {
                            if (!fromLandmark.TryGetValue(node, out float prevD))
                                prevD = float.PositiveInfinity;

                            fromMinValue = math.min(cf.g, prevD);
                            fromLandmark[node] = fromMinValue;
                        }
                        else
                            fromMinValue = 0; // when not reachable, exclude

                        if (directed)
                        {
                            if (mem2.cameFrom.TryGetValue(nodes[j], out cf))
                            {
                                if (!toLandmark.TryGetValue(node, out float prevD))
                                    prevD = float.PositiveInfinity;

                                toMinValue = math.min(cf.g, prevD);
                                toLandmark[node] = toMinValue;
                            }
                            else
                                toMinValue = 0;
                        }
                        else
                            toMinValue = fromMinValue;


                        float fromToMin = math.min(fromMinValue, toMinValue);
                        if (fromToMin > maxDist)
                        {
                            maxDist = fromToMin;
                            maxNode = node;
                        }
                    }

                    outputLandmarks[i] = maxNode;
                }
            }
            
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