using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace PathBerserker2d
{
    /// <summary>
    /// Let the NavAgent walk to a mouse click.
    /// </summary>
    class MouseWalker : MonoBehaviour
    {
        [SerializeField]
        public NavAgent navAgent;

        void Update()
        {
            // mouse click occurred?
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Mouse.current.leftButton.wasPressedThisFrame)
#else
            if (Input.GetMouseButtonDown(0))
#endif
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                Vector2 pos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
#else
                Vector2 pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
#endif
                if (!navAgent.PathTo(pos))
                {
                    if (navAgent.HasValidPosition)
                        Debug.Log($"{name}: Pathfinding failed.");
                    else
                        Debug.Log($"{name}: Agent is not on a NavSurface.");
                }
            }
        }

        private void Reset()
        {
            navAgent = GetComponent<NavAgent>();
        }
    }
}
