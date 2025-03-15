using System.Collections;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Lets the NavAgent walk to a random point
    /// </summary>
    public class RandomWalker : MonoBehaviour
    {
        [SerializeField]
        public NavAgent navAgent;

        /// <summary>
        /// The random destination my not always be reachable. RetryCount determines the maximum amount of rolls for a random reachable position.
        /// </summary>
        [SerializeField, Tooltip("The random destination may not always be reachable. RetryCount determines the maximum amount of rolls each update for a random reachable position.")]
        public int retryCount = 10;

        /// <summary>
        /// Will make the agent pick a new random position to walk to, after reaching the previous one. Makes the agent walk between random points, until its set to false.
        /// </summary>
        [SerializeField]
        public bool keepWalkingRandomly = true;

        void Update()
        {
            if (keepWalkingRandomly && navAgent.IsIdle)
            {
                StartRandomWalk();
            }
        }

        private void Reset()
        {
            navAgent = GetComponent<NavAgent>();
        }

        /// <summary>
        /// Picks a random position and makes the NavAgent walk to it.
        /// </summary>
        /// <returns>True, if a random reachable position was found within a maximum of retryCount tries.</returns>
        public bool StartRandomWalk()
        {
            for (int i = 0; i < retryCount && !navAgent.SetRandomDestination(); i++)
            {
                
            }

            return !navAgent.IsIdle;
        }
    }
}