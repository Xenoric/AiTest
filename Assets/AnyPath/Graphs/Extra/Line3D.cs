using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace AnyPath.Graphs.Extra
{
    /// <summary>
    /// 3D line segment used by the WaypointGraph
    /// </summary>
    public struct Line3D
    {
        /// <summary>
        /// Endpoint of the line segment
        /// </summary>
        public float3 a { get; set; }
        
        /// <summary>
        /// Endpoint of the line segment
        /// </summary>
        public float3 b { get; set; }
        
        public Line3D(float3 a, float3 b)
        {
            this.a = a;
            this.b = b;
        }

        /// <summary>
        /// Shorthand for the center of the line
        /// </summary>
        public float3 Center => .5f * (a + b);

        public float3 GetDirection() => math.normalize(b - a);

        public float GetLength() => math.distance(a, b);

        
        /// <summary>
        /// Returns the closest point on this line from a point
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetClosestPoint(float3 pos)
        {
            return math.lerp(a, b, GetClosestPositionT(pos));
        }
        
        /// <summary>
        /// Returns a value between 0 and 1 describing how far along the line the closest point on this line is to a given point 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetClosestPositionT(float3 pos)
        {
            float3 ba = b - a;
            return math.saturate(math.dot(pos - a, ba) / math.dot(ba, ba));
        }
    }
}