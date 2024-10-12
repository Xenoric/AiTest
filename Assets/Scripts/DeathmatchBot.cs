using UnityEngine;
using PathBerserker2d;

public class DeathmatchBot : MonoBehaviour
{
    [SerializeField]
    public NavAgent navAgent;
    [SerializeField]
    Transform[] targets; // Список целей (врагов)
    [SerializeField]
    float detectionRange = 10f;
    [SerializeField]
    float shootingRange = 5f;
    [SerializeField]
    GameObject bulletPrefab;
    [SerializeField]
    float bulletSpeed = 10f;
    [SerializeField]
    float shootingCooldown = 1f;
    [SerializeField]
    float bulletSpread = 5f;
    [SerializeField]
    LayerMask obstacleLayer; // Слой для препятствий

    private Transform currentTarget;
    private float lastShotTime;

    private void Start()
    {
        if (targets.Length > 0)
        {
            currentTarget = targets[0]; // Начинаем с первой цели
            MoveToTarget();
        }
    }

    private void Update()
    {
        if (currentTarget != null)
        {
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

            if (distanceToTarget <= shootingRange)
            {
                ShootAtTarget();
            }
            else if (distanceToTarget <= detectionRange)
            {
                if (IsPathClear(currentTarget.position))
                {
                    MoveToTarget();
                }
                else
                {
                    FlyToTarget(currentTarget.position);
                }
            }
            else
            {
                // Если цель слишком далеко, переключаемся на следующую
                SwitchToNextTarget();
            }
        }
    }

    private void MoveToTarget()
    {
        navAgent.PathTo(currentTarget.position);
    }

    private void FlyToTarget(Vector3 targetPosition)
    {
        Vector3 flyingPosition = new Vector3(targetPosition.x, targetPosition.y + 3f, targetPosition.z); // Лететь на высоту 3 единицы выше цели
        transform.position = Vector3.MoveTowards(transform.position, flyingPosition, Time.deltaTime * 5f); // Скорость полета
    }

    private bool IsPathClear(Vector3 targetPosition)
    {
        Vector2 direction = (targetPosition - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, targetPosition);
        return !Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
    }

    private void SwitchToNextTarget()
    {
        int currentIndex = System.Array.IndexOf(targets, currentTarget);
        currentIndex = (currentIndex + 1) % targets.Length; // Переход к следующей цели
        currentTarget = targets[currentIndex];
        MoveToTarget();
    }

    private void ShootAtTarget()
    {
        if (Time.time - lastShotTime > shootingCooldown)
        {
            Vector2 direction = (currentTarget.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            angle += Random.Range(-bulletSpread, bulletSpread);

            GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.Euler(0, 0, angle));
            bullet.GetComponent<Rigidbody2D>().velocity = direction * bulletSpeed;

            lastShotTime = Time.time;
        }
    }
}