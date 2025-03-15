using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// A link from one segment to another.
    /// </summary>
    /// <remarks>
    /// NavLink gets added to the pathfinder at runtime.
    /// It can be loaded and unloaded by enabling / disabling the component
    /// After being loaded and added to the pathfinder, the position of the link will not be updated.
    /// For example that means, if your link start position gets mapped to a position on a moving platform, the initial mapping of the link start to the segment won't change.
    /// The mapped position is relative to the NavSurface containing the moving platform. 
    /// It will follow the movements of the platform, even though the start marker of the link will not.
    /// ## Mapping
    /// You can update the links mapping by calling <see cref="UpdateMapping"/> 
    ///
    /// Internally, when moving the start and end marker around in scene view while the game is playing,  <see cref="UpdateMapping"/> is called.
    /// That means you can move the markers around and the link will update its mapping.
    /// If you move them around by any other means however, you have to call <see cref="UpdateMapping"/> afterwards.
    /// ## Visualization
    /// Everything visualization related is purely for you and is not meant to accessed at runtime.
    /// Visualizations like bezier or projectile are meant to allow you to figure out a good clearance value.
    /// ## Traversable
    /// When a link is marked bidirectional, internally two links get added to the pathfinder. One for each direction.
    /// Both links traversable can be set separately with <see cref="SetStartToGoalLinkTraversable"/> and <see cref="SetGoalToStartLinkTraversable"/>.
    ///
    /// **Not being traversable should always be temporary.** In the sense of, **"this link is not traversable right now, but will be in the future"**.
    /// NavAgents will wait indefinitely for the link to become traversable again.
    /// You can adjust AvgWaitTime to increase the cost of such not alway traversable links.
    /// The pathfinder will add it to the cost of traversal.
    /// The pathfinder does not care if a link is marked as traversable or not. It only cares about the cost of traversal.
    /// If you want to disable a link for a longer time, consider disabling the link component. Then it will be unloaded and not considered for any pathfinding.
    /// </remarks>
    [AddComponentMenu("PathBerserker2d/Nav Link")]
    public sealed class NavLink : BaseNavLink
    {
        public enum VisualizationType
        {
            Linear = 0,
            QuadradticBezier = 1,
            Projectile = 2,
            Teleport = 3,
            None = 4,
            TransformBasedMovement = 5
        }

        public Vector2 GoalWorldPosition
        {
            get
            {
                return transform.TransformPoint(goal);
            }
            set
            {
                goal = transform.InverseTransformPoint(value);
            }
        }
        public Vector2 StartWorldPosition
        {
            get
            {
                return transform.TransformPoint(start);
            }
            set
            {
                start = transform.InverseTransformPoint(value);
            }
        }
        public Vector2 StartLocalPosition
        {
            get
            {
                return start;
            }
            set
            {
                start = value;
            }
        }
        public Vector2 GoalLocalPosition
        {
            get
            {
                return goal;
            }
            set
            {
                goal = value;
            }
        }

        public VisualizationType CurrentVisualizationType { get { return visualizationType; } }

        public bool IsBidirectional
        {
            get { return isBidirectional; }
            set
            {
                if (isBidirectional != value && !value)
                {
                    linkGoalToStart.RemoveFromWorld();
                }
                isBidirectional = value;
            }
        }

        public bool IsAddedToWorld => linkStartToGoal?.IsAdded ?? false;

        internal float HorizontalSpeed => horizontalSpeed;
        internal float TraversalAngle => traversalAngle;
        internal Vector2 BezierControlPoint { get => bezierControlPoint; set => bezierControlPoint = value; }

        [Header("Location")]
        [SerializeField]
        Vector2 start = Vector2.left * 2;
        [SerializeField]
        Vector2 goal = Vector2.right * 2;
        [SerializeField]
        bool isBidirectional = true;
        [SerializeField]
        VisualizationType visualizationType = VisualizationType.TransformBasedMovement;
        [SerializeField]
        float traversalAngle = 0;
        [SerializeField]
        float horizontalSpeed = 1;
        [SerializeField, HideInInspector]
        Vector2 bezierControlPoint = Vector2.up * 3;

        private NavLinkInstance linkStartToGoal;
        private NavLinkInstance linkGoalToStart;

        #region UNITY
        private void OnEnable()
        {
            if (linkStartToGoal == null)
                linkStartToGoal = new NavLinkInstance(this);
            if (linkGoalToStart == null)
                linkGoalToStart = new NavLinkInstance(this);

            AutoUpdateMapping();
        }

        private void OnDisable()
        {
            linkStartToGoal.RemoveFromWorld();
            linkGoalToStart.RemoveFromWorld();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (linkGoalToStart != null && (!linkGoalToStart.IsAdded || linkStartToGoal != null))
            {
                AutoUpdateMapping();

                if (!isBidirectional)
                    linkGoalToStart.RemoveFromWorld();
            }
        }
        #endregion

        /// <summary>
        /// Update the mapping for both link instances. Call after link positions have been changed. 
        /// </summary>
        public void UpdateMapping()
        {
            NavSegmentPositionPointer navStart, navGoal;
            if (PBWorld.TryMapPointWithStaged(StartWorldPosition, out navStart)
                && PBWorld.TryMapPointWithStaged(GoalWorldPosition, out navGoal))
            {
                linkStartToGoal.UpdateMapping(navStart, navGoal, StartWorldPosition, GoalWorldPosition);
                linkGoalToStart.UpdateMapping(navGoal, navStart, GoalWorldPosition, StartWorldPosition);

                linkStartToGoal.AddToWorld();
                if (isBidirectional) linkGoalToStart.AddToWorld();
            }
            else
            {
                linkStartToGoal.RemoveFromWorld();
                linkGoalToStart.RemoveFromWorld();
            }
        }

        /// <summary>
        /// Set the link instance from start point to goal traversable.
        /// </summary>
        public void SetStartToGoalLinkTraversable(bool traversable)
        {
            this.linkStartToGoal.IsTraversable = traversable;
        }

        /// <summary>
        /// Set the link instance from goal point to start traversable. This link only exist, if the link is bidirectional.
        /// </summary>
        public void SetGoalToStartLinkTraversable(bool traversable)
        {
            this.linkGoalToStart.IsTraversable = traversable;
        }

        private void AutoUpdateMapping()
        {
            if (autoMap)
                UpdateMapping();
        }
    }
}