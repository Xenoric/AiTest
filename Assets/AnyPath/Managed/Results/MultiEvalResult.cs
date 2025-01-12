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
    /// Result of a <see cref="MultiPathEvaluator{TGraph,TNode,TH,TMod}"/>
    /// </summary>
    public class MultiEvalResult : IReadOnlyList<Eval>
    {
        private Eval[] evals;

        /// <summary>
        /// Amount of paths currently stored in the result
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Returns the evaluation at a given index
        /// </summary>
        public Eval this[int index] =>
            index >= 0 && index < Count ? evals[index] : throw new ArgumentOutOfRangeException(nameof(index));

        private void Hydrate<TJob>(ref TJob job) where TJob : IJobMultiEvalResult
        {
            this.Count = job.Result.Length;
            if (evals == null || evals.Length < Count)
                evals = new Eval[Count];

            for (int i = 0; i < job.Result.Length; i++)
                evals[i] = new Eval(job.Result[i]);
        }
        
        [ExcludeFromDocs]
        public static void Hydrate<TJob>(MultiEvalResult result, ref TJob job) where TJob : IJobMultiEvalResult
        {
            result.Hydrate(ref job);
        }
        
        [ExcludeFromDocs]
        public static MultiEvalResult Create<TJob>(ref TJob job) where TJob : IJobMultiEvalResult
        {
            var result = new MultiEvalResult();
            result.Hydrate(ref job);
            return result;
        }

        public ArraySliceEnumerator<Eval> GetEnumerator() => new ArraySliceEnumerator<Eval>(evals, 0, Count);
        IEnumerator<Eval> IEnumerable<Eval>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}