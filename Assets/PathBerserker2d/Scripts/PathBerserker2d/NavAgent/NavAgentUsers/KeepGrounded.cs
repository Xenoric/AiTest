using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Keeps the agent on moving platforms, by parenting the agent to them.
    /// </summary>
    public class KeepGrounded : MonoBehaviour
    {
        [SerializeField]
        public LayerMask movingPlatformLayermask = 0;

        Transform originalParent;

        private void Awake()
        {
            originalParent = transform.parent;
        }

        void FixedUpdate()
        {
            var hit = Physics2D.Raycast(transform.position + transform.up * 0.1f, -transform.up, 0.4f, movingPlatformLayermask);
            if (hit.collider != null)
            {
                // we hit a moving platform -> parent
                transform.SetParent(hit.collider.transform, true);
            }
            else
            {
                // we didn't hit a moving platform -> unparent
                transform.SetParent(originalParent, true);
            }
        }
    }
}