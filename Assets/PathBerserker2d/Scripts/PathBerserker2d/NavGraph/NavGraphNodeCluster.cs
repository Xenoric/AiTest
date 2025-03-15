using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal class NavGraphNodeCluster : LineSegmentWithClearance
    {
        internal readonly List<NavGraphNode> nodes = new List<NavGraphNode>();
        // unsorted
        internal readonly List<NavAreaMarkerInstance> modifiers = new List<NavAreaMarkerInstance>(1);
        internal readonly bool[] containsGoal;
        public readonly NavSurfaceRecord owner;

        public NavGraphNodeCluster(NavSegment segment, int threadCount, NavSurfaceRecord owner) : base(segment)
        {
            containsGoal = new bool[threadCount];
            this.owner = owner;
        }

        public IEnumerable<NavConnection> EnumerateReachableNavVerts(float t, NavAgent agent, IList<NavGraphNode> goals, int pathValueId)
        {
            for (int iNode = 0; iNode < nodes.Count; iNode++)
            {
                var node = nodes[iNode];
                if (!IsAgentAbleToTraverse(agent, t, node))
                    continue;

                yield return new NavConnection(node, node.link.TravelCosts(node.WPosition(), node.WGoalPosition()) * agent.GetLinkTraversalMultiplier(node.link.LinkType) * agent.GetNavTagTraversalMultiplier(node.link.NavTag) + TraversalCosts(t, node.t, agent));
            }
            if (containsGoal[pathValueId])
            {
                for (int iGoal = 0; iGoal < goals.Count; iGoal++)
                {
                    if (goals[iGoal].cluster == this && IsAgentAbleToTraverse(agent, t, goals[iGoal]))
                        yield return new NavConnection(goals[iGoal], TraversalCosts(t, goals[iGoal].t, agent));
                }
            }
        }

        public void AddNode(int pathValueCount, float t, NavGraphNodeCluster linkTarget, float linkTargetT,
            INavLinkInstance link)
        {
            nodes.Add(
                new NavGraphNode(
                    pathValueCount,
                    this,
                    t,
                    linkTarget, linkTargetT,
                    link
                    ));
        }

        public void RemoveNode(INavLinkInstance link)
        {
            int index = nodes.FindIndex(node => node.link == link);
            if (index != -1)
                nodes.RemoveAt(index);
        }

        public void MoveNode(INavLinkInstance link, float newT)
        {
            int index = nodes.FindIndex(node => node.link == link);
            nodes[index].t = newT;
        }

        public NavGraphNode GetNode(INavLinkInstance link)
        {
            return nodes.Find(node => node.link == link);
        }

        public bool DoesNodeExist(INavLinkInstance link)
        {
            return nodes.Exists(node => node.link == link);
        }

        public void ApplyCellClearances(float[] differentClearances)
        {
            for (int i = 0; i < cellClearances.Length; i++)
            {
                cellClearances[i] = Mathf.Min(cellClearances[i], differentClearances[i]);
            }
        }

        public void AddNodeClusterModifier(NavAreaMarkerInstance mod)
        {
            modifiers.Add(mod);
        }

        public void RemoveNodeClusterModifier(NavAreaMarkerInstance mod)
        {
            modifiers.Remove(mod);
        }

        public int GetNavTagVector(float pos)
        {
            int vector = 0;
            foreach (var mod in modifiers)
            {
                if (mod.T <= pos && mod.T + mod.Length >= pos)
                    vector |= (1 << mod.NavTag);
            }
            return vector;
        }

        public int GetNavTagVector(Vector2 pos)
        {
            return GetNavTagVector(DistanceOfPointAlongSegment(pos));
        }

        public bool CanAgentReachPoint(NavAgent agent, float startT, float goalT)
        {
            return agent.CanTraverseSegment(owner.LocalToWorld.MultiplyVector(Normal), GetMinClearanceTo(startT, goalT));
        }

        public bool CanAgentBeAtPoint(NavAgent agent, float t)
        {
            return Vector2.Angle(Vector2.up, owner.LocalToWorld.MultiplyVector(Normal)) <= agent.MaxSlopeAngle && GetClearanceAlongSegment(t) >= agent.Height && (GetNavTagVector(t) & ~agent.NavTagMask) == 0;
        }

        private bool IsAgentAbleToTraverse(NavAgent agent, float startT, NavGraphNode node)
        {
            return agent.CanTraverseLink(node.link) && CanAgentReachPoint(agent, startT, node.t);
        }

        private float TraversalCosts(float t, float goal, NavAgent agent)
        {
            float a, b;
            if (t < goal)
            {
                a = t;
                b = goal;
            }
            else
            {
                a = goal;
                b = t;
            }
            if (modifiers.Count == 0)
            {
                return b - a;
            }

            float costs = b - a;
            foreach (var mod in modifiers)
            {
                if (mod.T + mod.Length <= a || mod.T >= b)
                    continue;

                // some overlap exists
                costs += (Mathf.Min(mod.T + mod.Length, b) - Mathf.Max(mod.T, a)) * agent.GetNavTagTraversalMultiplier(mod.NavTag);
            }
            return costs;
        }
    }
}
