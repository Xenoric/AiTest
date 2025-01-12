using System;
using System.Collections.Generic;
using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    /// <summary>
    /// Container for adding options to a finder
    /// </summary>
    /// <typeparam name="TOption"></typeparam>
    /// <typeparam name="TNode"></typeparam>
    public interface IFinderOptions<in TOption, TNode>
    {
        void Add(TOption target, TNode start, TNode goal);
        void Add(TOption target, TNode start, TNode via, TNode goal); // semi common use case, add 3 stops without IEnumerable
        void Add(TOption target, IEnumerable<TNode> stops);
        int Count { get; }
    }
    
    [ExcludeFromDocs]
    public interface IJobOption
    {
        int TargetIndex { get; }
    }
    
    [ExcludeFromDocs]
    public interface IJobOption<TNode> : IJobOption where TNode : unmanaged, IEquatable<TNode>
    {
        NativeList<TNode> Nodes { get; set; }
        NativeList<OffsetInfo> Offsets { get; set; }
    }

    [ExcludeFromDocs]
    public enum FinderTargetsResultMethod
    {
        CreateHasPathResult,
        CreateNoPathResult,
        Retry
    }

    public abstract class InOptions<TOption, TJob>
        where TJob : struct, IJobOption
    {
        public abstract FinderTargetsResultMethod CreateResult(ref TJob job, out TOption winningTarget);
    }

    /// <summary>
    /// Shared code between Cheapest and First finders
    /// </summary>
    /// <typeparam name="TNode">Type of node</typeparam>
    /// <typeparam name="TOption">Type of target</typeparam>
    /// <typeparam name="TJob">Type of job</typeparam>
    public class InOptions<TOption, TNode, TJob> : InOptions<TOption, TJob>, IFinderOptions<TOption, TNode>,
        
        IOptionValidator<TOption>, 
        IOptionReserver<TOption>, IComparer<TOption>

        where TNode : unmanaged, IEquatable<TNode>
        where TJob : struct, IJobOption<TNode>

    {
        private List<TOption> targets;
        private List<OffsetInfo> offsets;
        private List<TNode> nodes;
        private TOption winner;
        
        private IOptionValidator<TOption> optionValidator;
        private IOptionReserver<TOption> optionReserver;
        private IRetryableFinder finder;
        private FinderTargetsResultMethod internalResultMethod;
        
        private int maxRetries = -1;
        private int currentRetries;
        
        public InOptions(IRetryableFinder finder, int initialCapacity)
        {
            this.finder = finder;
            
            //this.validationIndexes = new List<int>(initialCapacity);
            this.targets = new List<TOption>(initialCapacity);
            this.offsets = new List<OffsetInfo>(initialCapacity);
            this.nodes = new List<TNode>(initialCapacity);

            this.optionValidator = this; // default to no validation
            this.optionReserver = this; // default to no reservation
        }
        
        public int MaxRetries
        {
            get => maxRetries;
            set
            {
                if (!finder.IsMutable) throw new ImmutableFinderException();
                maxRetries = value;
            }
        }

        public TOption Get(int index) => targets[index];
        
        public IOptionValidator<TOption> OptionValidator
        {
            get => optionValidator;
            set
            {
                if (!finder.IsMutable) throw new ImmutableFinderException();
                this.optionValidator = value == null ? this : value;
            }
        }
        
        public IOptionReserver<TOption> OptionReserver
        {
            get => optionReserver;
            set
            {
                if (!finder.IsMutable) throw new ImmutableFinderException();
                this.optionReserver = value == null ? this : value;
            }
        }

        public void Add(TOption target, TNode start, TNode goal)
        {
            if (!finder.IsMutable)
                throw new ImmutableFinderException();
            
            nodes.Add(start);
            nodes.Add(goal);
            targets.Add(target);
            offsets.Add(new OffsetInfo(nodes.Count - 2, 2));
        }
        
        public void Add(TOption target, TNode start, TNode via, TNode goal)
        {
            if (!finder.IsMutable)
                throw new ImmutableFinderException();
            
            nodes.Add(start);
            nodes.Add(via);
            nodes.Add(goal);
            targets.Add(target);
            offsets.Add(new OffsetInfo(nodes.Count - 3, 3));
        }

        public void Add(TOption target, IEnumerable<TNode> stops)
        {
            if (!finder.IsMutable)
                throw new ImmutableFinderException();
            
            int startIndex = nodes.Count;
            foreach (TNode stop in stops)
                nodes.Add(stop);
            
            if (nodes.Count == startIndex)
                throw new ArgumentException("Must contain at least one stop (the start)", nameof(stops));
            
            targets.Add(target);
            offsets.Add(new OffsetInfo(startIndex, nodes.Count - startIndex));
        }

        public int Count => targets.Count;

        public IEnumerable<TOption> Targets => targets;
      

        internal void AssignContainers(ref TJob job)
        {
            job.Offsets = offsetPool.Get();
            job.Nodes = nodePool.Get();
            
            // reset at the beginning, if multiple finders use the same validator object
            // this will keep working (assuming it's all on the main thread that is)
            //validationIndexes.Clear();
            
            if (offsets.Count != targets.Count)
                Debug.LogError("!");

            for (int i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                int lengthBefore = job.Nodes.Length;

                if (optionValidator.Validate(target))
                {
                    var offsetInfo = offsets[i];

                    for (int j = offsetInfo.startIndex; j < offsetInfo.startIndex + offsetInfo.length; j++)
                        job.Nodes.Add(nodes[j]);

                    // construct a new offset info that is up to date with the pruned nodes
                    job.Offsets.Add(new OffsetInfo(lengthBefore, job.Nodes.Length - lengthBefore));
                }
                else
                {
                    // add no stops, will skip evaluation in the job altogether
                    job.Offsets.Add(new OffsetInfo(lengthBefore, 0));
                }
            }
        }

        public void ReturnContainers(ref TJob job)
        {
            offsetPool.Return(job.Offsets);
            nodePool.Return(job.Nodes);
        }

        public void DisposeContainers(ref TJob job, JobHandle inputDeps)
        {
            job.Offsets.Dispose(inputDeps);
            job.Nodes.Dispose(inputDeps);
        }

        public override FinderTargetsResultMethod CreateResult(ref TJob job, out TOption winningTarget)
        {
            int winnerIndex = job.TargetIndex;
            
            // none of the candidates yielded a path, we're done
            if (winnerIndex < 0)
            {
                internalResultMethod = FinderTargetsResultMethod.CreateNoPathResult;
                winner = winningTarget = default;
                return internalResultMethod;
            }

            winner = winningTarget = targets[winnerIndex];
            if (!optionValidator.Validate(winner))
            {
                // keep track of how many retries we've done. if we exceed max, accept a no result
                bool acceptResult = ++currentRetries > (maxRetries < 0 ? targets.Count : maxRetries);
                
                // if we're going to retry, dont even create a result class
                internalResultMethod = acceptResult ? FinderTargetsResultMethod.CreateNoPathResult : FinderTargetsResultMethod.Retry;
                return internalResultMethod;
            }
            
            internalResultMethod = FinderTargetsResultMethod.CreateHasPathResult;
            return internalResultMethod;
        }

        public void OnCompletedInternal(bool sync)
        {
            switch (internalResultMethod)
            {
                case FinderTargetsResultMethod.CreateHasPathResult:
                    optionReserver.Reserve(winner);
                    goto case FinderTargetsResultMethod.CreateNoPathResult;
                case FinderTargetsResultMethod.CreateNoPathResult:
                    currentRetries = 0; // no retry will be attempted
                    finder.OnNoRetry();
                    //finder.Proceed(FinderTargetsProceedMethod.Complete);
                    return;
            }

            // invoke a retry
            finder.Clear(ClearFinderFlags.KeepAll);
            if (sync)
                finder.OnRetryRun();
            else
                finder.OnRetrySchedule();
            //finder.Proceed(sync ? FinderTargetsProceedMethod.RetryRun : FinderTargetsProceedMethod.RetrySchedule);
        }
        
        public void Clear(ClearFinderFlags clearFinderFlags)
        {
            if ((clearFinderFlags & ClearFinderFlags.KeepNodes) == 0)
            {
                targets.Clear();
                offsets.Clear();
                nodes.Clear();
            }

            if ((clearFinderFlags & ClearFinderFlags.KeepValidator) == 0)
                optionValidator = this;
            
            if ((clearFinderFlags & ClearFinderFlags.KeepReserver) == 0)
                optionReserver = this;
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

        public bool Validate(TOption option) => true;
        public void Reserve(TOption option) { }
        public int Compare(TOption x, TOption y) => 0; // used by PriorityFinder as a substitute for when no comparer is given
    }
}