using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Managed.Finders.Common
{
    public interface IJobMultiPathResult
    {
        NativeList<AStarFindPathResult> Result { get; set; }
    }

    public class OutMultiPath<TSeg> where TSeg : unmanaged
    {
        public static void AssignContainersMulti<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobMultiPathResult
        {
            job.PathBuffer = PathBufferPool<TSeg>.Instance.Get();
            job.Result = multiNativeFindPathResultPool.Get();
        }

        public static void ReturnContainersMulti<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobMultiPathResult
        {
            PathBufferPool<TSeg>.Instance.Return(job.PathBuffer);
            multiNativeFindPathResultPool.Return(job.Result);
        }

        public static void DisposeContainersMulti<TJob>(ref TJob job, JobHandle inputDeps) where TJob : IJobPathBuffer<TSeg>, IJobMultiPathResult
        {
            job.PathBuffer.Dispose(inputDeps);
            job.Result.Dispose(inputDeps);
        }

        private readonly static MultiNativeFindPathResultPool multiNativeFindPathResultPool = new MultiNativeFindPathResultPool();

        private class MultiNativeFindPathResultPool : Pool<NativeList<AStarFindPathResult>>
        {
            protected override void Clear(NativeList<AStarFindPathResult> unit) => unit.Clear();
            protected override NativeList<AStarFindPathResult> Create() => new NativeList<AStarFindPathResult>(Allocator.Persistent);
            protected override void DisposeUnit(NativeList<AStarFindPathResult> unit) => unit.Dispose();
        }
    }
}