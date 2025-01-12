using AnyPath.Managed.Finders.Common;
using UnityEngine.Internal;

namespace AnyPath.Managed
{
    /// <summary>
    /// Interface inheriting from <see cref="IFinder{TGraph,TResult}"/> to make the default Option finders interchangeable.
    /// Provides functionality for adding options, a validator and a reserver, allowing to build and schedule a full request.
    /// Option finders are:
    /// <list type="bullet">
    /// <item>
    /// <term>
    /// OptionFinder/Evaluator
    /// </term>
    /// <description>
    /// Searches for the first option that passes validation and has a path.
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// CheapestOptionFinder/Evaluator
    /// </term>
    /// <description>
    /// Finds the cheapest path among a set of options
    /// </description>
    /// </item>
    /// <item>
    /// <term>
    /// PriorityOptionFinder/Evaluator
    /// </term>
    /// <description>
    /// Evaluates different the options based on a priority and returns the prioritized option that passes validation and has a path.
    /// </description>
    /// </item>
    /// </list>
    /// </summary>
    /// <typeparam name="TOption">The user defined type of option</typeparam>
    /// <typeparam name="TGraph">The graph type</typeparam>
    /// <typeparam name="TNode">Type of nodes</typeparam>
    /// <typeparam name="TResult">The type of result.</typeparam>
    [ExcludeFromDocs]
    public interface IOptionFinder<TOption, TGraph, TNode, out TResult> : 
        IFinder<TGraph, TResult>,  
        IGetFinderOptions<TOption, TNode>,
        ISetFinderOptionReserver<TOption>, 
        ISetFinderOptionValidator<TOption>
    {
    }
}