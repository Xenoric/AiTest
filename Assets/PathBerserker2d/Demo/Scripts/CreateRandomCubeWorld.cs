using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace PathBerserker2d.Demo
{
    public class CreateRandomCubeWorld : MonoBehaviour
    {
        [SerializeField]
        RectTransform levelBounds;
        [SerializeField]
        GameObject cubePrefab;
        [SerializeField]
        float perlinMult = 4;

        private void Start()
        {
            GenerateLevel();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current.eKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.E))
#endif
            {
                for (int i = 0; i < transform.childCount; i++)
                    Destroy(transform.GetChild(i).gameObject);
                GenerateLevel();
            }
        }

        private void GenerateLevel()
        {
            GameObject root = new GameObject("Root", typeof(NavSurface));
            root.transform.parent = this.transform;

            // place boxes randomly using perlin noise
            float offsetX = Random.Range(-10, 10);
            float offsetY = Random.Range(-10, 10);

            float width = levelBounds.rect.width;
            float height = levelBounds.rect.height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float n = Mathf.PerlinNoise(x * perlinMult / width + offsetX, y * perlinMult / height + offsetY);
                    if (n > 0.5f)
                    {
                        Vector2 pos = new Vector2(x, y);
                        var cube = GameObject.Instantiate<GameObject>(cubePrefab, pos, Quaternion.identity);
                        cube.transform.parent = root.transform;
                    }
                }
            }

            StartCoroutine(root.GetComponent<NavSurface>().Bake());
        }
    }
}
