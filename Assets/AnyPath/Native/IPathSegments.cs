using UnityEngine.Internal;

namespace AnyPath.Native
{
    [ExcludeFromDocs]
    public interface IPathSegments<TSeg>
    {
        /// <summary>
        /// Indexes the edges from the start to the end of the path.
        /// </summary>
        /// <param name="index">Index of the edge</param>
        TSeg this[int index] { get; }

        /// <summary>
        /// The amount of edges contained in the path
        /// </summary>
        int Length { get; }
    }
}