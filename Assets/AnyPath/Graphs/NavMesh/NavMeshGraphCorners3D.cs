using System;
using AnyPath.Native;
using Unity.Collections;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Converts a <see cref="NavMeshGraph"/> path into a list of corner points.
    /// Use this processor if your world has arbitrary curvature.
    /// </summary>
    /// <remarks>The resulting path will be as straight as possible but will follow the exact curvature of the NavMesh. This means
    /// there can be multiple points that form a straight line.</remarks>
    public struct NavMeshGraphCorners3D : IPathProcessor<NavMeshGraphLocation, CornerAndNormal>
    {
        /// <summary>
        /// Optional value between 0 and 1 tot allows for shrinking on the portals, keeping the path more towards the center of the triangles.
        /// In general, leave this value at zero, because it may result in less smooth paths if the portals don't touch each other anymore.
        /// This can be used to keep the agent from touching walls, but a better option would be to already have your navmesh defined in such
        /// a way that the triangles are far enough away from walls
        /// </summary>
        public float shrinkRatio;

        /// <summary>
        /// Weld corners that are below this distance together. This can prevent multiple corners at the same position where 3 or more triangles
        /// in the path intersect
        /// </summary>
        public float weldThreshold;
        
        public void ProcessPath(
            NavMeshGraphLocation queryStart,
            NavMeshGraphLocation queryGoal, 
            NativeList<NavMeshGraphLocation> path, 
            NativeList<CornerAndNormal> appendTo)
        {
            
            if (path.Length == 1)
            {
                #if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (!(queryStart.Equals(queryGoal)))
                    throw new Exception();
                #endif
                
                // start is in the same triangle as goal, just make a straight line
                //appendTo.Add(queryGoal.Left3D);
                appendTo.Add(new CornerAndNormal()
                {
                    position = queryGoal.Left3D,
                    normal = queryGoal.Normal
                });
                
                return;
            }
            
            // the query's start and goal already have their Left+Right set to the exact position
            path.Add(queryGoal.AsStartOrGoal());

            // optionally shrink portals
            if (shrinkRatio > 0)
            {
                for (int i = 1; i < path.Length - 1; i++)
                {
                    path[i] = path[i].Shrink(shrinkRatio);
                }
            }

            NativeArray<UnrolledNavMeshGraphPortal> segments = new NativeArray<UnrolledNavMeshGraphPortal>(path.Length, Allocator.Temp);
            UnrolledNavMeshGraphPortal.Unroll<NavMeshGraphLocation>(path.AsArray(), segments);
            
            /*
            for (int i = 0; i < path.Length; i++)
            {
                appendTo.Add(new CornerAndNormal()
                {
                    normal = path[i].Normal,
                    position = path[i].ExitPosition
                });
            }
            */
            
            
            SSFA.AppendCornersUnrolled<UnrolledNavMeshGraphPortal>(segments, appendTo, weldThreshold);

            segments.Dispose();
        }

        public bool InsertQueryStart => true;
    }
}