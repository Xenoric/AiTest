using UnityEngine;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Make a camera smoothly follow a target.
    /// </summary>
    public class SmoothFollow : MonoBehaviour
    {
        [SerializeField]
        public Transform target;
        [SerializeField]
        float speed = 3;

        void LateUpdate()
        {
            if (Vector2.Distance(transform.position, target.position) > 0.1f)
            {
                float z = transform.position.z;
                Vector3 v = Vector2.Lerp(transform.position, target.position, speed * Time.deltaTime);
                v.z = z;
                transform.position = v;
            }
        }
    }
}
