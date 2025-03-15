using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Spawns platforms randomly with a random delay.
    /// </summary>
    public class PlatformSpawner : MonoBehaviour
    {
        [SerializeField]
        GameObject[] platformPrefabs = null;

        [SerializeField]
        float fallSpeed = 1;

        [SerializeField]
        float minSpawnDelay = 0.3f;
        [SerializeField]
        float maxSpawnDelay = 0.7f;

        [SerializeField]
        float maxJumpDistance = 4;

        float spawnDelay;

        void Update()
        {
            spawnDelay -= Time.deltaTime;
            if (spawnDelay <= 0)
            {
                // spawn!
                Vector2 pos = new Vector2(transform.position.x + (Random.value - 0.5f) * transform.localScale.x, transform.position.y);

                // chose prefab
                var prefab = platformPrefabs[Random.Range(0, platformPrefabs.Length)];

                // instantiate
                var platform = Instantiate<GameObject>(prefab, pos, Quaternion.identity);

                // set some platform vars
                var fpComp = platform.GetComponent<FallingPlatform>();
                fpComp.fallSpeed = fallSpeed;
                fpComp.deleteBelowYLevel = transform.position.y - transform.localScale.y * 0.5f;
                fpComp.maxJumpDistance = maxJumpDistance;

                // set next spawn delay
                spawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            }
        }

        private void OnDrawGizmos()
        {
            Vector3 leftPoint = transform.position - Vector3.right * transform.localScale.x * 0.5f;

            Gizmos.DrawLine(leftPoint,
                transform.position + Vector3.right * transform.localScale.x * 0.5f);

            Gizmos.DrawLine(leftPoint,
                leftPoint + Vector3.down * transform.localScale.y * 0.5f);
        }
    }
}
