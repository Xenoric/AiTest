using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Makes a NavAgent follow another.
    /// </summary>
    public class Follower : MonoBehaviour
    {
        [SerializeField]
        public NavAgent navAgent = null;
        [SerializeField]
        public Transform target = null;

        /// <summary>
        /// Radius when agent should start moving towards the target. Should be >= travelStopRadius
        /// </summary>
        [SerializeField, Tooltip("Radius when agent should start moving towards the target. Should be >= travelStopRadius")]
        public float closeEnoughRadius = 3;

        /// <summary>
        /// Radius when agent should stop moving towards the target.
        /// </summary>
        [SerializeField, Tooltip("Radius when agent should stop moving towards the target")]
        public float travelStopRadius = 1;

        /// <summary>
        /// Using the targets velocity, predicts the targets position in the future and uses this prediction as pathfinding goal. Useful for fast moving enemies. Only works when the target has a Rigidbody2d component or a component that implements IVelocityProvider. (NavAgent does not!)
        /// </summary>
        [SerializeField]
        [Tooltip("Using the targets velocity, predicts the targets position in the future and uses this prediction as pathfinding goal. Useful for fast moving enemies. Only works when the target has a Rigidbody2d component or a component that implements IVelocityProvider. (NavAgent does not!)")]
        public float targetPredictionTime = 0;

        void Update()
        {
            if (target == null)
                return;

            Vector2 targetPos = GetTargetPosition();
            float distToTarget = Vector2.Distance(transform.position, targetPos);

            if (distToTarget > closeEnoughRadius && 
                !(navAgent.PathGoal.HasValue &&  Vector2.Distance(navAgent.PathGoal.Value, targetPos) < travelStopRadius))
            {
                if (!navAgent.UpdatePath(targetPos) && targetPredictionTime > 0)
                {
                    navAgent.UpdatePath(target.position);
                }
            }
            else if (distToTarget < travelStopRadius)
            {
                navAgent.Stop();
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, closeEnoughRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, travelStopRadius);
        }

        private void OnValidate()
        {
            closeEnoughRadius = Mathf.Max(travelStopRadius, closeEnoughRadius);
        }

        private void Reset()
        {
            navAgent = GetComponent<NavAgent>();
        }

        private Vector2 GetTargetPosition()
        {
            Vector2 tpos = target.position;
            if (targetPredictionTime > 0)
            {
                IVelocityProvider velocityProvider = target.GetComponent<IVelocityProvider>();
                if (velocityProvider != null)
                    return tpos + velocityProvider.WorldVelocity * targetPredictionTime;

                Rigidbody2D rigidbody = target.GetComponent<Rigidbody2D>();
                if (rigidbody != null)
                    return tpos + rigidbody.velocity * targetPredictionTime;
            }
            return tpos;
        }
    }
}
