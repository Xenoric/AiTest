using UnityEngine.Internal;

namespace AnyPath.Native
{
    /// <summary>
    /// Indicates wether a path was found and contains the index of the target.
    /// </summary>
    public struct AStarEvalOptionResult
    {
        /// <summary>
        /// The index of the target to which a path was found. -1 if there was no path found/
        /// </summary>
        public readonly int targetIndex;
        
        /// <summary>
        /// The <see cref="AStarEvalResult"/>
        /// </summary>
        public readonly AStarEvalResult evalResult;

        [ExcludeFromDocs]
        public AStarEvalOptionResult(int targetIndex, AStarEvalResult evalResult)
        {
            this.targetIndex = targetIndex;
            this.evalResult = evalResult;
        }

        [ExcludeFromDocs]
        public static readonly AStarEvalOptionResult NoPath = new AStarEvalOptionResult(-1, AStarEvalResult.NoPath);
    }
}