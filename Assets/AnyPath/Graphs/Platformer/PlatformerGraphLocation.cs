using System;
using AnyPath.Graphs.Extra;
using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Graphs.PlatformerGraph
{
    /// <summary>
    /// Represents a location on the platformer graph. A location can be retrieved by calling the Raycast or Closest functions on the PlatformerGraph.
    /// A location can be used as a start/stop/goal in a path request.
    /// This same struct is also used as the path segment type after processing. This is done because the flags may contain information your agent
    /// needs while navigating through your world, and for performance reasons because a big chunk of the path can just be memcpyd'.
    /// </summary>
    public struct PlatformerGraphLocation : IEquatable<PlatformerGraphLocation>, INodeFlags
    {
        /// <summary>
        /// The index of the edge this location is at
        /// </summary>
        public readonly int edgeIndex;
        
        /// <summary>
        /// The (optional) id of the edge this location is at
        /// </summary>
        public readonly int edgeId;
            
        /// <summary>
        /// The flags of the edge this location is at
        /// </summary>
        public int Flags { get; }
        
        /// <summary>
        /// A value between 0 and 1 that describes how far along the <see cref="line"/> the position is.
        /// </summary>
        public float PositionT { get; set; }

        /// <summary>
        /// The exact position on the edge.
        /// </summary>
        public float2 Position => math.lerp(line.a, line.b, PositionT);

        /// <summary>
        /// The line segment that describes the edge the location was sampled on. For directed edges, the direction of the edge
        /// is from A to B. For undirected edges, the edge also goes from B to A.
        /// </summary>
        /// <remarks>
        /// This value needs to be stored within the location to reconstruct the direction of travel for the first segment after the path
        /// has been found.</remarks>
        public Line2D line;

        /// <summary>
        /// The starting position for this edge. 
        /// This value is only meaningful as part of a pathfinding result.
        /// </summary>
        public float2 EnterPosition => line.a;

        /// <summary>
        /// The end position for this edge. This will be the same as the starting position of the next edge in the path.
        /// This value is only meaningful as part of a pathfinding result.
        /// </summary>
        public float2 ExitPosition => line.b;
            
        [ExcludeFromDocs] public bool Equals(PlatformerGraphLocation other) => this.edgeIndex == other.edgeIndex;
        [ExcludeFromDocs] public override int GetHashCode() => edgeIndex.GetHashCode();

        [ExcludeFromDocs]
        public PlatformerGraphLocation(int edgeIndex, int edgeId, int flags, Line2D line, float positionT)
        {
            this.edgeIndex = edgeIndex;
            this.edgeId = edgeId;
            this.Flags = flags;
            this.line = line;
            this.PositionT = positionT;
        }

        [ExcludeFromDocs]
        public PlatformerGraphLocation(int edgeIndex, int edgeId, int flags, Line2D line)
        {
            this.edgeIndex = edgeIndex;
            this.edgeId = edgeId;
            this.Flags = flags;
            this.line = line;
            this.PositionT = .5f;
        }
    }
}