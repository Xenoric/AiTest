using UnityEngine;
using System.Collections.Generic;

namespace PathBerserker2d
{
    /// <summary>
    /// Marks all segments within an area with a specific NavTag.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("PathBerserker2d/Nav Area Marker")]
    [HelpURL("https://oribow.github.io/PathBerserker2dDemo/Documentation/classPathBerserker2d_1_1NavAreaMarker.html")]
    public class NavAreaMarker : MonoBehaviour
    {
        public int NavTag
        {
            get => navTag;
            set
            {
                navTag = PathBerserker2dSettings.EnsureNavTagExists(value);
            }
        }
        public Color MarkerColor => PathBerserker2dSettings.GetNavTagColor(navTag);

        [SerializeField]
        int navTag = 0;

        [Tooltip("Minimum angle between the segment tangent and up. Use this to only mark segments with certain angles.")]
        [SerializeField, Range(0, 360)]
        float minAngle = 0;

        [Tooltip("Maximum angle between the segment tangent and up. Use this to only mark segments with certain angles.")]
        [SerializeField, Range(0, 360)]
        float maxAngle = 360;

        /// <summary>
        /// Updates the modified marked area after a continuous time period of no movement.
        /// </summary>
        [Tooltip("Updates the modified marked area after a continuous time period of no movement.")]
        [SerializeField]
        public float updateAfterTimeOfNoMovement = 0.2f;

        /// <summary>
        /// Updates the modified marked area after this amount of time passed.
        /// </summary>
        [Tooltip("Updates the modified marked area after this amount of time passed.")]
        [SerializeField]
        public float updateAfterTime = 1;

        public int PBComponentId { get; }

        private RectTransform rectTransform;
        private List<NavAreaMarkerInstance> instances;
        private float lastMovementTime;
        private float isDirtySince;
        private bool isDirty = false;

        #region UNITY
        private void OnEnable()
        {
            rectTransform = GetComponent<RectTransform>();
            instances = new List<NavAreaMarkerInstance>();
            transform.hasChanged = false;
            AddToGraph();
        }

        private void OnDisable()
        {
            RemoveFromGraph();
        }

        private void Update()
        {
            if (transform.hasChanged)
            {
                if (!isDirty)
                    isDirtySince = Time.time;

                isDirty = true;
                transform.hasChanged = false;
                lastMovementTime = Time.time;
            }

            if (isDirty && 
                (Time.time - lastMovementTime > updateAfterTimeOfNoMovement
                || Time.time - isDirtySince > updateAfterTime))
            {
                AddToGraph();
                lastMovementTime = float.MaxValue;
            }
        }

        private void OnValidate()
        {
            navTag = PathBerserker2dSettings.EnsureNavTagExists(navTag);
            updateAfterTimeOfNoMovement = Mathf.Max(0, updateAfterTimeOfNoMovement);
            updateAfterTime = Mathf.Max(0, updateAfterTime);
        }

        private void Reset()
        {
            // only modify sizeDelta, if its at default value
            var rt = GetComponent<RectTransform>();
            if (rt.sizeDelta == new Vector2(100, 100))
                rt.sizeDelta = Vector2.one;
        }
        #endregion

        /// <summary>
        /// Updates area of effect mapping. Call after modifying NavAreaMarker transform.
        /// Alternatively, instead of calling this function, set transform.hasChanged to true.
        /// </summary>
        public void UpdateMappings()
        {
            AddToGraph();
            lastMovementTime = float.MaxValue;
        }

        private void AddToGraph()
        {
            isDirty = false;
            if (instances.Count > 0)
                RemoveFromGraph();

            var r = rectTransform.rect;
            Vector2 scaleFactor = rectTransform.lossyScale * r.size * 0.5f;
            Vector2 center = r.center;

            r.min = center - scaleFactor + (Vector2)rectTransform.position;
            r.max = center + scaleFactor + (Vector2)rectTransform.position;

            var results = PBWorld.BoxCastWithStaged(r, rectTransform.rotation.eulerAngles.z, minAngle, maxAngle);

            foreach (var pointer in results)
            {
                var instance = new NavAreaMarkerInstance(this, pointer);
                PBWorld.NavGraph.AddSegmentModifier(instance);
                instances.Add(instance);
            }
        }

        private void RemoveFromGraph()
        {
            foreach (var instance in instances)
            {
                PBWorld.NavGraph.RemoveSegmentModifier(instance);
            }
            instances.Clear();
        }
    }

    internal class NavAreaMarkerInstance
    {
        public int NavTag => original.NavTag;
        public float T => position.t;
        public float Length => position.length;

        public NavSubsegmentPointer position;
        public int PBComponentId => original.PBComponentId;

        NavAreaMarker original;

        public NavAreaMarkerInstance(NavAreaMarker original)
        {
            this.original = original;
        }

        public NavAreaMarkerInstance(NavAreaMarker original, NavSubsegmentPointer position)
        {
            this.original = original;
            this.position = position;
        }
    }
}
