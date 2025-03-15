using Priority_Queue;

namespace PathBerserker2d
{
    internal class PathValues : FastPriorityQueueNode
    {
        public NavGraphNode node;
        public NavGraphNode parent;
        public float costSoFar;
        public float estimatedFutherPath;

        public PathValues(NavGraphNode node)
        {
            this.node = node;
        }
    }
}
