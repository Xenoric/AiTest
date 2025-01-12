using System;
using Unity.Collections;

namespace AnyPath.Native
{
    public interface IEdgeMod<TNode> where TNode : unmanaged, IEquatable<TNode>
    {
        /// <summary>
        /// Allows for modification of the cost of a path segment, or totally discarding it as a possibility.
        /// This is called during the A* algorithm for every segment between locations A* encounters.
        /// </summary>
        /// <param name="from">The source node for this edge</param>
        /// <param name="to">The other end of the edge</param>
        /// <param name="cost">Modify this parameter to alter the cost of the segment. Be careful as to not make the cost lower than
        /// a value that the heuristic function of the graph would calculate, as this can result in sub optimal paths. For some graph types
        /// this may not be immediately noticable but for true graph structures, providing a lower cost than the heuristic may cause the path
        /// to contain detours that look strange.</param>
        /// <returns>When you return false, the edge is discarded as a possibility. This can be used for instance to simulate a closed door.</returns>
        /// <remarks>The default NoProcessing does not touch the cost and returns true. Keeping the original graph as is.
        /// The burst compiler is smart enough to detect this and totally discard this method</remarks>
        bool ModifyCost(in TNode from, in TNode to, ref float cost);

        /// <summary>
        /// <para>
        /// Allows for modifying the edge buffer after it is filled by the <see cref="IGraph{TNode}.Collect"/> implementation.
        /// This is called every time the algorithm visits a node and right after the default graph implementation has filled it.
        /// Leave this method empty if you don't need it. The Burst compiler will strip the code away and this is no impact on performance.
        /// </para>
        /// <para>
        /// This can be used to create "portals" that aren't part of the default graph. Here's an example for the square grid:
        /// <code>
        /// private SquareGridCell portalFrom;
        /// private SquareGridCell portalTo;
        /// public void ModifyEdgeBuffer(in SquareGridCell from, ref NativeList{Edge{SquareGridCell}} edgeBuffer)
        /// {
        ///    // One portal, but we could also use a hashset to match against many.
        ///    // If we're on the portal position, add an edge/link to the portal destination.
        ///    if (from.Equals(portalFrom))
        ///        edgeBuffer.Add(new Edge{SquareGridCell}(portalTo, cost: 1));
        ///}
        /// </code>
        /// </para>
        /// <para>
        /// You can use the generic readymade <see cref="EdgeMods.AdditionalEdges{TNode}"/> modifier for this use case which uses a hashmap internally.
        /// If your use case is more specific, there may be more better ways to do it (as the one illustrated above, where there is only one portal).
        /// </para>
        /// <para>
        /// You can also add other dynamic stuff, or remove edges from the default implementation.
        /// While you can remove, it may be more performant to use <see cref="ModifyCost"/> and return false to discard an edge.
        /// </para>
        /// </summary>
        /// <param name="from">
        /// The node the algorithm is currently at. Similar to <see cref="IGraph{TNode}.Collect"/>.
        /// </param>
        /// <param name="edgeBuffer">Pre filled list containing the edges that originate from <see cref="from"/>.</param>
        /// <remarks>This can be called multiple times for the same <see cref="from"/> node. It should act
        /// the same every time within a single pathfinding query.</remarks>
        void ModifyEdgeBuffer(in TNode from, ref NativeList<Edge<TNode>> edgeBuffer);
    }

    /// <summary>
    /// Default edge mod that does nothing
    /// </summary>
    public struct NoEdgeMod<TNode> : IEdgeMod<TNode> where TNode : unmanaged, IEquatable<TNode>
    {
        public bool ModifyCost(in TNode @from, in TNode to, ref float cost) => true;
        public void ModifyEdgeBuffer(in TNode node, ref NativeList<Edge<TNode>> edgeBuffer) { }
    }
}