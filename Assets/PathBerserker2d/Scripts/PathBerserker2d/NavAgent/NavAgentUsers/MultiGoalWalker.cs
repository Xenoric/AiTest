using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Let the agent walk to the closest of the given goals.
    /// </summary>
    public class MultiGoalWalker : MonoBehaviour
    {
        [SerializeField]
        public NavAgent navAgent;
        [SerializeField]
        Transform[] goals = null;
        [SerializeField]
        public bool activateOnStart = true;

        void Start()
        {
            if (activateOnStart)
            {
                MoveToClosestGoal();
            }
        }

        private void Reset()
        {
            navAgent = GetComponent<NavAgent>();
        }

        /// <summary>
        /// Starts moving to closest of this.goals.
        /// </summary>
        public void MoveToClosestGoal()
        {
            Vector2[] vs = new Vector2[goals.Length];
            for (int i = 0; i < goals.Length; i++)
                vs[i] = goals[i].position;
            navAgent.PathTo(vs);
        }

        /// <summary>
        /// Starts moving to closest of supplied goals.
        /// </summary>
        public void MoveToClosestGoal(Transform[] goals)
        {
            Vector2[] vs = new Vector2[goals.Length];
            for (int i = 0; i < goals.Length; i++)
                vs[i] = goals[i].position;
            navAgent.PathTo(vs);
        }
    }
}
