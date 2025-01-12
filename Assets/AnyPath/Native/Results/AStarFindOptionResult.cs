using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Indicates wether a path was found, contains the index of the target and the index and length of the path in the buffer.
    /// </summary>
    public struct AStarFindOptionResult
    {
        /// <summary>
        /// The index of the option to which a path was found. -1 if there was no path.
        /// This index corresponds to the index in the <see cref="OffsetInfo"/> array provided to the request.
        /// </summary>
        public readonly int optionIndex;
        
        /// <summary>
        /// Information about the actual path
        /// </summary>
        public readonly AStarFindPathResult findPathResult;

        [ExcludeFromDocs]
        public AStarFindOptionResult(int optionIndex, AStarFindPathResult findPathResult)
        {
            this.optionIndex = optionIndex;
            this.findPathResult = findPathResult;
        }

        [ExcludeFromDocs]
        public static readonly AStarFindOptionResult NoPath = new AStarFindOptionResult(-1, AStarFindPathResult.NoPath);
    }
}