using System;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Let the NavAgent walk to a series of goals in a loop.
    /// </summary>
    public class PatrolWalker : MonoBehaviour
    {
        /// <summary>
        /// Radius at which the agent starts calculating path to next goal on patrol route. Must be >= 0.
        /// </summary>
        public float CalcNextPathRad
        {
            get => calcNextPathRad;
            set
            {
                if (calcNextPathRad < 0)
                    throw new ArgumentException("CalcNextPathRad must be greater or equal to 0");
                calcNextPathRad = value;
            }
        }

        public Transform[] PatrolRoute
        {
            get => goals;
            set
            {
                this.goals = value;
                this.currentGoal = 0;
            }
        }

        [SerializeField]
        public NavAgent navAgent;
        [SerializeField]
        Transform[] goals = null;
        [SerializeField]
        float calcNextPathRad = 0.2f;

        private Transform goal => goals[currentGoal];

        int currentGoal = 0;

        private void Start()
        {
            navAgent.PathTo(goal.position);
        }

        void Update()
        {
            if (goals == null)
                return;

            // close enough move to next
            float dist = Vector2.Distance(navAgent.Position, goal.position);
            if (dist < calcNextPathRad)
            {
                currentGoal++;
                if (currentGoal >= goals.Length)
                {
                    currentGoal = 0;
                }
                navAgent.UpdatePath(goal.position);
            }
        }

        private void OnValidate()
        {
            calcNextPathRad = Mathf.Max(calcNextPathRad, 0.1f);
        }

        private void Reset()
        {
            navAgent = GetComponent<NavAgent>();
        }

        /// <summary>
        /// Added be me
        /// </summary>
        public void RefreshPath()
        {
            navAgent.PathTo(goal.position);
        }
    }
}