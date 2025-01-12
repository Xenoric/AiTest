using Unity.Collections;
using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Contains information about where a path starts and ends in an array.
    /// </summary>
    public struct OffsetInfo
    {
        /// <summary>
        /// The index at which this path starts
        /// </summary>
        public readonly int startIndex;
        
        /// <summary>
        /// The length of the path
        /// </summary>
        public readonly int length;

        [ExcludeFromDocs]
        public OffsetInfo(int startIndex, int lengh)
        {
            this.startIndex = startIndex;
            this.length = lengh;
        }
    }
    
    [ExcludeFromDocs]
    public static class OffsetInfoExtensions
    {
        public static NativeSlice<T> Slice<T>(this OffsetInfo info, NativeList<T> toSlice) where T : unmanaged
        {
            return new NativeSlice<T>(toSlice.AsArray(), info.startIndex, info.length);
        }
        
        public static NativeSlice<T> Slice<T>(this OffsetInfo info, NativeSlice<T> toSlice) where T : unmanaged
        {
            return new NativeSlice<T>(toSlice, info.startIndex, info.length);
        }
    }
}