using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Graphs.Node
{
    [ExcludeFromDocs]
    public struct NodeGraphHeuristicProvider : IHeuristicProvider<NodeGraphNode>
    {
        public NodeGraphNode goal;
        
        public void SetGoal(NodeGraphNode goal)
        {
            this.goal = goal;
        }

        public float Heuristic(NodeGraphNode x)
        {
            return math.distance(x.position, goal.position);
        }
    }
}