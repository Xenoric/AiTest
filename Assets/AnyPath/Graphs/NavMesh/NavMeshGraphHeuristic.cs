using AnyPath.Native;
using Unity.Mathematics;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Basic heuristic provider for usage with the NavMeshGraph
    /// </summary>
    public struct NavMeshGraphHeuristic : IHeuristicProvider<NavMeshGraphLocation>
    {
        private NavMeshGraphLocation goal;

        public void SetGoal(NavMeshGraphLocation goal)
        {
            this.goal = goal;
        }
        
        public float Heuristic(NavMeshGraphLocation a)
        {
            return math.distance(a.ExitPosition, goal.ExitPosition);
            /*
            var goalTri = graph.GetTriangle(goal.TriangleIndex);
            var currentTri = graph.GetTriangle(a.TriangleIndex);
 
            float h = math.distance(currentTri.ClosestPoint(a.ExitPosition), goal.ExitPosition);
            */



            /*
            float3 prefferedDirection = normalizesafe(goal.ExitPosition - start);
            float3 direction = normalizesafe(a.ExitPosition - start);
            h *= 1f - .001f * math.dot(prefferedDirection, direction);
            */


            //h += new Random((uint)a.TriangleIndex).NextFloat() * .3f;

            /*
            float dx1 = a.ExitPosition.x - goal.ExitPosition.x;
            float dy1 = a.ExitPosition.z - goal.ExitPosition.z;
            float dx2 = start.x - goal.ExitPosition.x;
            float dy2 = start.z - goal.ExitPosition.z;
            float cross = abs(dx1 * dy2 - dx2 * dy1);
            h += cross * 0.1f;
            */

            //return h;
            //return 0;
        }
    }
}