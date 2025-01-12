using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Native.Heuristics
{
    /// <summary>
    /// Utility class to generate <see cref="ALT{TNode}"/> heuristics.
    /// This class contains static methods that run burst accelerated code for generating the ALT heuristics.
    /// The included SquareGrid example demonstrates how to use this.
    /// </summary>
    /// <remarks>
    /// Because of a limitation of the burst compiler, you need to explicity provide the types to this class when calling
    /// any of the static methods it contains.
    /// See https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/compilation-generic-jobs.html for more information.
    /// </remarks>
    /// <typeparam name="TGraph">The type of graph to generate ALT heuristics for</typeparam>
    /// <typeparam name="TNode">The type of node, tied to the graph</typeparam>
    public class ALTCompute<TGraph, TNode>
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
    {
        
        private static void CheckLengthAndThrow(int length)
        {
            int maxLandmarks = new FixedList128Bytes<float>().Capacity;
            if (length == 0 || length > maxLandmarks)
                throw new ArgumentOutOfRangeException($"Landmarks length must be greater than zero and less than or equal to {maxLandmarks}");
        }
        
        /// <summary>
        /// Intermediate struct to allow for parallel computation of distances to and from landmarks.
        /// This can either represent a distance from a landmark to a node or a distance from a node to a landmark, which
        /// are not neccessarily equal in directed graphs.
        /// </summary>
        private readonly struct LandmarkDistance
        {
            /// <summary>
            /// The index of the landmark this distance belongs to
            /// </summary>
            public readonly int landmarkIndex;
            
            /// <summary>
            /// The (graph) distance
            /// </summary>
            public readonly float distance;
            
            /// <summary>
            /// The node that this distance belongs to
            /// </summary>
            public readonly TNode node;
            
            [ExcludeFromDocs]
            public LandmarkDistance(TNode node, int index, float distance)
            {
                this.node = node;
                this.landmarkIndex = index;
                this.distance = distance;
            }
        }
        
        /// <summary>
        /// Computes this ALT heuristic provider in parallel using Unity's Job system. This is the fastest way to compute ALT heuristics.
        /// </summary>
        /// <param name="graph"></param>
        /// <typeparam name="TGraph"></typeparam>
        /// <returns>A jobhandle that must be completed before this heuristic provider can be used</returns>
        public static JobHandle ScheduleComputeUndirected(ALT<TNode> alt, ref TGraph graph, NativeArray<TNode> landmarks, JobHandle dependsOn = default)
        {
            CheckLengthAndThrow(landmarks.Length);
            
            // using persistent always because this might take more than 4 frames
            // and the queue can potentially grow very large
            var queue = new NativeQueue<LandmarkDistance>(Allocator.Persistent);
            
            var clearJob = new ClearJob()
            {
                @from = alt.fromLandmarks,
                to = alt.toLandmarks,
                landmarks = alt.landmarks,
                
                newLandmarks = landmarks,
                isDirected = false,
                isDirectedRef = alt.isDirected
            };
          
            var enqueueJob = new EnqueueJob()
            {
                graph = graph,
                landmarks = landmarks,
                queue = queue.AsParallelWriter()
            };

            var dequeueJob = new DequeueJob()
            {
                landmarkCount = landmarks.Length,
                fromToLandmarks = alt.fromLandmarks,
                queue = queue
            };

            // fire
            var clearHandle = clearJob.Schedule(dependsOn);
            var enqueueHandle = enqueueJob.Schedule(landmarks.Length, 1, clearHandle);
            var populateHandle = dequeueJob.Schedule(enqueueHandle);
          
            queue.Dispose(populateHandle);
            return populateHandle;
        }
        
        /// <summary>
        /// Computes this ALT heuristic provider in parallel using Unity's Job system. This is the fastest way to compute ALT heuristics.
        /// </summary>
        /// <param name="graph"></param>
        /// <typeparam name="TGraph"></typeparam>
        /// <returns>A jobhandle that must be completed before this heuristic provider can be used</returns>
        public static JobHandle ScheduleComputeDirected(ALT<TNode> alt, ref TGraph graph, ref ReversedGraph<TNode> reversedGraph, NativeArray<TNode> landmarks, JobHandle dependsOn = default)
        {
            CheckLengthAndThrow(landmarks.Length);

            // using persistent always because this might take more than 4 frames
            // and the queue can potentially grow very large
            var queue1 = new NativeQueue<LandmarkDistance>(Allocator.Persistent);
            var queue2 = new NativeQueue<LandmarkDistance>(Allocator.Persistent);

            var clearJob = new ClearJob()
            {
                @from = alt.fromLandmarks,
                to = alt.toLandmarks,
                isDirected = true,
                isDirectedRef = alt.isDirected,
                newLandmarks = landmarks,
                landmarks = alt.landmarks
            };
          
            var enqueueJob1 = new EnqueueJob()
            {
                graph = graph,
                landmarks = landmarks,
                queue = queue1.AsParallelWriter()
            };
            
            var enqueueJob2 = new RevEnqueueJob()
            {
                graph = reversedGraph,
                landmarks = landmarks,
                queue = queue2.AsParallelWriter()
            };

            var dequeueJob1 = new DequeueJob()
            {
                landmarkCount = landmarks.Length,
                fromToLandmarks = alt.fromLandmarks,
                queue = queue1
            };
            
            var dequeueJob2 = new DequeueJob()
            {
                landmarkCount = landmarks.Length,
                fromToLandmarks = alt.toLandmarks,
                queue = queue2
            };

            // fire
            var clearHandle = clearJob.Schedule(dependsOn);
            var enqueueHandle1 = enqueueJob1.Schedule(landmarks.Length, 1, clearHandle);
            var enqueueHandle2 = enqueueJob2.Schedule(landmarks.Length, 1, clearHandle);

            var populateHandle1 = dequeueJob1.Schedule(enqueueHandle1);
            var populateHandle2 = dequeueJob2.Schedule(enqueueHandle2);

            queue1.Dispose(populateHandle1);
            queue2.Dispose(populateHandle2);
            
            return JobHandle.CombineDependencies(populateHandle1, populateHandle2);
        }

        /// <summary>
        /// Computes ALT heuristics for a set of landmarks. Use this version if your graph contains directed edges.
        /// </summary>
        /// <param name="graph">The graph to compute ALt heuristics for</param>
        /// <param name="reversedGraph">The reversed version of the graph. <see cref="ReversedGraph{TNode}"/>.</param>
        /// <param name="landmarks">Array containing the landmarks. Ideally, landmarks should be placed "behind" frequent
        /// starting and goal locations in the graph. Currently a maximum of 31 landmarks is supported.</param>
        /// <typeparam name="TGraph"></typeparam>
        /// <remarks>The computation can be resource intensive for large graphs. This operation is done in parallel and using
        /// Unity's Burst compiler to maximize performance.</remarks>
        public static void ComputeDirected(ALT<TNode> alt, ref TGraph graph, ref ReversedGraph<TNode> reversedGraph, TNode[] landmarks)
        {
            var arr = new NativeArray<TNode>(landmarks, Allocator.TempJob);
            var handle = ScheduleComputeDirected(alt, ref graph, ref reversedGraph, arr);
            arr.Dispose(handle);
            handle.Complete();
        }
        
        public static void ComputeUndirected(ALT<TNode> alt, ref TGraph graph, TNode[] landmarks)
        {
            var arr = new NativeArray<TNode>(landmarks, Allocator.TempJob);
            var handle = ScheduleComputeUndirected(alt, ref graph, arr);
            arr.Dispose(handle);
            handle.Complete();
        }

 
        
        /// <summary>
        /// Dequeues source into the hashmap that contains either all distances to or from each landmark.
        /// </summary>
        private static void DequeueFromTo(NativeQueue<LandmarkDistance> sourceQueue, int landmarkCount, NativeHashMap<TNode, FixedList128Bytes<float>> destFromOrToLandmarks)
        {
            while (sourceQueue.TryDequeue(out var entry))
            {
                if (!destFromOrToLandmarks.TryGetValue(entry.node, out var list))
                {
                    list.Length = landmarkCount;
                    for (int i = 0; i < landmarkCount; i++)
                        list[i] = float.PositiveInfinity;
                }
                    
                // this could potentially be faster if we had a direct pointer to the memory location of the index
                // copying a 128 byte struct now each time...
                
                list[entry.landmarkIndex] = entry.distance; // set correct cost for this landmark at it's index
                destFromOrToLandmarks[entry.node] = list; // reassign
            }
        }

        
        /// <summary>
        /// Computes all distances from a landmark to every node in the graph and enqueues them at the destination queue.
        /// </summary>
        /// <remarks>For undirected graphs, this method using the original graph is sufficient.
        /// For directed graphs however, a <see cref="ReversedGraph{TNode}"/> has te be created in order to find the distances
        /// from every node to the landmark.</remarks>
        private static void ComputeDistancesFromLandmark(ref TGraph graph, NativeArray<TNode> landmarks, int landmarkIndex,
            ref AStar<TNode> aStar, ref NativeQueue<LandmarkDistance>.ParallelWriter writer)
        {
            var landmark = landmarks[landmarkIndex];
            aStar.Dijkstra(ref graph, landmark, default(NoEdgeMod<TNode>));
            using var enumerator = aStar.cameFrom.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var kv = enumerator.Current;
                writer.Enqueue(new LandmarkDistance(kv.Key, landmarkIndex, kv.Value.g));
            }
        }
        
        private static void RevComputeDistancesFromLandmark(ref ReversedGraph<TNode> graph, NativeArray<TNode> landmarks, int landmarkIndex,
            ref AStar<TNode> aStar, ref NativeQueue<LandmarkDistance>.ParallelWriter writer)
        {
            var landmark = landmarks[landmarkIndex];
            aStar.Dijkstra(ref graph, landmark, default(NoEdgeMod<TNode>));
            using var enumerator = aStar.cameFrom.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var kv = enumerator.Current;
                writer.Enqueue(new LandmarkDistance(kv.Key, landmarkIndex, kv.Value.g));
            }
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private struct EnqueueJob : IJobParallelFor
        {
            [ReadOnly] public TGraph graph;
            [ReadOnly] public NativeArray<TNode> landmarks;
            public NativeQueue<LandmarkDistance>.ParallelWriter queue;
            
            public void Execute(int index)
            {
                CheckIfBurstCompiled();
                var memory = new AStar<TNode>(Allocator.Temp);
                ComputeDistancesFromLandmark(ref graph, landmarks, index, ref memory, ref queue);
            }
            
            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled!");
#endif
            }
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private struct RevEnqueueJob : IJobParallelFor
        {
            [ReadOnly] public ReversedGraph<TNode> graph;
            [ReadOnly] public NativeArray<TNode> landmarks;
            public NativeQueue<LandmarkDistance>.ParallelWriter queue;
            
            public void Execute(int index)
            {
                CheckIfBurstCompiled();
                var memory = new AStar<TNode>(Allocator.Temp);
                RevComputeDistancesFromLandmark(ref graph, landmarks, index, ref memory, ref queue);
            }
            
            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled!");
#endif
            }
        }
        
        [BurstCompile]
        private struct ClearJob : IJob
        {
            public bool isDirected;
            [ReadOnly] public NativeArray<TNode> newLandmarks;
            
            public NativeHashMap<TNode, FixedList128Bytes<float>> from;
            public NativeHashMap<TNode, FixedList128Bytes<float>> to;
            public NativeReference<bool> isDirectedRef;
            public NativeList<TNode> landmarks;

            public void Execute()
            {
                CheckIfBurstCompiled();
                @from.Clear();
                to.Clear();
                landmarks.Clear();
                isDirectedRef.Value = isDirected;
                
                landmarks.AddRange(newLandmarks);
            }
            
            [BurstDiscard]
            void CheckIfBurstCompiled()
            {
#if !UNITY_EDITOR
                throw new System.Exception("Job is not burst compiled!");
#endif
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        private struct DequeueJob : IJob
        {
            public int landmarkCount;
            public NativeQueue<LandmarkDistance> queue;
            public NativeHashMap<TNode, FixedList128Bytes<float>> fromToLandmarks;
            
            public void Execute()
            {
                CheckIfBurstCompiled();
                DequeueFromTo(queue, landmarkCount, fromToLandmarks);
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