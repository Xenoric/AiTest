using System;
using System.Collections;
using System.Collections.Generic;
using AnyPath.Managed.Finders.Common;
using AnyPath.Native;
using AnyPath.Native.Util;
using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Managed.Results
{
    /// <summary>
    /// The result of a managed path finding request. 
    /// </summary>
    /// <typeparam name="TSeg">The type of edges contained in the path</typeparam>
    public class Path<TSeg> : IPath<TSeg>, IReadOnlyList<TSeg>
        where TSeg : unmanaged
    {
        private TSeg[] segments;


        /// <summary>
        /// Indexes the edges from the start to the end of the path.
        /// </summary>
        /// <param name="index">Index of the edge</param>
        public TSeg this[int index] => index >= 0 && index < Length ? segments[index] : throw new ArgumentOutOfRangeException(nameof(index));

        /// <summary>
        /// Amount of segments contained in this path
        /// </summary>
        public int Length { get; private set; }
        
        /// <summary>
        /// The total cost of the path
        /// </summary>
        public float Cost { get; private set; }
        
        /// <summary>
        /// Indicates wether a path was found
        /// </summary>
        public bool HasPath { get; private set; }

        /// <summary>
        /// Constructs a path from a native result.
        /// </summary>
        /// <param name="aStarResult">The native result</param>
        /// <param name="resultBuffer">The buffer containing all of the edges of the result</param>
        [ExcludeFromDocs]
        public Path(AStarFindPathResult aStarResult, NativeList<TSeg> resultBuffer)
        {
            Hydrate(aStarResult, resultBuffer);
        }

        [ExcludeFromDocs]
        public Path()
        {
        }

        /// <summary>
        /// Fill this path container with data from a native A* result.
        /// This can be used to prevent new allocations.
        /// </summary>
        /// <param name="aStarResult"></param>
        /// <param name="resultBuffer"></param>
        [ExcludeFromDocs]
        protected void Hydrate(AStarFindPathResult aStarResult, NativeList<TSeg> resultBuffer)
        {
            Cost = aStarResult.evalResult.cost;
            HasPath = aStarResult.evalResult.hasPath;
            Length = aStarResult.offsetInfo.length;

            if (segments == null || segments.Length < Length)
                segments = new TSeg[Length];

            // Only copy when length > 0 (even though haspath can be true, if start == goal)
            // Otherwise method throws an exception
            if (HasPath && Length > 0)
                NativeArray<TSeg>.Copy(resultBuffer.AsArray(), aStarResult.offsetInfo.startIndex, segments, 0, Length);
        }
        
        /// <summary>
        /// Fill this path container with data from a managed dijkstra result.
        /// This can be used to prevent new allocations.
        /// </summary>
        /// <remarks>If hasPath is false, resultBuffer may be null</remarks>
        [ExcludeFromDocs]
        protected void Hydrate(List<TSeg> resultBuffer, bool hasPath, float cost)
        {
            Cost = cost;
            HasPath = hasPath;
            Length = hasPath ? resultBuffer.Count : 0;

            if (segments == null || segments.Length < Length)
                segments = new TSeg[Length];

            if (HasPath && Length > 0)
                resultBuffer.CopyTo(0, segments, 0, Length);
        }
        
        /// <summary>
        /// Constructs a path result using the supplied array and cost
        /// </summary>
        /// <param name="hasPath">The value of hasPath, if false, supply an empty array of segments</param>
        /// <param name="segments">The array that is supplied is used directly (no copy is made)</param>
        /// <param name="cost">Total cost of the path</param>
        protected void Hydrate(bool hasPath, TSeg[] segments, float cost)
        {
            this.Cost = cost;
            this.segments = segments;
            this.HasPath = true;
        }
        
        /// <summary>
        /// Constructs a path result using the supplied array and cost
        /// </summary>
        /// <param name="hasPath">The value of hasPath, if false, supply an empty array of segments</param>
        /// <param name="segments">The array that is supplied is used directly (no copy is made)</param>
        /// <param name="cost">Total cost of the path</param>
        [ExcludeFromDocs]
        public Path(bool hasPath, TSeg[] segments, float cost)
        {
            Hydrate(hasPath, segments, cost);
        }
        
        [ExcludeFromDocs]
        public static void Hydrate(Path<TSeg> path, AStarFindPathResult aStarResult, NativeList<TSeg> resultBuffer)
        {
            path.Hydrate(aStarResult, resultBuffer);
        }
        
        /// <summary>
        /// Hydrate for a managed dijkstra result. If there is no path, resultbuffer is allowed to be null.
        /// </summary>
        [ExcludeFromDocs]
        public static void Hydrate(Path<TSeg> path, bool hasPath, List<TSeg> resultBuffer, float cost)
        {
            path.Hydrate(resultBuffer, hasPath, cost);
        }

        public ArraySliceEnumerator<TSeg> GetEnumerator() => new ArraySliceEnumerator<TSeg>(segments, 0, Length);
        IEnumerator<TSeg> IEnumerable<TSeg>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        int IReadOnlyCollection<TSeg>.Count => Length;
    }
    
    /// <summary>
    /// The result of a managed path finding request. The target to which a path was found is included.
    /// </summary>
    /// <typeparam name="TOption">The type of option</typeparam>
    /// <typeparam name="TSeg">The type of segments contained in the path</typeparam>
    public class Path<TOption, TSeg> : Path<TSeg>, IPath<TOption, TSeg>
        where TSeg : unmanaged
    {
        /// <summary>
        /// The option to which a path was found. If no path was found, this will be null or default.
        /// </summary>
        public TOption Option { get; private set; }
        
        [ExcludeFromDocs]
        public Path(TOption candidate, AStarFindPathResult aStarResult, NativeList<TSeg> resultBuffer) : base(aStarResult, resultBuffer)
        {
            this.Option = candidate;
        }
        
        /// <summary>
        /// Fill this path container with data from a native A* result.
        /// This can be used to prevent new allocations.
        /// </summary>
        /// <param name="aStarResult"></param>
        /// <param name="resultBuffer"></param>
        protected void Hydrate(TOption candidate, AStarFindPathResult aStarResult, NativeList<TSeg> resultBuffer)
        {
            this.Option = candidate;
            base.Hydrate(aStarResult, resultBuffer);
        }
        
        [ExcludeFromDocs]
        public static Path<TOption, TSeg> CreateResultOption<TJob>(ref TJob job, InOptions<TOption, TJob> inOptions, out bool resultCreated) 
            where TJob : struct, IJobPathBuffer<TSeg>, IJobOption, IJobOptionPathResult
        {
            switch (inOptions.CreateResult(ref job, out TOption winner))
            {
                case FinderTargetsResultMethod.Retry:
                    resultCreated = false;
                    return default;
                case FinderTargetsResultMethod.CreateNoPathResult:
                    resultCreated = true;
                    return new Path<TOption, TSeg>(default, AStarFindPathResult.NoPath, default);
                default:
                    resultCreated = true;
                    return new Path<TOption, TSeg>(winner, job.Result.Value.findPathResult, job.PathBuffer);
            }
        }
        
        [ExcludeFromDocs]
        public static void Hydrate<TJob>(Path<TOption, TSeg> path, ref TJob job, InOptions<TOption, TJob> inOptions) 
            where TJob : struct, IJobPathBuffer<TSeg>, IJobOption, IJobOptionPathResult
        {
            switch (inOptions.CreateResult(ref job, out TOption winner))
            {
                case FinderTargetsResultMethod.Retry:
                    return;
                case FinderTargetsResultMethod.CreateNoPathResult:
                    path.Hydrate(default, AStarFindPathResult.NoPath, default);
                    break;
                default:
                    path.Hydrate(winner, job.Result.Value.findPathResult, job.PathBuffer);
                    break;
            }
        }
    }
}