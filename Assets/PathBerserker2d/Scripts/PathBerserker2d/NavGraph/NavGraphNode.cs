using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal class NavGraphNode
    {
        public float LinkTargetT { get; set; }
        public NavGraphNodeCluster LinkTarget { get; set; }

        // location
        public readonly NavGraphNodeCluster cluster;
        public float t;

        public readonly INavLinkInstance link;
        public PathValues[] pathValues;

        public bool IsGoal => link.LinkType == -1;

        public NavGraphNode(int threadCount, NavGraphNodeCluster owner, float t, NavGraphNodeCluster linkTarget, float linkTargetT, INavLinkInstance original)
        {
            this.cluster = owner;
            this.t = t;

            this.LinkTargetT = linkTargetT;
            this.LinkTarget = linkTarget;

            this.link = original;

            this.pathValues = new PathValues[threadCount];
            InitializePathValueArray();
        }

        public NavGraphNode(int threadCount, NavGraphNodeCluster owner, float t)
        {
            this.cluster = owner;
            this.t = t;
            this.link = new ArtificialLink(-1);
            this.LinkTarget = cluster;
            this.LinkTargetT = t;

            this.pathValues = new PathValues[threadCount];
            InitializePathValueArray();
        }

        public IEnumerable<NavConnection> GetConnections(NavAgent agent, IList<NavGraphNode> goals, int pathValueId)
        {
            return LinkTarget.EnumerateReachableNavVerts(LinkTargetT, agent, goals, pathValueId);
        }

        // param must be local
        public float HeuristicalCostsToGoal(Vector2 goal)
        {
            return Vector2.Distance(goal, GoalPosition());
        }

        public Vector2 GoalPosition()
        {
            return LinkTarget.GetPositionAlongSegment(LinkTargetT);
        }

        public Vector2 WGoalPosition()
        { 
            return LinkTarget.owner.LocalToWorld.MultiplyPoint3x4(LinkTarget.GetPositionAlongSegment(LinkTargetT));
        }

        public Vector2 Position() {
            return cluster.GetPositionAlongSegment(t);
        }

        public Vector2 WPosition()
        {
            return cluster.owner.LocalToWorld.MultiplyPoint3x4(cluster.GetPositionAlongSegment(t));
        }

        private void InitializePathValueArray()
        {
            for (int i = 0; i < pathValues.Length; i++)
            {
                pathValues[i] = new PathValues(this);
            }
        }
    }

    internal struct NavConnection
    {
        public readonly NavGraphNode end;
        public readonly float traversalCosts;

        public NavConnection(NavGraphNode end, float traversalCosts)
        {
            this.end = end;
            this.traversalCosts = traversalCosts;
        }
    }
}