using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    /// <summary>
    /// Indicates if a finder is mutable. Used by their internal FinderList's.
    /// </summary>
    [ExcludeFromDocs]
    public interface IMutableFinder
    {
        bool IsMutable { get; }
    }
    
    
    /// <summary>
    /// Used to 'retry' the target finder when needed
    /// </summary>
    [ExcludeFromDocs]
    public interface IRetryableFinder : IMutableFinder
    {
        void Clear(ClearFinderFlags flags);

        void OnRetryRun();
        void OnRetrySchedule();
        void OnNoRetry();
    }
}