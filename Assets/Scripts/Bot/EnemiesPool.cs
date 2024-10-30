using UnityEngine;

namespace Scripts.Bot
{
    public class EnemiesPool : MonoBehaviour
    {
        [SerializeField] private Transform[] _enemies;

        public Transform GetClosest(Vector2 selfPosition)
        {
            Transform closestEnemy = null;

            var closestDistance = Mathf.Infinity;//GetDistance(selfPosition, closestEnemy.position);
            for (int i = 0; i < _enemies.Length; i++)
            {
                var enemyPosition = _enemies[i];

                if (!enemyPosition)
                    continue;
                var distance = Calculations.GetDistance(selfPosition, enemyPosition.position);

                if (closestDistance > distance)
                {
                    closestDistance = distance;
                    closestEnemy = enemyPosition;
                }
            }

            return closestEnemy;
        }
    }
}