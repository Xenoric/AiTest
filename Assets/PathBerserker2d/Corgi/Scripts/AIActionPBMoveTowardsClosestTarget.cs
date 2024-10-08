using MoreMountains.Tools;
using System.Linq;
using UnityEngine;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Moves Character to closest of the given targets. 
    /// </summary>
    public class AIActionPBMoveTowardsClosestTarget : AIAction
    {
        /// <summary>
        /// Radius when agent should stop moving towards the target.
        /// </summary>
        [SerializeField, Tooltip("Radius when agent should stop moving towards the target")]
        public float travelStopRadius = 1;
        [SerializeField]
        public Transform[] targets = null;

        float lastTick;

        private NavAgent agent;

        public override void Initialization()
        {
            base.Initialization();
            agent = this.GetComponentInParent<NavAgent>();
        }

        public override void PerformAction()
        {
            if (_brain.Target == null)
            {
                agent.Stop();
                return;
            }

            float distToTarget = Vector3.Distance(transform.position, _brain.Target.position);

            if (distToTarget > travelStopRadius && (Time.time - lastTick > 1 || agent.IsIdle))
            {
                // found mapping, path to it
                Vector2[] targetPos = new Vector2[targets.Length];
                int i = 0;
                foreach (var t in targets.Where(t => t != null))
                {
                    targetPos[i++] = t.position;
                }

                agent.UpdatePath(targetPos);
                lastTick = Time.time;
            }
            else if (distToTarget < travelStopRadius)
            {
                agent.Stop();
            }
        }
    }
}
