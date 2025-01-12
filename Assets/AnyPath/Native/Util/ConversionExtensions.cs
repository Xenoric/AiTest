using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Native.Util
{
    /// <summary>
    /// Utility to convert between the math library's vector types and Unity's default vector types
    /// </summary>
    [ExcludeFromDocs]
    public static class ConversionExtensions
    {
        public static int2 ToInt2(this Vector2Int v)
        {
            return new int2(v.x, v.y);
        }

        public static int2 ToInt2(this Vector3Int v)
        {
            return new int2(v.x, v.y);
        }

        public static int2 RoundToInt2(this Vector3 v)
        {
            return new int2(round(v.ToFloat2()));
        }
        
        public static int2 RoundToInt2(this Vector2 v)
        {
            return new int2(round(v));
        }
        
        public static Vector3 ToVec3(this float2 v)
        {
            return new Vector3(v.x, v.y);
        }
        
        public static Vector3 ToVec3(this float2 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }
        
        public static float2 ToFloat2(this Vector3 v)
        {
            return new float2(v.x, v.y);
        }
        
        public static float2 ToFloat2(this Vector2 v)
        {
            return new float2(v.x, v.y);
        }

        public static Vector3Int ToVector3Int(this int2 v, int z = 0)
        {
            return new Vector3Int(v.x, v.y, z);
        }
    }
}