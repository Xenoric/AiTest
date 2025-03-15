using System;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Common basis for NavLink and NavLinkCluster.
    /// </summary>
    public abstract class BaseNavLink : MonoBehaviour, INavLinkInstanceCreator
    {
        public int LinkType
        {
            get { return linkType; }
            set {
                if (value < 0 || value >= PathBerserker2dSettings.NavLinkTypeNames.Length)
                    throw new ArgumentOutOfRangeException($"{value} is not a valid link type.");
                linkType = value;
            }
        }
        public string LinkTypeName
        {
            get { return PathBerserker2dSettings.NavLinkTypeNames[linkType]; }
            set { linkType = PathBerserker2dSettings.GetLinkTypeFromName(value); }
        }
        public float Clearance
        {
            get { return clearance; }
            set { clearance = value; }
        }
        public float AvgWaitTime
        {
            get { return avgWaitTime; }
            set { avgWaitTime = value; }
        }
        public float CostOverride
        {
            get { return costOverride; }
            set { costOverride = value; }
        }
        public GameObject GameObject => gameObject;
        public int NavTag
        {
            get { return navTag; }
            set { navTag = PathBerserker2dSettings.EnsureNavTagExists(value); }
        }
        public float MaxTraversableDistance
        {
            get { return maxTraversableDistance; }
            set { maxTraversableDistance = value; }
        }

        public int PBComponentId { get; protected set; }

        [Tooltip("Cost of traversing this link. If this is <= 0 the distance between start and goal is used instead.")]
        [SerializeField]
        protected float costOverride = -1;

        [SerializeField]
        protected int linkType = 1;

        [Tooltip("Maximum height an agent can be to traverse this link.")]
        [SerializeField]
        protected float clearance = 2;

        [SerializeField]
        protected int navTag = 0;

        [Tooltip("Average time an agent has to wait before starting to traverse this link. This is purely to tune the pathfinding algorithm.")]
        [SerializeField]
        protected float avgWaitTime = 0;

        [Tooltip("Maximum distances between start and goal, that is considered traversable. If this distance is exceeded (e.g. on a moving platform) an agent will wait to traverse this link.")]
        [SerializeField]
        protected float maxTraversableDistance = 0;

        /// <summary>
        /// Should this link be automatically mapped. If not, you have to call UpdateMapping() yourself.
        /// </summary>
        [SerializeField, Tooltip("Should this link be automatically mapped. If not, you have to call UpdateMapping() yourself.")]
        public bool autoMap = true;

        protected virtual void Awake()
        {
            PBComponentId = PBWorld.GeneratePBComponentId();
        }

        protected virtual void OnValidate()
        {
            linkType = PathBerserker2dSettings.EnsureNavLinkTypeExists(linkType);
            navTag = PathBerserker2dSettings.EnsureNavTagExists(navTag);
        }

        /// <summary>
        /// MUST BE THREAD SAFE!
        /// Calculates the cost of traversing from start to goal.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="goal"></param>
        /// <returns></returns>
        public float TravelCosts(Vector2 start, Vector2 goal)
        {
            float costOverride = this.costOverride;
            if (costOverride >= 0)
                return costOverride + avgWaitTime;
            else
                return Mathf.Max(maxTraversableDistance, Vector2.Distance(start, goal)) + avgWaitTime;
        }
    }
}
