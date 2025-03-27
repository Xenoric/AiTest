using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Makes a NavAgent follow another.
    /// </summary>
    [RequireComponent(typeof(NavAgent))]
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

        private void Awake()
        {
            // Гарантируем, что NavAgent всегда будет доступен
            if (navAgent == null)
            {
                navAgent = GetComponent<NavAgent>();
            }
        }

        void Update()
        {
            if (target == null || navAgent == null)
                return;

            Vector2 targetPos = GetTargetPosition();
            float distToTarget = Vector2.Distance(transform.position, targetPos);

            if (distToTarget > closeEnoughRadius && 
                !(navAgent.PathGoal.HasValue && Vector2.Distance(navAgent.PathGoal.Value, targetPos) < travelStopRadius))
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

        // Метод для получения позиции цели с учетом предсказания движения
        private Vector2 GetTargetPosition()
        {
            if (targetPredictionTime <= 0)
                return target.position;

            // Попытка получить velocity от IVelocityProvider или Rigidbody2D
            Vector2 velocity = Vector2.zero;
            
            // Проверка на Rigidbody2D
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                velocity = rb.velocity;
            }
            
            // Расчет предсказанной позиции
            return (Vector2)target.position + velocity * targetPredictionTime;
        }
    }
}