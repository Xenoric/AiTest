using UnityEngine;
using System.Collections.Generic;

namespace PathBerserker2d
{
    /// <summary>
    /// Creates links to interconnect a collection.
    /// </summary>
    /// <remarks>
    /// Consists of a list of points. 
    /// At runtime a link is generated for each point to connect it with each other point.
    /// This is a convenience component. It drastically reduces the amount of work required to setup an elevator or ladder for example.
    ///
    /// Otherwise it functions and behaves the same as a NavLink.
    /// Reference the documentation for NavLink for further details.
    /// </remarks>
    [AddComponentMenu("PathBerserker2d/Nav Link Cluster")]
    public sealed class NavLinkCluster : BaseNavLink
    {
        internal enum PointTraversalType
        {
            Exit,
            Entry,
            Both
        }

        internal LinkPoint[] LinkPoints => linkPoints;

        [SerializeField]
        internal LinkPoint[] linkPoints = new LinkPoint[] { new LinkPoint(Vector2.left * 2), new LinkPoint(Vector2.right * 2) };

        private List<NavLinkInstance> linkInstances;

        #region UNITY
        private void OnEnable()
        {
            if (linkInstances == null)
                linkInstances = new List<NavLinkInstance>();

            if (autoMap)
                UpdateMapping();
        }

        private void OnDisable()
        {
            foreach (var li in linkInstances)
                li.RemoveFromWorld();
        }
        #endregion

        /// <summary>
        /// Update the mapping for all link instances. Call after link positions have been changed.
        /// </summary>
        public void UpdateMapping()
        {
            NavSegmentPositionPointer navStart, navGoal;
            int instanceCounter = 0;
            foreach (var startPoint in linkPoints)
            {
                if (startPoint.traversalType == PointTraversalType.Exit)
                    continue;

                Vector2 worldStart = transform.TransformPoint(startPoint.point);
                foreach (var goalPoint in linkPoints)
                {
                    if (goalPoint.traversalType == PointTraversalType.Entry || goalPoint.point == startPoint.point)
                        continue;

                    Vector2 worldGoal = transform.TransformPoint(goalPoint.point);
                    if (instanceCounter >= linkInstances.Count)
                    {
                        linkInstances.Add(new NavLinkInstance(this));
                    }
                    var linkInstance = linkInstances[instanceCounter++];

                    if (PBWorld.TryMapPointWithStaged(worldStart, out navStart)
                        && PBWorld.TryMapPointWithStaged(worldGoal, out navGoal))
                    {
                        linkInstance.UpdateMapping(navStart, navGoal, worldStart, worldGoal);
                        linkInstance.AddToWorld();
                    }
                    else
                    {
                        linkInstance.RemoveFromWorld();
                    }
                }
            }
        }

        /// <summary>
        /// Set link instances to be traversable based on their start and end points.
        /// </summary>
        /// <param name="traversableFunc">Determines whether to enable or disable the given link instance. Link instance is given as its start and goal position.</param>
        public void SetLinksTraversable(System.Func<Vector2, Vector2, bool> traversableFunc)
        {
            foreach (var link in linkInstances)
            {
                if (link.IsAdded)
                    link.IsTraversable = traversableFunc(link.Start.Position, link.Goal.Position);
            }
        }

        [System.Serializable]
        internal struct LinkPoint
        {
            [SerializeField]
            public Vector2 point;
            [SerializeField]
            public PointTraversalType traversalType;

            public LinkPoint(Vector2 point)
            {
                this.point = point;
                this.traversalType = PointTraversalType.Both;
            }
        }
    }
}