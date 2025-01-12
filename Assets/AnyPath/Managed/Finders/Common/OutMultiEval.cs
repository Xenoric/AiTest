using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Managed.Finders.Common
{
    public interface IJobMultiEvalResult
    {
        NativeList<AStarEvalResult> Result { get; set; }
    }

    public class OutMultiEval
    {
        public static void AssignContainers<TJob>(ref TJob job) where TJob : IJobMultiEvalResult
        {
            job.Result = multiEvalResultPool.Get();
        }

        public static void ReturnContainers<TJob>(ref TJob job) where TJob : IJobMultiEvalResult
        {
            multiEvalResultPool.Return(job.Result);
        }

        public static void DisposeContainers<TJob>(ref TJob job, JobHandle inputDeps) where TJob : IJobMultiEvalResult
        {
            job.Result.Dispose(inputDeps);
        }

        private readonly static MultiNativeFindPathResultPool multiEvalResultPool = new MultiNativeFindPathResultPool();

        private class MultiNativeFindPathResultPool : Pool<NativeList<AStarEvalResult>>
        {
            protected override void Clear(NativeList<AStarEvalResult> unit) => unit.Clear();
            protected override NativeList<AStarEvalResult> Create() => new NativeList<AStarEvalResult>(Allocator.Persistent);
            protected override void DisposeUnit(NativeList<AStarEvalResult> unit) => unit.Dispose();
        }
    }
}