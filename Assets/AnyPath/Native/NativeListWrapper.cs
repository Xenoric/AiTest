using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Wrapper struct to allow SSFA.GetDirection to be used in native context. NativeList does not implement IReadOnlyList or some other interface
    /// that provides a read only indexer and Length, so wrapping a NativeList containing the path within this struct allows
    /// for using the same code for the managed version as well as on a raw NativeList.
    /// </summary>
    /// <typeparam name="TSeg">The type of path segment. Currently this is only used for UnrolledNavMeshGraphPortal</typeparam>
    /// <remarks>The same pattern can be used for DynamicBuffer if you use ECS. The code is provided but needs to be uncommented in NativeListWraper.cs</remarks>
    public struct NativeListWrapper<TSeg> : IPathSegments<TSeg>
        where TSeg : unmanaged
    {
        [ReadOnly] private NativeList<TSeg> list;

        [ExcludeFromDocs]
        public NativeListWrapper(NativeList<TSeg>  list)
        {
            this.list = list;
        }

        [ExcludeFromDocs] public TSeg this[int index] => list[index];
        [ExcludeFromDocs] public int Length => list.Length;
    }
    
    // Uncomment code below if you use ECS with DynamicBuffer
    
    /*
    
    public struct DynamicBufferWrapper<TSeg> : IPathSegments<TSeg>
        where TSeg : unmanaged
    {
        [ReadOnly] private DynamicBuffer<TSeg> list;

        public DynamicBufferWrapper(DynamicBuffer<TSeg>  list)
        {
            this.list = list;
        }

        public TSeg this[int index] => list[index];
        public int Length => list.Length;
    }
    
    */
}