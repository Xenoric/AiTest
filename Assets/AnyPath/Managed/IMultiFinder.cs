using AnyPath.Managed.Finders.Common;
using UnityEngine.Internal;

namespace AnyPath.Managed
{
    /// <summary>
    /// Interface inheriting from <see cref="IFinder{TGraph,TResult}"/> to make the default MultiPathFinder and MultiPathEvaluator interchangeable.
    /// Provides functionality for adding sub requests, allowing to build and schedule a full request.
    /// </summary>
    /// <typeparam name="TGraph">The graph type</typeparam>
    /// <typeparam name="TNode">Type of nodes</typeparam>
    /// <typeparam name="TResult">The type of result.</typeparam>
    [ExcludeFromDocs]
    public interface IMultiFinder<TGraph, TNode, out TResult> : IFinder<TGraph, TResult>, IGetFinderMultiRequests<TNode>
    {
    }
}