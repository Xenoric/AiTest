using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Managed.Finders.Common
{
    public interface IJobPathBufferCheapest<TSeg> where TSeg : unmanaged
    {
        NativeList<TSeg> TempBuffer1 { get; set; }    
        NativeList<TSeg> TempBuffer2 { get; set; }
    }
    
    public static class OutCheapestOption<TSeg> where TSeg : unmanaged
    {
        public static void AssignContainers<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobPathBufferCheapest<TSeg>, IJobOptionPathResult
        {
            OutOptionPath<TSeg>.AssignContainers(ref job);
            job.TempBuffer1 = PathBufferPool<TSeg>.Instance.Get();
            job.TempBuffer2 = PathBufferPool<TSeg>.Instance.Get();
        }

        public static void ReturnContainers<TJob>(ref TJob job) where TJob : IJobPathBuffer<TSeg>, IJobPathBufferCheapest<TSeg>, IJobOptionPathResult
        {
            OutOptionPath<TSeg>.ReturnContainers(ref job);
            PathBufferPool<TSeg>.Instance.Return(job.TempBuffer1);
            PathBufferPool<TSeg>.Instance.Return(job.TempBuffer2);
        }
        
        public static void DisposeContainers<TJob>(ref TJob job, JobHandle inputDeps) where TJob : IJobPathBuffer<TSeg>, IJobPathBufferCheapest<TSeg>, IJobOptionPathResult
        {
            OutOptionPath<TSeg>.DisposeContainers(ref job, inputDeps);
            job.TempBuffer1.Dispose(inputDeps);
            job.TempBuffer2.Dispose(inputDeps);
        }
    }
}