using MoreMountains.Tools;
using UnityEngine;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Moves the Character along a set of points. Last point loops back to the first point.
    /// </summary>
    public class AIActionPBPatrol : AIAction
    {
        public Transform[] PatrolRoute
        {
            get => patrolPoints;
            set
            {
                this.patrolPoints = value;
                this.goalIndex = 0;
            }
        }

        [SerializeField]
        Transform[] patrolPoints = null;

        /// <summary>
        /// Radius at which the agent starts calculating path to next goal on patrol route. Must be >= 0.
        /// </summary>
        [SerializeField]
        public float calcNextPathRad = 0.2f;

        private Transform goal => patrolPoints[goalIndex];

        private NavAgent agent;
        private int goalIndex = 0;

        public override void Initialization()
        {
            base.Initialization();
            agent = this.GetComponentInParent<NavAgent>();
        }

        public override void PerformAction()
        {
            float dist = Vector2.Distance(agent.Position, goal.position);
            if (dist < calcNextPathRad || agent.IsIdle)
            {
                goalIndex++;
                if (goalIndex >= patrolPoints.Length)
                {
                    goalIndex = 0;
                }
                agent.UpdatePath(goal.position);
            }
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            agent.PathTo(goal.position);
        }

        public override void OnExitState()
        {
            base.OnExitState();
            agent.Stop();
        }
    }
}