using AnyPath.Managed.Disposal;
using AnyPath.Native;

namespace AnyPath.Managed
{
    /// <summary>
    /// Provides extension methods to safely dispose of graph structs without having to worry about jobs that are still running on the
    /// graph.
    /// </summary>
    public static class ManagedDisposeExtensions
    {
        /// <summary>
        /// Disposes a graph struct with all of it's native containers.
        /// AnyPath internally keeps track of all of the active jobs that use the graph and will make sure the graph is only truly
        /// disposed when all of these jobs are finished.
        /// </summary>
        /// <param name="graph">The graph to dispose</param>
        /// <typeparam name="TGraph">The type of graph to dispose</typeparam>
        /// <remarks>Even though the graph is disposed after all of the jobs that operate on it are completed, you should take caution
        /// in accessing disposed graphs on finders that still exist and have the disposed graph set on it.
        /// </remarks>
        public static void DisposeGraph<TGraph>(this TGraph graph) where TGraph : IGraph =>
            ManagedDisposer.DisposeSafe(graph);
        
        /// <summary>
        /// Disposes an edge modifier with all of it's native containers. Waits until all of the jobs that use this modifier are finished
        /// before actually disposing. See <see cref="DisposeGraph{TGraph}"/> for more details.
        /// </summary>
        /// <param name="processor">The modifier that should be disposed</param>
        /// <typeparam name="TProc">The type of modifier</typeparam>
        /// <remarks>
        /// An edge modifier only needs to be disposed when it contains NativeContainers. If your modifier only uses
        /// unmanaged types, disposing is not neccessary
        /// </remarks>
        // public static void DisposeProcessor<TProc>(this TProc processor) where TProc : IDisposablePathProcessor =>
        //     ManagedDisposer.DisposeSafe(processor);
    }
}