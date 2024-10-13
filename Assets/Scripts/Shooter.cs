using UnityEngine;

public class Shooter : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab of the bullet to shoot")]
    public GameObject bulletPrefab;

    [SerializeField, Tooltip("Speed of the bullet")]
    public float bulletSpeed = 10f;

    [SerializeField, Tooltip("Time between shots")]
    public float fireRate = 1f;

    [SerializeField, Tooltip("The transform representing the weapon")]
    public Transform weaponTransform;

    [SerializeField, Tooltip("The point from which bullets will be fired")]
    public Transform firePoint;

    [SerializeField, Tooltip("Maximum spread angle in degrees")]
    public float maxSpreadAngle = 5f;

    private float lastShotTime;

    private void Awake()
    {
        if (firePoint == null)
        {
            firePoint = weaponTransform;
        }
    }

    public void AimAt(Vector2 targetPosition)
    {
        if (weaponTransform != null)
        {
            Vector2 direction = targetPosition - (Vector2)weaponTransform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            weaponTransform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    public bool TryShoot(Vector2 targetPosition)
    {
        if (Time.time - lastShotTime >= fireRate)
        {
            Shoot(targetPosition);
            lastShotTime = Time.time;
            return true;
        }
        return false;
    }

    private void Shoot(Vector2 targetPosition)
    {
        if (bulletPrefab != null && firePoint != null)
        {
            Vector2 direction = (targetPosition - (Vector2)firePoint.position).normalized;
            
            // Add spread
            float spreadAngle = Random.Range(-maxSpreadAngle, maxSpreadAngle);
            direction = Quaternion.Euler(0, 0, spreadAngle) * direction;

            GameObject bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = direction * bulletSpeed;
            }
            else
            {
                Debug.LogWarning("Bullet prefab does not have a Rigidbody2D component!");
            }

            Destroy(bullet, 5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(firePoint.position, 0.1f);
            Gizmos.DrawRay(firePoint.position, firePoint.right * 1f);
        }
    }
}