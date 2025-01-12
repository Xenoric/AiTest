using System;
using System.Collections.Generic;
using AnyPath.Managed.Pooling;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    [ExcludeFromDocs]
    public interface IFinderPriority<TTarget>
    {
        IComparer<TTarget> TargetComparer { get; set; }
    }
    
    [ExcludeFromDocs]
    public interface IJobOptionPriority
    {
        NativeList<int> Remap { get; set; }
    }
    
    public class InOptionsPriority<TOption, TNode, TJob> : 
        
        IFinderOptions<TOption, TNode>, IFinderPriority<TOption>,
        IComparer<int>
    
        where TNode : unmanaged, IEquatable<TNode>
        where TJob : struct, IJobOption<TNode>, IJobOptionPriority
    
    {
        private InOptions<TOption, TNode, TJob> inOptions;
        private MinHeap<int> heap;
        private IComparer<TOption> targetComparer;
        private IMutableFinder finder;

        public InOptionsPriority(IMutableFinder finder, InOptions<TOption, TNode, TJob> inOptions)
        {
            this.finder = finder;
            this.inOptions = inOptions;
            this.heap = new MinHeap<int>(this);
            this.targetComparer = inOptions; // substitute for when not provided
        }
        
        int IComparer<int>.Compare(int x, int y)
        {
            return targetComparer.Compare(inOptions.Get(x), inOptions.Get(y));
        }
        
        public IComparer<TOption> TargetComparer
        {
            get => this.targetComparer;
            set
            {
                if (!finder.IsMutable)
                    throw new ImmutableFinderException();
                this.targetComparer = value == null ? inOptions : value;
            }
        }
        
        public void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
        {
            if ((flags & ClearFinderFlags.KeepComparer) == 0)
                targetComparer = inOptions;
            
            #if UNITY_EDITOR
            if (heap.Count != 0) throw new Exception("Heap count not zero on clear");
            #endif
            
            // when keeping the targets/nodes, retransfer them to minheap
            if ((flags & ClearFinderFlags.KeepNodes) != 0)
            {
                for (int i = 0; i < inOptions.Count; i++)
                    heap.Push(i);
            }
        }
        
        public void AssignContainers(ref TJob job)
        {
            var remap = remapPool.Get();
            while (heap.Count > 0)
                remap.Add(heap.ExtractMin());
            job.Remap = remap;
        }
        
        public void ReturnContainers(ref TJob job)
        {
            remapPool.Return(job.Remap);
        }
        
        public void DisposeContainers(ref TJob job, JobHandle inputDeps)
        {
            job.Remap.Dispose(inputDeps);
        }
        
        void IFinderOptions<TOption, TNode>.Add(TOption target, TNode start, TNode goal)
        {
            #if UNITY_EDITOR
            if (heap.Count != inOptions.Count)
                throw new Exception("Shouldn't happen!");
            #endif
            
            inOptions.Add(target, start, goal);
            heap.Push(inOptions.Count - 1);
        }

        void IFinderOptions<TOption, TNode>.Add(TOption target, TNode start, TNode via, TNode goal)
        {
#if UNITY_EDITOR
            if (heap.Count != inOptions.Count)
                throw new Exception("Shouldn't happen!");
#endif
            
            inOptions.Add(target, start, via, goal);
            heap.Push(inOptions.Count - 1);
        }

        void IFinderOptions<TOption, TNode>.Add(TOption target, IEnumerable<TNode> stops)
        {
            #if UNITY_EDITOR
            if (heap.Count != inOptions.Count)
                throw new Exception("Shouldn't happen!");
            #endif
            
            inOptions.Add(target, stops);
            heap.Push(inOptions.Count - 1);
        }

        int IFinderOptions<TOption, TNode>.Count => inOptions.Count;
        
        [ExcludeFromDocs]
        private readonly static RemapPool remapPool = new RemapPool();
        
        [ExcludeFromDocs]
        private class RemapPool : Pool<NativeList<int>>
        {
            protected override void Clear(NativeList<int> unit) => unit.Clear();
            protected override NativeList<int> Create() => new NativeList<int>(Allocator.Persistent);
            protected override void DisposeUnit(NativeList<int> unit) => unit.Dispose();
        }
    }
}