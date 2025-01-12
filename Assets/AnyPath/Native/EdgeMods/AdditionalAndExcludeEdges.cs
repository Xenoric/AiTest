using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Internal;

namespace AnyPath.Native.EdgeMods
{
    /// <summary>
    /// Combines <see cref="AdditionalEdges{TNode}"/> and <see cref="ExcludeEdges{TNode}"/> into one modifier.
    /// </summary>
    public struct AdditionalAndExcludeEdges<TNode> : IEdgeMod<TNode>, INativeDisposable
        where TNode : unmanaged, IEquatable<TNode>
    {
        private AdditionalEdges<TNode> additionalEdges;
        private ExcludeEdges<TNode> excludeEdges;

        public AdditionalAndExcludeEdges(Allocator allocator)
        {
            additionalEdges = new AdditionalEdges<TNode>(allocator);
            excludeEdges = new ExcludeEdges<TNode>(allocator);
        }

        [ExcludeFromDocs]
        public bool ModifyCost(in TNode from, in TNode to, ref float cost) => excludeEdges.ModifyCost(from, to, ref cost);
        
        [ExcludeFromDocs]
        public void ModifyEdgeBuffer(in TNode from, ref NativeList<Edge<TNode>> edgeBuffer) =>
            additionalEdges.ModifyEdgeBuffer(from, ref edgeBuffer);

        /// <summary>
        /// Adds an additional edge. <see cref="AdditionalEdges{TNode}.AddEdge"/>
        /// </summary>
        public void AddAdditionalEdge(TNode from, TNode to, float cost) => additionalEdges.AddEdge(from, to, cost);
        
        /// <summary>
        /// Adds en exclusion edge. <see cref="ExcludeEdges{TNode}.AddExclusion"/>
        /// </summary>
        public void AddExclusion(TNode from, TNode to) => excludeEdges.AddExclusion(from, to);
        
        /// <summary>
        /// Clears all additional and excluded edges.
        /// </summary>
        public void Clear()
        {
            additionalEdges.Clear();
            excludeEdges.Clear();
        }

        [ExcludeFromDocs]
        public void Dispose()
        {
            additionalEdges.Dispose();
            excludeEdges.Dispose();
        }

        [ExcludeFromDocs]
        public JobHandle Dispose(JobHandle inputDeps) =>
            JobHandle.CombineDependencies(additionalEdges.Dispose(inputDeps), excludeEdges.Dispose(inputDeps));
    }
}