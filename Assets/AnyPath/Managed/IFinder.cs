using System.Collections;
using AnyPath.Managed.Finders.Common;
using UnityEngine.Internal;

namespace AnyPath.Managed
{
    /// <summary>
    /// Common Finder functionality.
    /// This can be used if you need interoperability between different finders and their results.
    /// </summary>
    /// <example>
    /// <code>
    /// public class IFinderExample
    /// {
    ///     class BaseTarget
    ///     {
    ///     }
    ///
    ///     class DerivedTarget : BaseTarget
    ///     {
    ///     }
    ///     
    ///     public void Test()
    ///     {
    ///         HexGridPathFinder pathFinderA = new HexGridPathFinder();
    ///         HexGridFirstTargetFinder&lt;BaseTarget&gt; pathFinderB = new HexGridFirstTargetFinder&lt;BaseTarget&gt;();
    ///         HexGridCheapestTargetFinder&lt;DerivedTarget&gt; pathFinderC = new HexGridCheapestTargetFinder&lt;DerivedTarget&gt;();
    ///         
    ///         pathFinderA.Completed += OnCompletedWithoutTarget;
    ///         pathFinderB.Completed += OnCompletedWithoutTarget;
    ///         pathFinderC.Completed += OnCompletedWithoutTarget;
    ///         
    ///         pathFinderB.Completed += OnCompletedWithTarget;
    ///         pathFinderC.Completed += OnCompletedWithTarget;
    ///     }
    ///     
    ///     private void OnCompletedWithoutTarget(IFinder&lt;HexGrid, Path&lt;HexGridEdge&gt;&gt; obj)
    ///     {
    ///         // compatible with A, B and C
    ///     }
    ///
    ///     private void OnCompletedWithTarget(IFinder&lt;HexGrid, IPath&lt;BaseTarget, HexGridEdge&gt;&gt; obj)
    ///     {
    ///         // using IPath with BaseTarget as the resut type makes this handler accept paths coming from both B and C
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IFinder
    {
        /// <summary>
        /// Indicates if the finder's last request was completed.
        /// </summary>
        bool IsCompleted { get; }
        
        /// <summary>
        /// Indicates if the values of this request can be modified. Once a request is sent, it becomes immutable
        /// until Clear() is called.
        /// </summary>
        bool IsMutable { get; }
        
        /// <summary>
        /// Clear this finder allowing for a new request to be made.
        /// </summary>
        /// <remarks>If the previous request was not completed yet, that request will be aborted.</remarks>
        /// <param name="flags">Flags that can be used to preserve certain settings on this finder</param>
        void Clear(ClearFinderFlags flags = ClearFinderFlags.ClearAll);



        /// <summary>
        /// Schedules this finder using Unity's Job System.
        /// </summary>
        /// <returns>An IEnumerator which can be used to suspend a coroutine while waiting for the result</returns>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        IEnumerator Schedule();

        /// <summary>
        /// Runs this finder's request immediately on the main thread.
        /// </summary>
        /// <returns>The result of the request</returns>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        void Run();
    }
    
    [ExcludeFromDocs]
    public interface IFinder<TGraph, TMod, out TResult> : IFinder<TGraph, TResult>, ISetFinderEdgeMod<TMod>
    {
        /// <summary>
        /// The settings for the request.
        /// </summary>
        /// <exception cref="ImmutableFinderException">This property can not be modified when the request is in flight</exception>
        int MaxExpand { get; set; }
    }
    
    [ExcludeFromDocs]
    public interface IFinder<TGraph, out TResult> : IFinder<TGraph>
    {
        /// <summary>
        /// The result of the last request on this finder.
        /// </summary>
        TResult Result { get; }
    }

    [ExcludeFromDocs]
    public interface IFinder<TGraph> : IFinder
    {
        /// <summary>
        /// The graph currently attached to this finder.
        /// </summary>
        TGraph Graph { get; set; }
    }
}