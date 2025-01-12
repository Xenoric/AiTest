namespace AnyPath.Examples
{
    public enum GoalFindMethod
    {
        /// <summary>
        /// Snakes use the PriorityFinder to select which goal to find a path to
        /// </summary>
        Priority,
        
        /// <summary>
        /// Snakes use the CheapestOptionFinder to select which goal to find a path to
        /// </summary>
        Cheapest,
        
        /// <summary>
        /// Snakes use the OptionFinder to select a goal to find a path to
        /// </summary>
        Any
    }
}