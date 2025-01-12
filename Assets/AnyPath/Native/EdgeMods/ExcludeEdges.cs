using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Native.EdgeMods
{
    /// <summary>
    /// Can be used to dynamically exclude certain edges from the graph without the need to rebuild it.
    /// </summary>
    /// <typeparam name="TNode"></typeparam>
    /// <remarks>
    /// <para>
    /// Note that this Edge Modifier uses a Native Container which means
    /// it needs to be disposed, just as graphs. Use the <see cref=" AnyPath.Managed.Disposal.ManagedDisposer.DisposeSafe{TGraph}"/> extension
    /// method for that.
    /// </para>
    /// </remarks>
    public struct ExcludeEdges<TNode> : IEdgeMod<TNode>, INativeDisposable 
        where TNode : unmanaged, IEquatable<TNode>
    {
        struct Pair : IEquatable<Pair>
        {
            public readonly TNode a;
            public readonly TNode b;
            public bool Equals(Pair other) => a.Equals(other.a) && b.Equals(other.b);
            public override int GetHashCode() => new int2(a.GetHashCode(), b.GetHashCode()).GetHashCode();
            public Pair(TNode a, TNode b)
            {
                this.a = a;
                this.b = b;
            }
        }
        
        private NativeHashSet<Pair> set;

        public ExcludeEdges(Allocator allocator, int capacity = 0)
        {
            set = new NativeHashSet<Pair>(capacity, allocator);
        }

        /// <summary>
        /// Exlcudes an edge to be traversed. Note that this is directional.
        /// </summary>
        /// <param name="from">The starting location.</param>
        /// <param name="to">The end of the edge.</param>
        public bool AddExclusion(TNode from, TNode to) => set.Add(new Pair(from, to));
        
        /// <summary>
        /// Clears all exclusions that were added.
        /// </summary>
        public void Clear() => set.Clear();

        
        [ExcludeFromDocs]
        public bool ModifyCost(in TNode from, in TNode to, ref float cost) => !set.Contains(new Pair(from, to));

        [ExcludeFromDocs]
        public void ModifyEdgeBuffer(in TNode from, ref NativeList<Edge<TNode>> edgeBuffer)
        {
        }

        [ExcludeFromDocs]
        public void Dispose() => set.Dispose();
        
        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps) => set.Dispose(inputDeps);
    }
}