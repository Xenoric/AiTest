using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Managed.Finders.Common
{
    public interface IJobOptionEvalResult
    {
        NativeReference<AStarEvalOptionResult> Result { get; set; }
    }

    public class OutOptionEval
    {
        public static void AssignContainersEval<TJob>(ref TJob job) where TJob : IJobOptionEvalResult
        {
            job.Result = evalTargetResultPool.Get();
        }

        public static void ReturnContainersEval<TJob>(ref TJob job) where TJob : IJobOptionEvalResult
        {
            evalTargetResultPool.Return(job.Result);
        }
        
        public static void DisposeContainersEval<TJob>(ref TJob job, JobHandle inputDeps) where TJob : IJobOptionEvalResult
        {
            job.Result.Dispose(inputDeps);
        }

        private readonly static AStarEvalTargetResultPool evalTargetResultPool = new AStarEvalTargetResultPool();

        private class AStarEvalTargetResultPool : Pool<NativeReference<AStarEvalOptionResult>>
        {
            protected override void Clear(NativeReference<AStarEvalOptionResult> unit) => unit.Value = default;
            protected override NativeReference<AStarEvalOptionResult> Create() => new NativeReference<AStarEvalOptionResult>(Allocator.Persistent);
            protected override void DisposeUnit(NativeReference<AStarEvalOptionResult> unit) => unit.Dispose();
        }
    }
}