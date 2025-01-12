using System;
using AnyPath.Native;
using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Managed.Results
{
    /// <summary>
    /// Contains the path as well as all segment A* expanded to, which can be useful when benchmarking the quality of your heuristic function.
    /// Ideally, you want A* to visit as little nodes as possible for performance.
    /// </summary>
    [ExcludeFromDocs]
    public class DebugPath<TNode, TSeg> : Path<TSeg>
        where TNode : unmanaged, IEquatable<TNode>
        where TSeg : unmanaged
    {
        /// <summary>
        /// All of the nodes A* expanded into. This can be useful to determine the quality of your heuristic function.
        /// Ideally, you want A* to visit as little nodes as possible for performance.
        /// </summary>
        public TNode[] AllExpanded { get; private set; }

        [ExcludeFromDocs]
        public DebugPath(AStarFindPathResult aStarResult, NativeList<TSeg> resultBuffer, ref AStar<TNode> aStar) : base(aStarResult, resultBuffer)
        {
            using (var expanded = aStar.cameFrom.GetKeyArray(Allocator.Temp))
                this.AllExpanded = expanded.ToArray();
        }
    }
}