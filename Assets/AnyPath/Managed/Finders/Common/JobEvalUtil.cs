using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Managed.Finders.Common
{
    public interface IJobEvalResult
    {
        NativeReference<AStarEvalResult> Result { get; set; }
    }
    
    public class JobEvalUtil
    {
        public static void AssignContainers<TJob>(ref TJob job) where TJob : IJobEvalResult
        {
            job.Result = evalResultPool.Get();
        }
 
        public static void ReturnContainers<TJob>(ref TJob job) where TJob : IJobEvalResult
        {
            evalResultPool.Return(job.Result);
        }

        public static void DisposeContainers<TJob>(ref TJob job, JobHandle inputDeps) where TJob : IJobEvalResult
        {
            job.Result.Dispose(inputDeps);
        }
        
        private readonly static NativeEvalResultPool evalResultPool = new NativeEvalResultPool();
        
        private class NativeEvalResultPool : Pool<NativeReference<AStarEvalResult>>
        {
            protected override void Clear(NativeReference<AStarEvalResult> unit) => unit.Value = default;
            protected override NativeReference<AStarEvalResult> Create() => new NativeReference<AStarEvalResult>(Allocator.Persistent);
            protected override void DisposeUnit(NativeReference<AStarEvalResult> unit) => unit.Dispose();
        }
    }
}