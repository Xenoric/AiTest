using AnyPath.Managed.Pooling;
using AnyPath.Native;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    public interface IJobOptionPathResult
    {
        NativeReference<AStarFindOptionResult> Result { get; set; }
    }
    
    public class OutOptionPath<TEdge> where TEdge : unmanaged
    {
        public static void AssignContainers<TJob>(ref TJob job) where TJob : IJobPathBuffer<TEdge>, IJobOptionPathResult
        {
            job.PathBuffer = PathBufferPool<TEdge>.Instance.Get();
            job.Result = findOptionResultPool.Get();
        }

        public static void ReturnContainers<TJob>(ref TJob job) where TJob : IJobPathBuffer<TEdge>, IJobOptionPathResult
        {
            PathBufferPool<TEdge>.Instance.Return(job.PathBuffer);
            findOptionResultPool.Return(job.Result);
        }
        
        public static void DisposeContainers<TJob>(ref TJob job, JobHandle inputDeps) where TJob : IJobPathBuffer<TEdge>, IJobOptionPathResult
        {
            job.PathBuffer.Dispose(inputDeps);
            job.Result.Dispose(inputDeps);
        }
        
        [ExcludeFromDocs]
        private readonly static NativeFindTargetResultPool findOptionResultPool = new NativeFindTargetResultPool();
        
        [ExcludeFromDocs]
        private class NativeFindTargetResultPool : Pool<NativeReference<AStarFindOptionResult>>
        {
            protected override void Clear(NativeReference<AStarFindOptionResult> unit) => unit.Value = default;
            protected override NativeReference<AStarFindOptionResult> Create() => new NativeReference<AStarFindOptionResult>(Allocator.Persistent);
            protected override void DisposeUnit(NativeReference<AStarFindOptionResult> unit) => unit.Dispose();
        }
        

    }
}