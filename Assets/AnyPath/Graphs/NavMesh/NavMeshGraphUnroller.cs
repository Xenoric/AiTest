using AnyPath.Native;
using Unity.Collections;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Processes a <see cref="NavMeshGraph"/> path for use with realtime steering using <see cref="SSFA.GetSteerTargetPosition{T}"/>
    /// </summary>
    /// <remarks>The raw path of <see cref="NavMeshGraphLocation"/> is converted into a path of <see cref="UnrolledNavMeshGraphPortal"/>.
    /// This path can be fed into <see cref="SSFA.GetSteerTargetPosition{T}"/> to obtain real time steering information, even if your agent
    /// is not exactly on the path.</remarks>
    public struct NavMeshGraphUnroller : IPathProcessor<NavMeshGraphLocation, UnrolledNavMeshGraphPortal>
    {
        /// <summary>
        /// Optional value between 0 and 1 tot allows for shrinking on the portals, keeping the path more towards the center of the triangles.
        /// In general, leave this value at zero, because it may result in less smooth paths if the portals don't touch each other anymore.
        /// This can be used to keep the agent from touching walls, but a better option would be to already have your navmesh defined in such
        /// a way that the triangles are far enough away from walls
        /// </summary>
        public float shrinkRatio;
        
        public void ProcessPath(
            NavMeshGraphLocation queryStart,
            NavMeshGraphLocation queryGoal, 
            NativeList<NavMeshGraphLocation> path, 
            NativeList<UnrolledNavMeshGraphPortal> appendTo)
        {
            // the query's start and goal already have their Left+Right set to the exact position
            path.Add(queryGoal.AsStartOrGoal());

            // optionally shrink portals
            if (shrinkRatio > 0)
            {
                for (int i = 1; i < path.Length - 1; i++)
                    path[i] = path[i].Shrink(shrinkRatio);
            }
            
            NativeArray<UnrolledNavMeshGraphPortal> segments = new NativeArray<UnrolledNavMeshGraphPortal>(path.Length, Allocator.Temp);
            UnrolledNavMeshGraphPortal.Unroll<NavMeshGraphLocation>(path.AsArray(), segments);

            appendTo.AddRange(segments);
            segments.Dispose();
        }

        // For this kind of "realtime" steering, we don't need to insert the exact query starting location because we'll use the agents
        // current position each iteration
        public bool InsertQueryStart => false;
    }
}