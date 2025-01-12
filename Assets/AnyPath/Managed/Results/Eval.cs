using AnyPath.Managed.Finders.Common;
using AnyPath.Native;
using UnityEngine.Internal;

namespace AnyPath.Managed.Results
{
    /// <summary>
    /// Indicates wether a path is possible.
    /// </summary>
    public struct Eval : IEval
    {
        /// <summary>
        /// The total cost of the path
        /// </summary>
        public float Cost { get; }
        
        /// <summary>
        /// Indicates wether a path was found
        /// </summary>
        public bool HasPath { get; }

        /// <summary>
        /// Constructs an Eval result from a native result.
        /// </summary>
        /// <param name="aStarEvalResult">The native result</param>
        [ExcludeFromDocs]
        public Eval(AStarEvalResult aStarEvalResult)
        {
            this.Cost = aStarEvalResult.cost;
            this.HasPath = aStarEvalResult.hasPath;
        }
        
        [ExcludeFromDocs]
        public static Eval CreateResult<TJob>(ref TJob job) where TJob : IJobEvalResult
        {
            return new Eval(job.Result.Value);
        }
        
        [ExcludeFromDocs]
        public static Eval[] CreateResultMulti<TJob>(ref TJob job) where TJob : IJobMultiEvalResult
        {
            var results = new Eval[job.Result.Length];

            for (int i = 0; i < job.Result.Length; i++)
                results[i] = new Eval(job.Result[i]);

            return results;
        }
    }
    
    /// <summary>
    /// Indicates wether a path to a target is possible.
    /// </summary>
    /// <typeparam name="TOption">The type of target</typeparam>
    public struct Eval<TOption> : IEval<TOption>
    {
        /// <summary>
        /// The total cost of the path
        /// </summary>
        public float Cost { get; }

        /// <summary>
        /// Indicates wether a path was found
        /// </summary>
        public bool HasPath { get; }

        /// <summary>
        /// The target to which a path was found. If no path was found, this will be null or default.
        /// </summary>
        public TOption Option { get; }

        /// <summary>
        /// Constructs an Eval result from a native result.
        /// </summary>
        /// <param name="aStarEvalPathResult">The native result</param>
        /// <param name="target">The target object that was associated with the result</param>
        public Eval(AStarEvalOptionResult aStarEvalPathResult, TOption target)
        {
            this.Cost = aStarEvalPathResult.evalResult.cost;
            this.HasPath = aStarEvalPathResult.evalResult.hasPath;
            this.Option = target;
        }

        public static readonly Eval<TOption> NoPath = new Eval<TOption>(AStarEvalOptionResult.NoPath, default);
        
        public static Eval<TOption> CreateResultOption<TJob>(ref TJob job, InOptions<TOption, TJob> inOptions, out bool resultCreated) 
            where TJob : struct, IJobOption, IJobOptionEvalResult
        {
            switch (inOptions.CreateResult(ref job, out TOption winner))
            {
                case FinderTargetsResultMethod.Retry:
                    resultCreated = false;
                    return default;
                case FinderTargetsResultMethod.CreateNoPathResult:
                    resultCreated = true;
                    return NoPath;
                default:
                    resultCreated = true;
                    return new Eval<TOption>(job.Result.Value, winner);
            }
        }
    }
}