using System;
using System.Collections.Generic;
using AnyPath.Managed.Pooling;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    /// <summary>
    /// Container for providing stops to a finder
    /// </summary>
    /// <typeparam name="TNode">The type of node</typeparam>
    public interface IFinderStops<TNode>
    {
        void Add(TNode stop);
        void SetStartAndGoal(TNode start, TNode goal);
        int Count { get; }
    }
    
    [ExcludeFromDocs]
    public interface IJobStops<TNode> where TNode : unmanaged, IEquatable<TNode>
    {
        NativeList<TNode> Stops { get; set; }
    }
    
    /// <summary>
    /// Component used by finders to define a starting node and multiple stops
    /// </summary>
    /// <typeparam name="TNode">The type of node</typeparam>
    /// <typeparam name="TJob">Type type of finder job</typeparam>
    public class InStops<TNode, TJob> : IFinderStops<TNode>
    
        where TNode : unmanaged, IEquatable<TNode>
        where TJob : struct, IJobStops<TNode>
    {
        private IMutableFinder finder;
        private List<TNode> stops;

        public InStops(IMutableFinder finder, int initialCapacity)
        {
            this.finder = finder;
            this.stops = new List<TNode>(initialCapacity);
        }

        public void Add(TNode stop)
        {
            if (!finder.IsMutable)
                throw new ImmutableFinderException();
            
            stops.Add(stop);
        }

        public void SetStartAndGoal(TNode start, TNode goal)
        {
            if (!finder.IsMutable)
                throw new ImmutableFinderException();
            
            stops.Clear();
            stops.Add(start);
            stops.Add(goal);
        }

        public void Clear()
        {
            if (!finder.IsMutable)
                throw new ImmutableFinderException();
            
            stops.Clear();
        }

        public int Count => stops.Count;
        
        public void AssignContainers(ref TJob job)
        {
            var currentStops = stopPool.Get();
            for (int i = 0; i < stops.Count; i++)
                currentStops.Add(stops[i]);
            job.Stops = currentStops;
        }

        public void DisposeContainers(ref TJob job, JobHandle inputDeps)
        {
            job.Stops.Dispose(inputDeps);
        }

        public void ReturnContainers(ref TJob job)
        {
            stopPool.Return(job.Stops);
        }

        public void Clear(ClearFinderFlags clearFinderFlags)
        {
            if ((clearFinderFlags & ClearFinderFlags.KeepNodes) == 0)
                stops.Clear();
        }
        
        private readonly static StopPool stopPool = new StopPool();
        private class StopPool : Pool<NativeList<TNode>>
        {
            protected override NativeList<TNode> Create() => new NativeList<TNode>(Allocator.Persistent);
            protected override void Clear(NativeList<TNode> unit) => unit.Clear();
            protected override void DisposeUnit(NativeList<TNode> unit) => unit.Dispose();
        }
    }
}