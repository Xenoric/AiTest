using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Native.EdgeMods
{
    /// <summary>
    /// Can be used to dynamically exclude certain locations from the graph without the need to rebuild it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Note that this Edge Modifier uses a Native Container which means
    /// it needs to be disposed, just as graphs. Use the <see cref=" AnyPath.Managed.Disposal.ManagedDisposer.DisposeSafe{TGraph}"/> extension
    /// method for that.
    /// </para>
    /// </remarks>
    public struct ExcludeLocations<TNode> : IEdgeMod<TNode>, INativeDisposable 
        where TNode : unmanaged, IEquatable<TNode>
    {
        private NativeHashSet<TNode> set;

        public ExcludeLocations(Allocator allocator, int capacity = 0)
        {
            set = new NativeHashSet<TNode>(capacity, allocator);
        }

        /// <summary>
        /// Exlcudes a location to be traversed.
        /// </summary>
        public bool AddExclusion(TNode location) => set.Add(location);
        
        /// <summary>
        /// Clears all exclusions that were added.
        /// </summary>
        public void Clear() => set.Clear();

        
        [ExcludeFromDocs]
        public bool ModifyCost(in TNode from, in TNode to, ref float cost) => !set.Contains(to);

        [ExcludeFromDocs]
        public void ModifyEdgeBuffer(in TNode from, ref NativeList<Edge<TNode>> edgeBuffer) { }

        [ExcludeFromDocs]
        public void Dispose() => set.Dispose();
        
        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps) => set.Dispose(inputDeps);
    }
}