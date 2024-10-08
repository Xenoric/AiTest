using MoreMountains.Tools;
using UnityEngine;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Moves Character to random point. May take a while to start moving if random rolled positions are not reachable.
    /// </summary>
    public class AIActionPBMoveTowardsRandomPathableTarget : AIAction
    {
        /// <summary>
        /// Radius when agent should stop moving towards the target
        /// </summary>
        [SerializeField, Tooltip("Radius when agent should stop moving towards the target")]
        public float travelStopRadius = 1;

        float lastTick;

        private NavAgent agent;

        public override void Initialization()
        {
            base.Initialization();
            agent = this.GetComponentInParent<NavAgent>();
        }

        public override void PerformAction()
        {
            float distToTarget = Vector3.Distance(transform.position, _brain.Target.position);

            if (distToTarget > travelStopRadius && (Time.time - lastTick > 1 || agent.IsIdle))
            {
                agent.SetRandomDestination();
                lastTick = Time.time;
            }
            else if (distToTarget < travelStopRadius)
            {
                agent.Stop();
            }
        }
    }
}
