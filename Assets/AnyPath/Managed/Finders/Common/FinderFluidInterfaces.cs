using System;
using System.Collections.Generic;
using UnityEngine.Internal;

namespace AnyPath.Managed.Finders.Common
{
    /*
     * Interfaces allowing fluid style extensions on finders.
     *
     * All fluid interfaces start with ISetFinder... or IGetFinder... to clarify the distinction between
     * other interfaces.
     */

    [ExcludeFromDocs]
    public interface ISetFinderPathProcessor<TProc>
    {
        TProc PathProcessor { get; set; }
    }
    
    [ExcludeFromDocs]
    public interface ISetFinderEdgeMod<TMod>
    {
        TMod EdgeMod { get; set; }
    }

    [ExcludeFromDocs]
    public interface ISetFinderHeuristicProvider<TH>
    {
        TH HeuristicProvider { get; set; }
    }

    [ExcludeFromDocs]
    public interface IGetFinderOptions<in TOption, TNode>
    {
        IFinderOptions<TOption, TNode> Options { get; }
    }

    [ExcludeFromDocs]
    public interface IGetFinderMultiRequests<TNode> 
    {
        IAddMulti<TNode> Requests { get; }
    }

    [ExcludeFromDocs]
    public interface ISetFinderOptionValidator<out TOption>
    {
        IOptionValidator<TOption> Validator { set; }
    }
    
    [ExcludeFromDocs]
    public interface ISetFinderOptionReserver<out TOption>
    {
        IOptionReserver<TOption> Reserver { set; }
    }
    
    [ExcludeFromDocs]
    public interface ISetFinderOptionComparer<out TOption>
    {
        IComparer<TOption> Comparer { set; }
    }

    [ExcludeFromDocs]
    public interface ISetFinderMaxRetries
    {
        int MaxRetries { set; }
    }

    [ExcludeFromDocs]
    public interface IGetFinderStops<TNode>
    {
        IFinderStops<TNode> Stops { get; }
    }

    [ExcludeFromDocs]
    public interface ISetFinderCompleted<TGraph, out TResult>
    {
        event Action<IFinder<TGraph, TResult>> Completed;
    }
}