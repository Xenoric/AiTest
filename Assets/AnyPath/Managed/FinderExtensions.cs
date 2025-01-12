using System;
using System.Collections.Generic;
using AnyPath.Managed.Finders.Common;
using AnyPath.Native;

namespace AnyPath.Managed
{
    /// <summary>
    /// Fluid syntax style extension methods for finders.
    /// </summary>
    public static class FinderExtensions
    {
        /// <summary>
        /// Sets a single starting node for the requests.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If stops were added before a call to SetStart, they will be cleared.
        /// </para>
        /// <para>
        /// This is a convenience method. It is also possible to just use <see cref="AddStop{T,TNode}"/>, as a single stop will act as
        /// a starting node.
        /// </para>
        /// </remarks>
        /// <param name="finder">The finder</param>
        /// <param name="start">The starting node of the request</param>
        /// <param name="goal">The goal node of the request</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <returns>The finder that will perform the request</returns>
        public static T SetStartAndGoal<T, TNode>(this T finder, TNode start, TNode goal) where T : IGetFinderStops<TNode> where TNode : unmanaged, IEquatable<TNode>
        {
            if (finder.Stops.Count > 0)
                throw new InvalidOperationException("Setting a start and goal may only be done when there are no stops added yet.");
            
            finder.Stops.Add(start);
            finder.Stops.Add(goal);
            return finder;
        }

        
        /// <summary>
        /// Sets the graph to perform the request on.
        /// </summary>
        /// <param name="finder">The finder that performs the request</param>
        /// <param name="graph">The graph to perform the request on</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TGraph">Type of graph</typeparam>
        /// <returns>The finder</returns>
        public static T SetGraph<T, TGraph>(this T finder, TGraph graph) where T : IFinder<TGraph> where TGraph : IGraph
        {
            finder.Graph = graph;
            return finder;
        }
        
        /// <summary>
        /// Adds a target with it's location to a TargetFinder
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="option">The target object</param>
        /// <param name="start">The starting location in the graph</param>
        /// <param name="goal">The goal location in the graph</param>
        /// <typeparam name="T">The finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <typeparam name="TOption">Type of target</typeparam>
        /// <returns>The finder</returns>
        /// <remarks>
        /// Altough a location has to be provided to a target, the target can be an arbitrary type and can contain arbitrary data.
        /// </remarks>
        public static T AddOption<T, TNode, TOption>(this T finder, TOption option, TNode start, TNode goal) 
            where T : IGetFinderOptions<TOption, TNode>
            where TNode : unmanaged, IEquatable<TNode>
        {
            finder.Options.Add(option, start, goal);
            return finder;
        }

        
        /// <summary>
        /// Adds multiple targets to a TargetFinder
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="start">The shared starting location in the graph. Each option will start from here.</param>
        /// <param name="options">The options</param>
        /// <param name="optionToLocation">Function providing a goal location for each option</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <typeparam name="TOption">User defined option type.</typeparam>
        /// <returns>The finder</returns>
        public static T AddOptions<T, TNode, TOption>(this T finder, IEnumerable<TOption> options, TNode start, Func<TOption, TNode> optionToLocation) 
            where T : IGetFinderOptions<TOption, TNode>
            where TNode : unmanaged, IEquatable<TNode>
        {
            foreach (var target in options)
                finder.Options.Add(target, start, optionToLocation(target));

            return finder;
        }
        
        /// <summary>
        /// Adds a range of options to an IOptions
        /// </summary>
        /// <param name="finderOptions">The options component of the minder</param>
        /// <param name="start">The shared starting location in the graph. Each option will start from here.</param>
        /// <param name="options">The options</param>
        /// <param name="optionToLocation">Function providing a goal location for each option</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <typeparam name="TOption">User defined option type.</typeparam>
        /// <returns>The IFinderOptions the options were added to</returns>
        public static T AddRange<T, TNode, TOption>(this T finderOptions, IEnumerable<TOption> options, TNode start, Func<TOption, TNode> optionToLocation) 
            where T : IFinderOptions<TOption, TNode>
            where TNode : unmanaged, IEquatable<TNode>
        {
            foreach (var target in options)
                finderOptions.Add(target, start, optionToLocation(target));

            return finderOptions;
        }
        
        /// <summary>
        /// Adds a stop to the request.
        /// </summary>
        /// <remarks>
        /// Stops are processed in the order they are added. If only one stop is added, it functions as a starting position.
        /// </remarks>
        /// <param name="finder">The finder</param>
        /// <param name="stop">The location of the stop</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <returns>The finder</returns>
        public static T AddStop<T, TNode>(this T finder, TNode stop) 
            where T : IGetFinderStops<TNode>
            where TNode : unmanaged, IEquatable<TNode>
        {
            finder.Stops.Add(stop);
            return finder;
        }
        
