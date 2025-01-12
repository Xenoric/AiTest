using System;
using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Converts a <see cref="NavMeshGraph"/> path into a list of corner points.
    /// Use this processor if your world is mostly flat (in the XZ plane). Slopes are allowed but they should not exceed 90 degrees.
    /// If you have a curved world, use <see cref="NavMeshGraphCorners3D"/>.
    /// </summary>
    /// <remarks>Note that the height will be reflected in the corner points, but there won't neccessarily be corner points at the start and end of slopes.</remarks>
    public struct NavMeshGraphCorners : IPathProcessor<NavMeshGraphLocation, float3>
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
            NativeList<float3> appendTo)
        {
            if (path.Length == 1)
            {
                #if UNITY_EDITOR
                if (!(queryStart.Equals(queryGoal)))
                    throw new Exception();
                #endif
                
                // start is in the same triangle as goal, just make a straight line
                appendTo.Add(queryGoal.Left3D);
                return;
            }
            
            // since InsertQueryStart is true, the original starting position is already present, just add the exact goal location
            // as the last "narrowed" portal
            path.Add(queryGoal.AsStartOrGoal());
            
            // optionally shrink portals
            if (shrinkRatio > 0)
            {
                for (int i = 1; i < path.Length - 1; i++)
                {
                    path[i] = path[i].Shrink(shrinkRatio);
                }
            }
            
            SSFA.AppendCorners<NavMeshGraphLocation>(path.AsArray(), appendTo);
        }

        public bool InsertQueryStart => true;
    }
}