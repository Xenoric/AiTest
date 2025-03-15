using UnityEngine;

namespace PathBerserker2d
{
    internal class CornerLink : INavLinkInstance
    {
        public NavSegmentPositionPointer Start => start;

        public NavSegmentPositionPointer Goal => goal;

        public int LinkType => 0;

        public string LinkTypeName => PathBerserker2dSettings.GetLinkTypeName(0);

        public GameObject GameObject => null;

        public float Clearance => float.MaxValue;

        public int NavTag => 0;

        public bool IsTraversable => true;
        public int PBComponentId => 0;

        private NavSegmentPositionPointer start;
        private NavSegmentPositionPointer goal;
        private float angle;

        public CornerLink(NavSegmentPositionPointer start, NavSegmentPositionPointer goal, float angle)
        {
            this.start = start;
            this.goal = goal;
            this.angle = angle;
        }

        public float TravelCosts(Vector2 start, Vector2 goal)
        {
            return angle;
        }

        public void OnRemove()
        {

        }
    }
}
