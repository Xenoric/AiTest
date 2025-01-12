using AnyPath.Managed.Pooling;
using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    public class PathBufferPool<TSeg> : Pool<NativeList<TSeg>> where TSeg : unmanaged
    {
        [ExcludeFromDocs]
        public readonly static PathBufferPool<TSeg> Instance = new PathBufferPool<TSeg>();

        protected override void Clear(NativeList<TSeg> unit)
        {
            unit.Clear();
        }

        protected override NativeList<TSeg> Create()
        {
            return new NativeList<TSeg>(Allocator.Persistent);
        }

        protected override void DisposeUnit(NativeList<TSeg> unit)
        {
            unit.Dispose();
        }
    }
}