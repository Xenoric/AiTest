using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Native.Heuristics
{
    /// <summary>
    /// <para>
    /// ALT heuristic provider that works on any type of graph. This can significantly speed up A* queries on large and complex graphs.
    /// Based on this research:
    /// https://www.microsoft.com/en-us/research/publication/computing-the-shortest-path-a-search-meets-graph-theory/?from=http%3A%2F%2Fresearch.microsoft.com%2Fpubs%2F154937%2Fsoda05.pdf
    /// </para>
    /// <para>
    /// To generate ALT heuristics, you must use <see cref="ALTCompute{TGraph,TNode}"/> and optionally <see cref="LandmarkSelection{TGraph,TNode,TEnumerator}"/>.
    /// The included Square grid example demonstrates this process.
    /// </para>
    /// </summary>
    /// <remarks>For best results, landmarks should be placed "behind" frequent start and goal locations.</remarks>
    public struct ALT<TNode> : IHeuristicProvider<TNode>, INativeDisposable
        where TNode : unmanaged, IEquatable<TNode>
    {
        internal NativeHashMap<TNode, FixedList128Bytes<float>> fromLandmarks;
        internal NativeHashMap<TNode, FixedList128Bytes<float>> toLandmarks;
        internal NativeList<TNode> landmarks;
        internal NativeReference<bool> isDirected;

        /// <summary>
        /// Allocates an ALT heuristic provider. 
        /// </summary>
        /// <param name="allocator">Allocator for the ALT heuristics.</param>
        public ALT(Allocator allocator)
        {
            this.fromLandmarks = new NativeHashMap<TNode, FixedList128Bytes<float>>(4, allocator);
            
            // we always allocate the to landmarks with zero capacity, even though it's not used for undirected graphs.
            // this makes constructing this struct a lot easier and allows for the ALT struct to be re-used between directed and undirected graphs.
            // also, we don't know upon construction if we're going to be used for a directed or undirected graph.
            this.toLandmarks = new NativeHashMap<TNode, FixedList128Bytes<float>>(0, allocator);
            this.landmarks = new NativeList<TNode>(allocator);
            this.isDirected = new NativeReference<bool>(allocator);
            
            // the will be initialized as soon as A* begins
            this.fromLandmarksT = default;
            this.toLandmarksT = default;
            this.t = default;
        }

        
        private FixedList128Bytes<float> fromLandmarksT;
        private FixedList128Bytes<float> toLandmarksT;
        private TNode t; // our current goal

        public void SetGoal(TNode goal)
        {
            // This gets called before A* begins
            // we cache the from/to landmarks for this goal as this saves us 2 hashmap lookups per node
            // since the goal will always be the same.
            // this severily increases the performance, as the heuristc function is called many many times
            fromLandmarks.TryGetValue(goal, out this.fromLandmarksT);
            toLandmarks.TryGetValue(goal, out this.toLandmarksT);
            this.t = goal;
        }
        
        /// <summary>
        /// Returns a cost estimate of a path between two nodes. Depending on the location of the landmarks, this estimate can
        /// be significantly better than a traditional heuristic, resulting in much less expanded nodes and thus faster pathfinding.
        /// </summary>
        /// <remarks>In order for the algorithm to work correctly, the edge cost's may not be negative.</remarks>
        public float Heuristic(TNode u)
        {
            if (u.Equals(t))
                return 0;

            if (!fromLandmarks.TryGetValue(u, out var fromLandmarksU)) return 0;

            float maxEstimate = 0;
            if (isDirected.Value)
            {
                // Directed
                if (!toLandmarks.TryGetValue(u, out var toLandmarksU)) return 0;

                for (int i = 0; i < fromLandmarksU.Length; i++)
                {
                    float fromU = fromLandmarksU[i];
                    float fromT = fromLandmarksT[i];
                    float toU = toLandmarksU[i];
                    float toT = toLandmarksT[i];
                    maxEstimate = max(max(toU - toT, fromT - fromU), maxEstimate);
                }
            }
            else
            {
                // Undirected
                for (int i = 0; i < fromLandmarksU.Length; i++)
                {
                    float fromU = fromLandmarksU[i];
                    float fromT = fromLandmarksT[i];
                    maxEstimate = max(abs(fromU - fromT), maxEstimate);
                }
            }

            return maxEstimate;
        }

        [ExcludeFromDocs]
        public void Dispose()
        {
            landmarks.Dispose();
            fromLandmarks.Dispose();
            toLandmarks.Dispose();
            isDirected.Dispose();
        }

        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps)
        {
            NativeArray<JobHandle> tmp = new NativeArray<JobHandle>(4, Allocator.Temp);
            tmp[0] = isDirected.Dispose(inputDeps);
            tmp[1] = landmarks.Dispose(inputDeps);
            tmp[2] = fromLandmarks.Dispose(inputDeps);
            tmp[3] = toLandmarks.Dispose(inputDeps);
            var handle = JobHandle.CombineDependencies(tmp);
            tmp.Dispose();
            return handle;
        }
        
        /// <summary>
        /// Returns the internal native containers which can be used to serialize the data.
        /// </summary>
        public void GetNativeContainers(
            out NativeHashMap<TNode, FixedList128Bytes<float>> fromLandmarks,
            out NativeHashMap<TNode, FixedList128Bytes<float>> toLandmarks,
            out NativeList<TNode> landmarks,
            out NativeReference<bool> isDirected)
        {
            fromLandmarks = this.fromLandmarks;
            toLandmarks = this.toLandmarks;
            landmarks = this.landmarks;
            isDirected = this.isDirected;
        }



        /// <summary>
        /// Returns wether this ALT heuristic provider was made for a directed graph.
        /// </summary>
        public bool IsDirected => isDirected.Value;
        
        /// <summary>
        /// Returns the amount of landmarks in this provider.
        /// </summary>
        public int LandmarkCount => landmarks.Length;
        
        /// <summary>
        /// Returns the location of the landmark at a given index.
        /// </summary>
        /// <param name="index">The index</param>
        /// <returns>The landmark location</returns>
        public TNode GetLandmarkLocation(int index) => landmarks[index];
        
        /// <summary>
        /// Returns key value arrays containing the graph distances to every landmark. This can be useful if you want to serialize the data.
        /// For undirected graphs, this data will be the same as <see cref="GetFromKeyValueArrays"/> so doesn't need to be serialized.
        /// </summary>
        /// <param name="allocator">Allocator to use for the key value array</param>
        public NativeKeyValueArrays<TNode, FixedList128Bytes<float>> GetToKeyValueArrays(Allocator allocator) => toLandmarks.GetKeyValueArrays(Allocator.Persistent);
        
        /// <summary>
        /// Returns key value arrays containing the graph distances from every landmark. This can be useful if you want to serialize the data.
        /// If you know your graph is undirected, it is sufficient to only serialize this data.
        /// </summary>
        /// <param name="allocator">Allocator to use for the key value array</param>
        public NativeKeyValueArrays<TNode, FixedList128Bytes<float>> GetFromKeyValueArrays(Allocator allocator) => fromLandmarks.GetKeyValueArrays(Allocator.Persistent);
    }
}