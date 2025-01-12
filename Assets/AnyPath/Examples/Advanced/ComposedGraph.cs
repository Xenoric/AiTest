using System;
using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Native
{
    // This is an example of how you could combine several graphs as 'sections'
    // where each section could be updated without affecting other sections
    // you would keep the section graphs as separate containers somewhere else, and run the pathfinding query
    // on this struct, which doesn't allocate the containers by itself
    // the downside is, you'll need to hardcode the amount of sections contained because there is no way to have
    // an array of NativeContainers in burst compiled code

    public struct ComposedGraph<TGraph, TNode> : IGraph<TNode>
        where TGraph : struct, IGraph<TNode>
        where TNode : unmanaged, IEquatable<TNode>
    {
        public TGraph section1;
        public TGraph section2;
        public TGraph section3;
        public TGraph section4;

        public void Collect(TNode node, ref NativeList<Edge<TNode>> edgeBuffer)
        {
            // let each section decide if they have edges coming from this node
            // e.g. if the current node was in section 1 and section 2 is connected to it, section 2 will add a node and so on
            section1.Collect(node, ref edgeBuffer);
            section2.Collect(node, ref edgeBuffer);
            section3.Collect(node, ref edgeBuffer);
            section4.Collect(node, ref edgeBuffer);
        }
        
        // we don't need any disposal here but rather the individual graph sections themselves should be disposed of
        public void Dispose()
        {
        }

        public JobHandle Dispose(JobHandle inputDeps) => inputDeps;
    }

    /*
    public class Test
    {
        // This can run as part of a burst compiled job, or in an ECS system
        public static void FindPathAndStoreInDynamicBuffer(ref SquareGrid grid, SquareGridCell start, SquareGridCell goal, ref DynamicBuffer<SquareGridCell> path)
        {
            // these could also be persistent in the system or job to prevent reallocation
            var aStar = new AStar<SquareGridCell>(Allocator.Temp);
            var tempPathBuffer = new NativeList<SquareGridCell>(128, Allocator.Temp);

            var result = aStar.FindPath(ref grid, start, goal,
                default(SquareGridHeuristicProvider),
                default(NoEdgeMod<SquareGridCell>),
                default(NoProcessing<SquareGridCell>), tempPathBuffer);

            if (!result.evalResult.hasPath)
                return;

            // copy the path to the DynamicBuffer. This uses a memcpy internally so it's very fast
            path.CopyFrom(tempPathBuffer);
        }
        
        public static void AnotherWay(NavMeshGraph graph, NativeList<NavMeshGraphLocation> stops, ref NativeList<float3> path)
        {
            // As an alternative to using the static AStar functions which require a lot of type parameters,
            // we can leverage the managed finder's Job struct to make our native query more readable.
            // we use the job struct and directly call Execute on it.
            // the NavMeshGraphPathFinder code can be generated using the code generator
        
            var job = new NavMeshGraphPathFinder.Job()
            {
                graph = graph,
                stops = stops,
                aStar = new AStar<NavMeshGraphLocation>(Allocator.Temp),
                path = path
            };

            job.Execute();
            job.aStar.Dispose();
        
            // path will now be filled with the corner points
        }
    }
    */
}