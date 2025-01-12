using UnityEngine.Internal;

namespace AnyPath.Managed.Results
{
    /// <summary>
    /// Indicates wether a path is possible.
    /// </summary>
    [ExcludeFromDocs]
    public interface IEval
    {
        /// <summary>
        /// The total cost of the path
        /// </summary>
        float Cost { get; } 
        
        /// <summary>
        /// Indicates wether a path was found
        /// </summary>
        bool HasPath { get; }
    }
    
    /// <summary>
    /// Indicates wether a path to a target is possible.
    /// </summary>
    /// <typeparam name="TOption">The type of target</typeparam>
    [ExcludeFromDocs]
    public interface IEval<out TOption> : IEval
    {
        /// <summary>
        /// The target to which a path was found. If no path was found, this will be null or default.
        /// </summary>
        TOption Option { get; }
    }
}