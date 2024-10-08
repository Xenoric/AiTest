using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Moves Character to current target.
    /// </summary>
    public class AIActionPBMoveTowardsTarget : AIAction
    {
        /// <summary>
        /// Radius when agent should stop moving towards the target.
        /// </summary>
        [SerializeField, Tooltip("Radius when agent should stop moving towards the target")]
        public float travelStopRadius = 4;

        /// <summary>
        /// Radius when agent should start moving towards the target. Should be >= travelStopRadius.
        /// </summary>
        [SerializeField, Tooltip("Radius when agent should start moving towards the target. Should be >= travelStopRadius")]
        public float travelStartRadius = 5;

        /// <summary>
        /// Using the targets velocity, predicts the targets position in the future and uses this prediction as pathfinding goal. Useful for fast moving enemies. Only works when the target has a CorgiController, Rigidbody2d component or a component that implements IVelocityProvider.
        /// </summary>
        [SerializeField]
        [Tooltip("Using the targets velocity, predicts the targets position in the future and uses this prediction as pathfinding goal. Useful for fast moving enemies. Only works when the target has a CorgiController, Rigidbody2d component or a component that implements IVelocityProvider.")]
        public float targetPredictionTime = 0;

        private NavAgent navAgent;

        private void OnValidate()
        {
            travelStartRadius = Mathf.Max(travelStopRadius, travelStartRadius);
        }

        public override void Initialization()
        {
            base.Initialization();
            navAgent = this.GetComponentInParent<NavAgent>();
        }

        public override void PerformAction()
        {
            if (_brain.Target == null)
            {
                navAgent.Stop();
                return;
            }

            Vector2 targetPos = GetTargetPosition();
            float distToTarget = Vector2.Distance(transform.position, targetPos);

            if (distToTarget > travelStartRadius && !(navAgent.PathGoal.HasValue && Vector2.Distance(navAgent.PathGoal.Value, targetPos) < travelStopRadius))
            {
                if (!navAgent.UpdatePath(targetPos) && targetPredictionTime > 0)
                {
                    navAgent.UpdatePath(_brain.Target.position);
                }
            }
            else if (distToTarget < travelStopRadius)
            {
                navAgent.Stop();
            }
        }

        private Vector2 GetTargetPosition()
        {
            Vector2 tpos = _brain.Target.position;
            if (targetPredictionTime > 0)
            {
                IVelocityProvider velocityProvider = _brain.Target.GetComponent<IVelocityProvider>();
                if (velocityProvider != null)
                    return tpos + velocityProvider.WorldVelocity * targetPredictionTime;

                CorgiController controller = _brain.Target.GetComponentInParent<CorgiController>();
                if (controller != null)
                    return tpos + controller.WorldSpeed * targetPredictionTime;

                Rigidbody2D rigidbody = _brain.Target.GetComponentInParent<Rigidbody2D>();
                if (rigidbody != null)
                    return tpos + rigidbody.velocity * targetPredictionTime;
            }
            return tpos;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, travelStartRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, travelStopRadius);
        }
    }
}
