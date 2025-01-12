using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Graphs.PlatformerGraph
{
    /// <summary>
    /// Simple heuristic provider for <see cref="PlatformerGraph"/>
    /// </summary>
    public struct PlatformerGraphHeuristic : IHeuristicProvider<PlatformerGraphLocation>
    {
        private PlatformerGraphLocation goal;

        public void SetGoal(PlatformerGraphLocation goal)
        {
            this.goal = goal;
        }
        
        [ExcludeFromDocs]
        public float Heuristic(PlatformerGraphLocation a)
        {
            return math.distance(a.Position, goal.Position);
        }
    }
}