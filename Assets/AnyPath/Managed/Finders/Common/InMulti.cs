using System;
using System.Collections.Generic;
using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    [ExcludeFromDocs]
    public interface IAddMulti<TNode>
    {
        void Add(TNode start, TNode goal);
        void Add(IEnumerable<TNode> stops);
        int Count { get; }
    }
    
    [ExcludeFromDocs]
    public interface IJobMulti<TNode>
        where TNode : unmanaged, IEquatable<TNode>
    {
        NativeList<TNode> Nodes { get; set; }
        NativeList<OffsetInfo> Offsets { get; set; }
    }
    
    /// <summary>
    /// Provides functionality for adding stops and a goal to a multi finder. Throws an exception if the finder is immutable.
    /// </summary>
    /// <typeparam name="TNode">Type of nodes</typeparam>
    /// <typeparam name="TJob">Type of job to populate</typeparam>
    public class InMulti<TNode, TJob> : IAddMulti<TNode>

        where TJob : struct, IJobMulti<TNode>
        where TNode : unmanaged, IEquatable<TNode>
    
    {
        private readonly IMutableFinder finder;
        private List<TNode> nodes;
        private List<OffsetInfo> offsets;

        public InMulti(IMutableFinder finder, int initialCapacity)
        {
            this.finder = finder;
            this.nodes = new List<TNode>(initialCapacity);
            this.offsets = new List<OffsetInfo>(initialCapacity);
        }

        public void Add(TNode start, TNode goal)
        {
            if (!finder.IsMutable) 
                throw new ImmutableFinderException();
            
            nodes.Add(start);
            nodes.Add(goal);
            offsets.Add(new OffsetInfo(nodes.Count - 2, 2));
        }
        
        public void Add(IEnumerable<TNode> stops)
        {
            if (!finder.IsMutable) 
                throw new ImmutableFinderException();

            int startIndex = nodes.Count;
            foreach (TNode stop in stops)
                nodes.Add(stop);

            if (nodes.Count == startIndex)
                throw new ArgumentException("Must contain at least one stop (the start)", nameof(stops));

            offsets.Add(new OffsetInfo(startIndex, nodes.Count - startIndex));
        }

        public void Clear(ClearFinderFlags clearFinderFlags)
        {
            if ((clearFinderFlags & ClearFinderFlags.KeepNodes) == 0)
            {
                nodes.Clear();
                offsets.Clear();
            }
        }
        
        /// <summary>
        /// Amount of requests added
        /// </summary>
        public int Count => offsets.Count;


        public void AssignContainers(ref TJob job)
        {
            var nativeNodes = nodePool.Get();
            for (int i = 0; i < nodes.Count; i++)
                nativeNodes.Add(nodes[i]);
            job.Nodes = nativeNodes;
            
            var nativeOffsets = offsetPool.Get();
            
            for (int i = 0; i < offsets.Count; i++)
                nativeOffsets.Add(offsets[i]);
            job.Offsets = nativeOffsets;
        }

        public void DisposeContainers(ref TJob job, JobHandle inputDeps)
        {
            job.Nodes.Dispose(inputDeps);
            job.Offsets.Dispose(inputDeps);
        }

        public void ReturnContainers(ref TJob job)
        {
            nodePool.Return(job.Nodes);
            offsetPool.Return(job.Offsets);
        }

        /// <summary>
        /// Common code between MultiEval and MultiFindPath
        /// Reconstructs the individual stops + goal for each batched request
        /// </summary>
        [ExcludeFromDocs]
        public static void GetStops(in NativeList<OffsetInfo> offsets, NativeList<TNode> nodes, int index, out NativeSlice<TNode> stops)
        {
            var offsetInfo = offsets[index];
            stops = new NativeSlice<TNode>(nodes.AsArray(), offsetInfo.startIndex, offsetInfo.length);
        }

        private readonly static OffsetPool offsetPool = new OffsetPool();
        
        private class OffsetPool : Pool<NativeList<OffsetInfo>>
        {
            protected override NativeList<OffsetInfo> Create() => new NativeList<OffsetInfo>(Allocator.Persistent);
            protected override void Clear(NativeList<OffsetInfo> unit) => unit.Clear();
            protected override void DisposeUnit(NativeList<OffsetInfo> unit) => unit.Dispose();
        }
        
        private readonly static NodePool nodePool = new NodePool();

        private class NodePool : Pool<NativeList<TNode>>
        {
            protected override NativeList<TNode> Create() => new NativeList<TNode>(Allocator.Persistent);
            protected override void Clear(NativeList<TNode> unit) => unit.Clear();
            protected override void DisposeUnit(NativeList<TNode> unit) => unit.Dispose();
        }
    }
}