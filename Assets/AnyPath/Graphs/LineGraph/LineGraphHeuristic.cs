using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Graphs.Line
{
    /// <summary>
    /// Simple heuristic provider for <see cref="LineGraph"/>
    /// </summary>
    public struct LineGraphHeuristic : IHeuristicProvider<LineGraphLocation>
    {
        private LineGraphLocation goal;

        public void SetGoal(LineGraphLocation goal)
        {
            this.goal = goal;
        }
        
        [ExcludeFromDocs]
        public float Heuristic(LineGraphLocation a)
        {
            return math.distance(a.Position, goal.Position);
        }
    }
}