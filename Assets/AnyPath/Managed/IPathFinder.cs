using AnyPath.Managed.Finders.Common;
using UnityEngine.Internal;

namespace AnyPath.Managed
{
    /// <summary>
    /// Interface inheriting from <see cref="IFinder{TGraph,TResult}"/> to make PathFinder and PathEvaluator interchangeable.
    /// Provides functionality for adding stops, allowing to build and schedule a full request.
    /// </summary>
    /// <typeparam name="TGraph">The graph type</typeparam>
    /// <typeparam name="TNode">Type of nodes</typeparam>
    /// <typeparam name="TResult">The type of result.</typeparam>
    [ExcludeFromDocs]
    public interface IPathFinder<TGraph, TNode, out TResult> : IFinder<TGraph, TResult>, IGetFinderStops<TNode>
    {

    }
}