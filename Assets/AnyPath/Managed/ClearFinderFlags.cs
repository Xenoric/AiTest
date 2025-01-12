using System;

namespace AnyPath.Managed
{
    /// <summary>
    /// Flags used to determine what data to keep when clearing a finder.
    /// </summary>
    [Flags]
    public enum ClearFinderFlags
    {
        /// <summary>
        /// Clears all data stored in the finder. This is the default value.
        /// </summary>
        ClearAll = 0,
        
        /// <summary>
        /// Preserve the graph struct on the finder.
        /// </summary>
        KeepGraph = 1,

        
        /// <summary>
        /// Preserve the heuristic provider
        /// </summary>
        KeepHeuristicProvider = 1 << 2,
        
        /// <summary>
        /// Preserve the edge modifier
        /// </summary>
        KeepEdgeMod = 1 << 3,
        
        /// <summary>
        /// Preserve the Completed event handlers
        /// </summary>
        KeepCompletedEventHandlers = 1 << 4,
        
        /// <summary>
        /// Keep all start and goal nodes. On target finders, this includes the target objects.
        /// </summary>
        KeepNodes = 1 << 5,
        
        /// <summary>
        /// Keep the validator used to validate the target objects on target finders.
        /// </summary>
        KeepValidator = 1 << 6,
        
        /// <summary>
        /// Keep the reserver
        /// </summary>
        KeepReserver = 1 << 7,

        /// <summary>
        /// Keep the comparer
        /// </summary>
        KeepComparer = 1 << 8,
       
        /// <summary>
        /// Preserves the graph and the heuristic provider
        /// </summary>
        KeepGraphAndHeuristicProvider = KeepGraph | KeepHeuristicProvider,
        
        /// <summary>
        /// Preserves all data except for the last stored result.
        /// </summary>
        KeepAll = KeepGraph | KeepHeuristicProvider | KeepEdgeMod | KeepCompletedEventHandlers | KeepNodes | KeepValidator | KeepReserver,
        
        /// <summary>
        /// Useful for First/Cheapest/Priority finders
        /// </summary>
        KeepValidatorReserverAndComparer = KeepValidator | KeepReserver | KeepComparer
    }
}