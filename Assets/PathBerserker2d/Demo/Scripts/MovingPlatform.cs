using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Makes a platform move between two points.
    /// </summary>
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField]
        Transform a = null;
        [SerializeField]
        Transform b = null;
        [SerializeField]
        float speed = 0.5f;

        public Vector2 Velocity { get; private set; }

        bool atob = true;

        private void Update()
        {
            if (atob)
            {
                Velocity = (b.position - a.position).normalized * speed;
                transform.Translate(Velocity * Time.deltaTime);

                if ((b.position - a.position).sqrMagnitude < (transform.position - a.position).sqrMagnitude)
                    atob = false;

            }
            else
            {
                Velocity = (a.position - b.position).normalized * speed;
                transform.Translate(Velocity * Time.deltaTime);

                if ((b.position - a.position).sqrMagnitude < (transform.position - b.position).sqrMagnitude)
                    atob = true;
            }
        }

        private void OnDrawGizmos()
        {
            if (a != null && b != null)
            {
                Gizmos.color = Color.red;

                DrawArrow(a.position, b.position);
                DrawArrow(b.position, a.position);
            }
        }

        private void DrawArrow(Vector2 start, Vector2 end)
        {
            Vector2 dir = end - start;
            float length = dir.magnitude;
            dir /= length;
            Vector2 baseA = start + dir * (length - 0.1f);
            Gizmos.DrawLine(start, baseA);

            Vector2 normal = new Vector2(-dir.y, dir.x) * 0.1f;
            Gizmos.DrawLine(baseA - normal, baseA + normal);
            Gizmos.DrawLine(baseA - normal, end);
            Gizmos.DrawLine(baseA + normal, end);
        }
    }
}