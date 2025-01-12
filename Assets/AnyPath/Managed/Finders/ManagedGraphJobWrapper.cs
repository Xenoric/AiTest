using System;
using System.Collections;
using AnyPath.Managed.Disposal;
using AnyPath.Managed.Finders.Common;
using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders
{
    /// <summary>
    /// Base for all finders, evaluators and managed dijkstra
    /// </summary>
    /// <typeparam name="TGraph">The type of graph this finder operates on</typeparam>
    /// <typeparam name="TNode">The node type associated with the type of graph</typeparam>
    /// <typeparam name="TJob">The job that this finder uses internally to perform the path finding</typeparam>
    /// <typeparam name="TResult">The type of result this finder gives</typeparam>
    [ExcludeFromDocs]
    public abstract class ManagedGraphJobWrapper<TGraph, TNode, TJob, TResult> : 
        
        IEnumerator,
        IScheduledFinder,
        IMutableFinder,
       
        IFinder<TGraph, TResult>
    
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
     
        where TJob : struct, IJobGraphAStar<TGraph, TNode>
    
    {
        private enum MutableState
        {
            Mutable,
            InFlight,
            Completed,
            Aborted
        }

        /// <summary>
        /// Implementations fill this struct with the neccessary arguments to perform the work.
        /// All native containers are fetched from a pool on demand, having two advantages:
        /// 1)  when the pathfinders aren't re-used, we're still not allocating and deallocating nativecontainers on every request
        /// 2)  if the pathfinder itself would store it's neccessary native containers, there's no way to manage disposing of them
        ///     since we don't have control over when the class can be disposed.
        /// </summary>
        protected TJob job;
        private JobHandle jobHandle;
        private MutableState state;
        
        private TResult cachedResult;
        private bool cachedResultCreated; // cachedResult may be a struct so we keep this around before calling HydrateResult
        
        /// <summary>
        /// Implementations can expose a public property that sets this field.
        /// When true, <see cref="cachedResult"/> will be created once and <see cref="HydrateResult"/> will be called with that
        /// field.
        /// </summary>
        protected bool reuseResult;
       
        /// <summary>
        /// This value is not used for Dijkstra's algo and only publicly exposed for descendants of <see cref="Eval{TGraph,TNode,TH,TMod,TJob,TResult}"/>.
        /// However we need the actual value here as it is more efficient to assign it directly upon the A* instantiation.
        /// </summary>
        protected int maxExpand = 65536;
       

        /// <summary>
        /// Event that occurs when the last request has been completed.
        /// </summary>
        public event Action<IFinder<TGraph, TResult>> Completed; 
        
        /// <summary>
        /// Indicates if the values of this request can be modified. Once a request is sent, it becomes immutable
        /// until Clear() is called.
        /// </summary>
        public bool IsMutable => state == MutableState.Mutable;

        /// <summary>
        /// Indicates if the request has been made and completed. This value gets reset after a call to Clear()
        /// </summary>
        public bool IsCompleted => state == MutableState.Completed;

        /// <summary>
        /// Jobhandle of the current job associated with this finder
        /// </summary>
        public JobHandle JobHandle => jobHandle;
        
        /// <summary>
        /// Result of the last request made from this finder.
        /// </summary>
        public TResult Result { get; private set; }

        /// <summary>
        /// The graph to perform the request on.
        /// </summary>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        public TGraph Graph
        {
            get => job.Graph;
            set
            {
                if (!IsMutable) throw new ImmutableFinderException();
                job.Graph = value;
            }
        }


        /// <summary>
        /// Schedules this finder using Unity's Job System.
        /// </summary>
        /// <returns>An IEnumerator which can be used to suspend a coroutine while waiting for the result</returns>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        public IEnumerator Schedule()
        {
            if (!IsMutable) throw new ImmutableFinderException();

            AssignContainers(ref job);
            this.jobHandle = job.Schedule();
            this.state = MutableState.InFlight;
            
            ManagedDisposer.Register(this);
            return this;
        }

        
        /// <summary>
        /// Clear this finder allowing for a new request to be made.
        /// </summary>
        /// <remarks>If the previous request was not completed yet, that request will be aborted.</remarks>
        /// <param name="flags">Flags that can be used to preserve certain settings on this finder</param>
        public virtual void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll)
        {
            if (state == MutableState.InFlight)
                ManagedDisposer.Abort(this);

            jobHandle = default;
            
            if ((flags & ClearFinderFlags.KeepGraph) == 0)
                job.Graph = default;
            if ((flags & ClearFinderFlags.KeepCompletedEventHandlers) == 0)
                Completed = null;
          
            Result = default;
            state = MutableState.Mutable;
        }
        
        JobHandle IScheduledFinder.JobHandle => jobHandle;
        
        [ExcludeFromDocs]
        void IScheduledFinder.Abort()
        {
#if UNITY_EDITOR
            if (state != MutableState.InFlight)
                throw new Exception("State should be in flight here");
#endif
           
            this.DisposeContainers(ref job, jobHandle);
            this.state = MutableState.Aborted;
        }
        
        // note: Run cannot return TResult because FinderTargets would need a TResult too to call back into Run.
        // Schedule() returns an IEnumerator so that's fine since it's not a generic type.

        /// <summary>
        /// Runs this finder's request immediately on the main thread.
        /// </summary>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        public void Run()
        {
            if (!IsMutable) throw new ImmutableFinderException();
            
            AssignContainers(ref job);
            this.job.Run();
            CreateOrHydrateResult();
            ReturnContainers(ref job);

            this.state = MutableState.Completed;
            OnCompletedInternal(true);
        }
        
        [ExcludeFromDocs]
        void IScheduledFinder.Complete()
        {
#if UNITY_EDITOR
            if (state != MutableState.InFlight)
                throw new Exception("State should be in flight here");
#endif
            jobHandle.Complete();

            CreateOrHydrateResult();
            ReturnContainers(ref job);
            this.state = MutableState.Completed;
            OnCompletedInternal(false);
        }

        [ExcludeFromDocs]
        void CreateOrHydrateResult()
        {
            if (reuseResult)
            {
                if (!this.cachedResultCreated)
                    cachedResult = CreateResult(ref job, out this.cachedResultCreated);
                else
                    HydrateResult(ref cachedResult, ref job);

                this.Result = cachedResult;
            }
            else
            {
                this.Result = CreateResult(ref job, out _);
            }
        }

        /// <summary>
        /// Gets called just before a request is completed. This stage can be used to reschedule the request automatically.
        /// </summary>
        /// <param name="sync">Indicates if the request was made on the main thread via a call to Run</param>
        [ExcludeFromDocs]
        protected virtual void OnCompletedInternal(bool sync) => OnCompleted();
        
        /// <summary>
        /// Gets called with a reference to the job that has executed. Extract the final result from the job.
        /// </summary>
        /// <param name="job">The job that was executed.</param>
        /// <returns>A managed result object</returns>
        [ExcludeFromDocs]
        protected abstract TResult CreateResult(ref TJob job, out bool cachedResultCreated);

        /// <summary>
        /// Implement modifying the result, gets called when <see cref="ReuseResult"/> is true and <see cref="CreateResult"/> is called
        /// at least once.
        /// </summary>
        [ExcludeFromDocs]
        protected abstract void HydrateResult(ref TResult result, ref TJob job);

        /// <summary>
        /// Occurs when the request was fully completed and no rescheduling will take place. Fires the Completed event by default.
        /// </summary>
        [ExcludeFromDocs]
        protected virtual void OnCompleted()
        {
            Completed?.Invoke(this);
        }

        /// <summary>
        /// Assigns the NativeContainers neccessary to perform the request to the job.
        /// </summary>
        /// <remarks>The NativeContainers should be fetched from their respective pools.
        /// This benefits performance as there will be less allocations. Also, the pools automatically manage their disposal</remarks>
        /// <param name="job">The job struct that will be scheduled</param>
        [ExcludeFromDocs]
        protected virtual void AssignContainers(ref TJob job)
        {
            var astar = aStarPool.Get();
            astar.maxExpand = this.maxExpand;
            job.AStar = astar;
        }

        /// <summary>
        /// Returns the used Native Containers to their respective pools.
        /// </summary>
        /// <param name="job">The job that has completed</param>
        [ExcludeFromDocs]
        protected virtual void ReturnContainers(ref TJob job)
        {
            aStarPool.Return(job.AStar);
        }

        /// <summary>
        /// Implements disposing the used containers. This get's called when a request was aborted.
        /// Since there's no way to determine when the containers can be returned to their pool, we dispose them instead.
        /// </summary>
        /// <param name="job">The job that has completed</param>
        /// <param name="inputDeps">Job dependencies that should be used for disposing the Native Containers</param>
        [ExcludeFromDocs]
        protected virtual void DisposeContainers(ref TJob job, JobHandle inputDeps)
        {
            job.AStar.Dispose(inputDeps);
        }
        
        [ExcludeFromDocs] bool IEnumerator.MoveNext()
        {
            return state == MutableState.InFlight;
        }

        [ExcludeFromDocs] void IEnumerator.Reset() { }
        [ExcludeFromDocs] object IEnumerator.Current => null;
        
        /*
         * Shared pools
         */
        
        [ExcludeFromDocs]
        protected readonly static AStarPool aStarPool = new AStarPool();
        
        [ExcludeFromDocs]
        protected class AStarPool : Pool<AStar<TNode>>
        {
            protected override AStar<TNode> Create() => new AStar<TNode>(Allocator.Persistent);
            protected override void Clear(AStar<TNode> unit) { }
            protected override void DisposeUnit(AStar<TNode> unit) => unit.Dispose();
        }
    }
}