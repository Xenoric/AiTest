using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Adjust the NavAgents rotation to match the segments rotation.
    /// </summary>
    public class AdjustRotation : MonoBehaviour
    {
        [SerializeField]
        public NavAgent agent;

        /// <summary>
        /// Speed at which the agent is rotated.
        /// </summary>
        [SerializeField, Tooltip("Speed at which the agent is rotated.")]
        public float rotationSpeed = 20;

        private void Update()
        {
            if (!agent.IsOnLink || agent.CurrentPathSegment?.link?.LinkTypeName != "corner")
            {
                this.transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, this.agent.CurrentSegmentNormal), Time.deltaTime * rotationSpeed);
            }
        }

        private void Reset()
        {
            agent = GetComponent<NavAgent>();
        }
    }
}
