using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Indicates wether a path was found
    /// </summary>
    public struct AStarEvalResult
    {
        /// <summary>
        /// Was a path found?
        /// </summary>
        public readonly bool hasPath;
        
        /// <summary>
        /// The cost of the found path. Zero if none was found.
        /// </summary>
        public readonly float cost;

        [ExcludeFromDocs]
        public AStarEvalResult(bool hasPath, float cost)
        {
            this.hasPath = hasPath;
            this.cost = cost;
        }

        public static readonly AStarEvalResult NoPath = new AStarEvalResult(false, 0);
    }
}