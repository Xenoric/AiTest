using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Indicates wether a path was found and contains the index and length of the path in the path buffer
    /// </summary>
    public struct AStarFindPathResult
    {
        /// <summary>
        /// The result of the evaluation
        /// </summary>
        public readonly AStarEvalResult evalResult;
        
        /// <summary>
        /// Contains the starting index and the length of the path in the path buffer
        /// </summary>
        public readonly OffsetInfo offsetInfo;
        
        [ExcludeFromDocs]
        public AStarFindPathResult(AStarEvalResult evalResult, int pathStartIndex, int pathLength)
        {
            this.evalResult = evalResult;
            this.offsetInfo = new OffsetInfo(pathStartIndex, pathLength);
        }

        [ExcludeFromDocs]
        public static readonly AStarFindPathResult NoPath = new AStarFindPathResult(AStarEvalResult.NoPath, -1, 0);
    }
}