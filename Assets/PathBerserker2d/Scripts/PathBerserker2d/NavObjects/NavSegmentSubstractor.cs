using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Marks with its RectTransform an area to remove segments from.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class NavSegmentSubstractor : MonoBehaviour
    {
        /// <summary>
        /// Minimum angle between the segment tangent and up. Use this to only remove segments with certain angles.
        /// </summary>
        [Tooltip("Minimum angle between the segment tangent and up. Use this to only remove segments with certain angles.")]
        [SerializeField, Range(0, 360)]
        public float fromAngle = 0;

        /// <summary>
        /// Maximum angle between the segment tangent and up. Use this to only remove segments with certain angles.
        /// </summary>
        [Tooltip("Maximum angle between the segment tangent and up. Use this to only remove segments with certain angles.")]
        [SerializeField, Range(0, 360)]
        public float toAngle = 360;
    }
}
