using System;
using System.Collections;
using System.Collections.Generic;
using AnyPath.Managed.Finders;
using AnyPath.Managed.Finders.Common;
using AnyPath.Native.Util;
using UnityEngine.Internal;

namespace AnyPath.Managed.Results
{
    /// <summary>
    /// Result of a <see cref="MultiPathFinder{TGraph,TNode,TH,TMod,TProc,TSeg}"/>
    /// </summary>
    /// <typeparam name="TSeg">The of path segment</typeparam>
    public class MultiPathResult<TSeg> : IReadOnlyList<Path<TSeg>>
        where TSeg : unmanaged
    {
        private Path<TSeg>[] paths;

        /// <summary>
        /// Amount of paths currently stored in the result
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Returns the path at a given index
        /// </summary>
        public Path<TSeg> this[int index] =>
            index >= 0 && index < Count ? paths[index] : throw new ArgumentOutOfRangeException(nameof(index));

        private void Hydrate<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobMultiPathResult
        {
            this.Count = job.Result.Length;
            if (paths == null || paths.Length < Count)
                paths = new Path<TSeg>[Count];

            for (int i = 0; i < job.Result.Length; i++)
            {
                var path = paths[i];
                if (path == null)
                    paths[i] = new Path<TSeg>(job.Result[i], job.PathBuffer);
                else
                {
                    Path<TSeg>.Hydrate(path, job.Result[i], job.PathBuffer);
                }
            }
        }
        
        [ExcludeFromDocs]
        public static void Hydrate<TJob>(MultiPathResult<TSeg> result, ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobMultiPathResult
        {
            result.Hydrate(ref job);
        }
        
        [ExcludeFromDocs]
        public static MultiPathResult<TSeg> Create<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobMultiPathResult
        {
            var result = new MultiPathResult<TSeg>();
            result.Hydrate(ref job);
            return result;
        }

        public ArraySliceEnumerator<Path<TSeg>> GetEnumerator() => new ArraySliceEnumerator<Path<TSeg>>(paths, 0, Count);
        IEnumerator<Path<TSeg>> IEnumerable<Path<TSeg>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}