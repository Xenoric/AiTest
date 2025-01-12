using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Managed.Finders.Common
{
    public interface IJobPathBuffer<TEdge> where TEdge : unmanaged
    {
        NativeList<TEdge> PathBuffer { get; set; }    
    }
    
    public interface IJobFindPathResult
    {
        NativeReference<AStarFindPathResult> Result { get; set; }
    }
    
    public class OutPath<TSeg> where TSeg : unmanaged
    {
        public static void AssignContainers<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobFindPathResult
        {
            job.PathBuffer = PathBufferPool<TSeg>.Instance.Get();
            job.Result = nativeFindPathResultPool.Get();
        }
 
        public static void ReturnContainers<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobFindPathResult
        {
            PathBufferPool<TSeg>.Instance.Return(job.PathBuffer);
            nativeFindPathResultPool.Return(job.Result);
        }

        public static void DisposeContainers<TJob>(ref TJob job, JobHandle inputDeps) where TJob : IJobPathBuffer<TSeg>, IJobFindPathResult
        {
            job.PathBuffer.Dispose(inputDeps);
            job.Result.Dispose(inputDeps);
        }
        
        private readonly static NativeFindPathResultResultPool nativeFindPathResultPool = new NativeFindPathResultResultPool();

        private class NativeFindPathResultResultPool : Pool<NativeReference<AStarFindPathResult>>
        {
            protected override void Clear(NativeReference<AStarFindPathResult> unit) => unit.Value = default;
            protected override NativeReference<AStarFindPathResult> Create() => new NativeReference<AStarFindPathResult>(Allocator.Persistent);
            protected override void DisposeUnit(NativeReference<AStarFindPathResult> unit) => unit.Dispose();
        }
    }
}