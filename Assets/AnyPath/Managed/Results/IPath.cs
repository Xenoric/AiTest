using AnyPath.Native;
using UnityEngine.Internal;

namespace AnyPath.Managed.Results
{
    /// <summary>
    /// The result of a managed path finding operation. 
    /// </summary>
    /// <typeparam name="TSeg">The type of segments contained in the path</typeparam>
    /// <inheritdoc cref="IEval"/>
    [ExcludeFromDocs]
    public interface IPath<TSeg> : IEval, IPathSegments<TSeg>
    {
    }
    
    /// <summary>
    /// Allows for paths with different target types but inheriting from the same base class to be interchanged.
    /// </summary>
    /// <seealso cref="IFinder{TGraph,TResult}"/>
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
    /// <inheritdoc cref="IPath{TEdge}"/>
    [ExcludeFromDocs]
    public interface IPath<out TTarget, TSeg> : IPath<TSeg>, IEval<TTarget>
    {
    }
}