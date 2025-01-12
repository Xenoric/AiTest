using Unity.Jobs;

namespace AnyPath.Managed.Disposal
{
    internal interface IScheduledFinder
    {
        JobHandle JobHandle { get; }
        void Complete();
        void Abort();
    }
}