        /// <summary>
        /// Adds multiple stops to the request.
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="stops">Stops to add, in order</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <returns>The finder</returns>
        public static T AddStops<T, TNode>(this T finder, IEnumerable<TNode> stops) 
            where T : IGetFinderStops<TNode>
            where TNode : unmanaged, IEquatable<TNode>
        {
            foreach (var stop in stops)
                finder.AddStop(stop);
            
            return finder;
        }

        /// <summary>
        /// Adds a request to a MultiFinder
        /// </summary>
        /// <remarks>
        /// A multifinder evaluates all of the requests that are added in a single job.
        /// </remarks>
        /// <param name="finder">The multi finder</param>
        /// <param name="start">The starting location</param>
        /// <param name="goal">The goal location</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <returns>The finder</returns>
        public static T AddRequest<T, TNode>(this T finder, TNode start, TNode goal)
            where T : IGetFinderMultiRequests<TNode>
            where TNode : unmanaged, IEquatable<TNode>
        {
            finder.Requests.Add(start, goal);
            return finder;
        }
        
        /// <summary>
        /// Adds a request with multiple stops to a MultiFinder
        /// </summary>
        /// <param name="finder">The multi finder</param>
        /// <param name="stops">The starting location and stops in order</param>
        /// <param name="goal">The goal location</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TNode">Type of node</typeparam>
        /// <returns>The finder</returns>
        public static T AddRequests<T, TNode>(this T finder, IEnumerable<TNode> stops)

            where T : IGetFinderMultiRequests<TNode>
            where TNode : unmanaged, IEquatable<TNode>

        {
            finder.Requests.Add(stops);
            return finder;
        }

        /// <summary>
        /// Sets a path processor for a request
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="processor">The processor struct <see cref="IPathProcessor{TNode,TSeg}"/></param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TProc">Type of processor</typeparam>
        /// <returns></returns>
        public static T SetPathProcessor<T, TProc>(this T finder, TProc processor) 
            where T : ISetFinderPathProcessor<TProc>
            where TProc : struct
        {
            finder.PathProcessor = processor;
            return finder;
        }

        
        /// <summary>
        /// Sets an edge modifier for a request. This can be used to exclude or modify edge cost's without updating the entire graph.
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="mod">The <see cref="IEdgeMod{TNode}"/> to use.</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TMod">Type of modifier</typeparam>
        /// <returns></returns>
        public static T SetEdgeMod<T, TMod>(this T finder, TMod mod) 
            where T : ISetFinderEdgeMod<TMod>
            where TMod : struct
        {
            finder.EdgeMod = mod;
            return finder;
        }
        
        /// <summary>
        /// Sets a heuristic provider for this request.
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="provider">The heuristic provider</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TH">Type of heuristic provider</typeparam>
        /// <returns></returns>
        public static T SetHeuristicProvider<T, TH>(this T finder, TH provider) 
            where T : ISetFinderHeuristicProvider<TH>
            where TH : struct
        {
            finder.HeuristicProvider = provider;
            return finder;
        }
        
        /// <summary>
        /// Sets a validator for a TargetFinder. See <see cref="IOptionValidator{TTarget}"/> for details.
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="validator">The validator</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TOption">Type of option</typeparam>
        /// <returns>The finder</returns>
        public static T SetValidator<T, TOption>(this T finder, IOptionValidator<TOption> validator) where T : ISetFinderOptionValidator<TOption>
        {
            finder.Validator = validator;
            return finder;
        }
        
        /// <summary>
        /// Sets a reserver for a TargetFinder. See <see cref="IOptionReserver{TTarget}"/> for details.
        /// </summary>
        /// <param name="finder">The finder</param>
        /// <param name="reserver">The reserver</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TOption">Type of option</typeparam>
        /// <returns>The finder</returns>
        public static T SetReserver<T, TOption>(this T finder, IOptionReserver<TOption> reserver) where T : ISetFinderOptionReserver<TOption>
        {
            finder.Reserver = reserver;
            return finder;
        }
        
        /// <summary>
        /// Sets a comparer to prioritize options for a PriorityOptionFinder.
        /// </summary>
        /// <param name="finder">The priority finder</param>
        /// <param name="comparer">Your comparer</param>
        /// <typeparam name="T">Type of finder</typeparam>
        /// <typeparam name="TOption">Type of option</typeparam>
        /// <returns>The finder</returns>
        public static T SetComparer<T, TOption>(this T finder, IComparer<TOption> comparer) where T : ISetFinderOptionComparer<TOption>
        {
            finder.Comparer = comparer;
            return finder;
        }

        /// <summary>
        /// Sets how many times the request may be retried if the validation of an option fails after a path has been found to it.
        /// Options that did not pass validation will be removed as a possibility for the next retry.
        /// </summary>
        public static T SetMaxRetries<T>(this T finder, int maxRetries) where T : ISetFinderMaxRetries
        {
            finder.MaxRetries = maxRetries;
            return finder;
        }
    }
}