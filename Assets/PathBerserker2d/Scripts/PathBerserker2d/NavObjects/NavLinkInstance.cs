using UnityEngine;

namespace PathBerserker2d
{
    internal sealed class NavLinkInstance : INavLinkInstance
    {
        public NavSegmentPositionPointer Start => start;
        public NavSegmentPositionPointer Goal => goal;
        public int LinkType => creator.LinkType;
        public string LinkTypeName => PathBerserker2dSettings.GetLinkTypeName(creator.LinkType);
        public GameObject GameObject => creator.GameObject;
        public bool IsAdded => isAdded;
        public float Clearance => creator.Clearance;
        public float CostOverride => creator.CostOverride;
        public int NavTag => creator.NavTag;
        public bool IsTraversable
        {
            get
            {
                return isTraversable && (creator.MaxTraversableDistance <= 0 || Vector2.Distance(Start.Position, Goal.Position) <= creator.MaxTraversableDistance);
            }
            set
            {
                isTraversable = value;
            }
        }
        public int PBComponentId => creator.PBComponentId;
        internal INavLinkInstanceCreator Creator => creator;


        private INavLinkInstanceCreator creator;
        private NavSegmentPositionPointer start;
        private Vector2 startPos;
        private NavSegmentPositionPointer goal;
        private Vector2 goalPos;
        private bool isAdded = false;
        private bool isTraversable = true;

        public NavLinkInstance(INavLinkInstanceCreator creator)
        {
            this.creator = creator;
        }

        public float TravelCosts(Vector2 start, Vector2 goal)
        {
            return creator.TravelCosts(start, goal);
        }



        public void AddToWorld()
        {
            if (!isAdded)
            {
                PBWorld.NavGraph.AddNavLink(this, start, goal);
                isAdded = true;
            }
        }

        public void RemoveFromWorld()
        {
            if (isAdded)
            {
                PBWorld.NavGraph.RemoveNavLink(this, start, goal);
                isAdded = false;
            }
        }

        public void UpdateMapping(NavSegmentPositionPointer start, NavSegmentPositionPointer goal, Vector2 startPos, Vector2 goalPos)
        {
            this.startPos = startPos;
            this.goalPos = goalPos;

            if (isAdded)
            {
                if (start != this.start)
                {
                    var oldPos = this.start;
                    this.start = start;

                    PBWorld.NavGraph.MoveNavLinkStart(this, start, goal, oldPos);
                }
                if (goal != this.goal)
                {
                    var oldPos = this.goal;
                    this.goal = goal;

                    PBWorld.NavGraph.MoveNavLinkGoal(this, start, goal, oldPos);
                }
            }
            else
            {
                this.start = start;
                this.goal = goal;
            }
        }

        public void OnRemove()
        {
            if (start.surface != null)
                RemoveFromWorld();
            else
                isAdded = false;
        }
    }
}