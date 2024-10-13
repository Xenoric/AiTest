using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    [RequireComponent(typeof(Shooter))]
    public class AdvancedFollowerWithShooting : MonoBehaviour
    {
        [SerializeField]
        public NavAgent navAgent = null;

        [SerializeField]
        public List<Transform> targets = new List<Transform>();

        [SerializeField, Tooltip("Radius when agent should start moving towards the target. Should be >= travelStopRadius")]
        public float closeEnoughRadius = 3;

        [SerializeField, Tooltip("Radius when agent should stop moving towards the target")]
        public float travelStopRadius = 1;

        [SerializeField, Tooltip("Using the targets velocity, predicts the targets position in the future")]
        public float targetPredictionTime = 0;

        [SerializeField, Tooltip("How often (in seconds) to recalculate the nearest target")]
        public float targetUpdateInterval = 1f;

        [SerializeField, Tooltip("Distance at which the bot starts shooting")]
        public float shootingRange = 5f;

        private Transform currentTarget;
        private float lastTargetUpdateTime;
        private Shooter shooter;

        void Awake()
        {
            shooter = GetComponent<Shooter>();
        }

        void Update()
        {
            UpdateNearestTarget();

            if (currentTarget == null)
                return;

            Vector2 targetPos = GetTargetPosition(currentTarget);
            float distToTarget = Vector2.Distance(transform.position, targetPos);

            // Always aim at the target
            shooter.AimAt(targetPos);

            if (distToTarget > closeEnoughRadius && 
                !(navAgent.PathGoal.HasValue && Vector2.Distance(navAgent.PathGoal.Value, targetPos) < travelStopRadius))
            {
                if (!navAgent.UpdatePath(targetPos) && targetPredictionTime > 0)
                {
                    navAgent.UpdatePath(currentTarget.position);
                }
            }
            else if (distToTarget < travelStopRadius)
            {
                navAgent.Stop();
            }

            // Shooting logic
            if (distToTarget <= shootingRange)
            {
                shooter.TryShoot(targetPos);
            }
        }

        private void UpdateNearestTarget()
        {
            if (Time.time - lastTargetUpdateTime < targetUpdateInterval)
                return;

            lastTargetUpdateTime = Time.time;

            Transform nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (var target in targets)
            {
                if (target == null) continue;

                float distance = Vector2.Distance(transform.position, target.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = target;
                }
            }

            currentTarget = nearestTarget;
        }

        private Vector2 GetTargetPosition(Transform target)
        {
            Collider2D collider = target.GetComponent<Collider2D>();
            Vector2 targetCenter = collider != null ? (Vector2)collider.bounds.center : (Vector2)target.position;

            if (targetPredictionTime > 0)
            {
                IVelocityProvider velocityProvider = target.GetComponent<IVelocityProvider>();
                if (velocityProvider != null)
                    return targetCenter + velocityProvider.WorldVelocity * targetPredictionTime;

                Rigidbody2D rigidbody = target.GetComponent<Rigidbody2D>();
                if (rigidbody != null)
                    return targetCenter + rigidbody.velocity * targetPredictionTime;
            }
            return targetCenter;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, closeEnoughRadius);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, travelStopRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, shootingRange);

            if (currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, GetTargetPosition(currentTarget));
            }
        }

        private void OnValidate()
        {
            closeEnoughRadius = Mathf.Max(travelStopRadius, closeEnoughRadius);
            targetUpdateInterval = Mathf.Max(0.1f, targetUpdateInterval);
            shootingRange = Mathf.Max(travelStopRadius, shootingRange);
        }

        private void Reset()
        {
            navAgent = GetComponent<NavAgent>();
            if (GetComponent<Shooter>() == null)
            {
                gameObject.AddComponent<Shooter>();
            }
        }
    }
}