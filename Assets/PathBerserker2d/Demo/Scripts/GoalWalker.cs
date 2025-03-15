using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Make Agent walk to specified goal, if it isn't there already. 
    /// </summary>
    class GoalWalker : MonoBehaviour
    {
        [SerializeField]
        public NavAgent navAgent;
        [SerializeField]
        Transform goal = null;

        void Update()
        {
            // are we not close enough to our goal and not already moving to its position
            if (Vector2.Distance(goal.position, navAgent.transform.position) > 0.5f && (navAgent.IsIdle || goal.hasChanged))
            {
                goal.hasChanged = false;
                navAgent.UpdatePath(goal.position);
            }
        }

        private void Reset()
        {
            navAgent = GetComponent<NavAgent>();
        }
    }
}
