using Priority_Queue;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal class Pathfinder
    {
        FastPriorityQueue<PathValues> openList = new FastPriorityQueue<PathValues>(1000);
        HashSet<NavGraphNode> closedList = new HashSet<NavGraphNode>();
        NavGraph navGraph;
        int pathValueId;

        float maxHeuristicAllowed;
        NavSegmentPositionPointer closestReachablePosition;
        NavAgent agent;

        public Pathfinder(NavGraph navGraph, int pathValueId)
        {
            this.navGraph = navGraph;
            this.pathValueId = pathValueId;
        }

        public void ProcessPathRequest(PathRequest request)
        {
            Path path = null;
            this.agent = request.client;

            navGraph.graphLock.AcquireReaderLock(-1);
            try
            {
                NavGraphNodeCluster startCluster;
                if (!navGraph.TryGetClusterAt(request.start, out startCluster))
                {
                    request.Fail(PathRequest.RequestFailReason.MappedStartChanged);
                    return;
                }
                NavGraphNode nvStart = new NavGraphNode(pathValueId + 1, startCluster, request.start.t);
                closestReachablePosition = request.start;

                IList<NavGraphNode> nvGoals = new List<NavGraphNode>(request.goals.Count);
                NavGraphNodeCluster goalCluster;
                for (int iGoal = 0; iGoal < request.goals.Count; iGoal++)
                {
                    if (navGraph.TryGetClusterAt(request.goals[iGoal], out goalCluster))
                    {
                        nvGoals.Add(new NavGraphNode(pathValueId + 1, goalCluster, request.goals[iGoal].t));
                        goalCluster.containsGoal[pathValueId] = true;
                    }
                }
                if (nvGoals.Count == 0)
                {
                    request.Fail(PathRequest.RequestFailReason.AllMappedGoalsChanged);
                    return;
                }

                if (nvGoals.Count == 1)
                {
                    path = this.FindPathSingleGoal(nvStart, nvGoals);
                }
                else
                {
                    path = this.FindPathMultiGoal(nvStart, nvGoals);
                }
                maxHeuristicAllowed = float.MaxValue;

                for (int iGoal = 0; iGoal < nvGoals.Count; iGoal++)
                {
                    nvGoals[iGoal].cluster.containsGoal[pathValueId] = false;
                }
            }
            finally
            {
                navGraph.graphLock.ReleaseReaderLock();
            }

            if (path == null)
            {
                request.closestReachablePosition = closestReachablePosition;
                request.Fail(PathRequest.RequestFailReason.NoPathFromStartToGoal);
            }
            else
            {
                request.Fulfill(path);
            }

        }

        private Path FindPathSingleGoal(NavGraphNode start, IList<NavGraphNode> goals)
        {
            Vector2 goalPos = goals[0].WPosition();
            Path path = null;
            float closestH = float.MaxValue;

        Restart:
            foreach (var conn in start.cluster.EnumerateReachableNavVerts(start.t, agent, goals, pathValueId))
            {
                Expand(conn, start, goalPos);
            }
            UpdateClosestPositionToGoal(start, goalPos, ref closestH);

#if PBDEBUG
            int iterationCount = 0;
#endif
            while (openList.Count > 0)
            {
#if PBDEBUG
                iterationCount++;
#endif
                var vert = openList.Dequeue();
                var node = vert.node;
                /*GizmosQueue.Instance.Enqueue(1, () =>
                {
                    Gizmos.color = Color.yellow;
                    Vector3 v = vert.WPosition();
                    v.z = 5;
                    DebugDrawingExtensions.DrawCircle(v, 0.1f);
                });*/
                if (node.IsGoal)
                {
                    // reached goal !!
                    if (vert.costSoFar < 0)
                    {
                        Debug.LogError("Path found with negative values. This breaks pathfinding. Found path will be thrown away.");
                        return path;
                    }

                    float v = CheckHeuristicValidity(start, node);
                    if (v != 0)
                    {
                        // heuristic was violated! start over. 
#if PBDEBUG
                        Debug.Log("Heuristic was violated. Retrying with upper bound.");
#endif
                        maxHeuristicAllowed = v;
                        openList.Clear();
                        closedList.Clear();

                        goto Restart;
                    }

                    path = GatherPath(start, node);
                    break;
                }

                closedList.Add(node);

                UpdateClosestPositionToGoal(node, goalPos, ref closestH);

                foreach (var conn in vert.node.GetConnections(agent, goals, pathValueId))
                {
                    Expand(conn, node, goalPos);
                }
            }

            openList.Clear();
            closedList.Clear();
#if NOPE_DEBUG
            if (path != null)
                Debug.Log(string.Format(
                    "Found in {0} iteration the path: {1}", iterationCount, path));
            else
            {
                Debug.Log(string.Format(
                    "Found no path in {0} iteration.", iterationCount));
            }
#endif

            return path;
        }

        private Path FindPathMultiGoal(NavGraphNode start, IList<NavGraphNode> goals)
        {
            Path path = null;

            foreach (var conn in start.cluster.EnumerateReachableNavVerts(start.t, agent, goals, pathValueId))
            {
                ExpandZeroH(conn, start);
            }

#if PBDEBUG
            int iterationCount = 0;
#endif
            while (openList.Count > 0)
            {
#if PBDEBUG
                iterationCount++;
#endif
                var vert = openList.Dequeue();
                var node = vert.node;
                if (node.IsGoal)
                {
                    // reached goal !!
                    if (vert.costSoFar < 0)
                    {
                        Debug.LogError("Path found with negative values. This breaks pathfinding. Found path will be thrown away.");
                        return path;
                    }

                    path = GatherPath(start, node);
                    break;
                }

                closedList.Add(node);
                foreach (var conn in node.GetConnections(agent, goals, pathValueId))
                {
                    ExpandZeroH(conn, node);
                }
            }

            openList.Clear();
            closedList.Clear();
#if PBDEBUG
            if (path != null)
                Debug.Log(string.Format(
                    "Found in {0} iteration the path: {1}", iterationCount, path));
            else
            {
                Debug.Log(string.Format(
                    "Found no path in {0} iteration.", iterationCount));
            }
#endif
            return path;
        }

        private void Expand(NavConnection conn, NavGraphNode vert, Vector2 goalPos)
        {
            if (float.IsPositiveInfinity(conn.traversalCosts) || closedList.Contains(conn.end))
                return;


            float costSoFar = vert.pathValues[pathValueId].costSoFar + conn.traversalCosts;
            var pathValue = conn.end.pathValues[pathValueId];

            bool isOpen = openList.Contains(pathValue);
            if (isOpen && pathValue.costSoFar <= costSoFar)
            {
                // we know about this vert and have a faster way of accessing it
                return;
            }

            pathValue.parent = vert;
            pathValue.costSoFar = costSoFar;

            float estimate = conn.end.HeuristicalCostsToGoal(vert.cluster.owner.WorldToLocal.MultiplyPoint3x4(goalPos));
            if (estimate > maxHeuristicAllowed)
                estimate = maxHeuristicAllowed;
            pathValue.estimatedFutherPath = estimate;
            float totalCosts = costSoFar + estimate;
            if (isOpen)
            {
                openList.UpdatePriority(pathValue, totalCosts);
            }
            else
            {
                openList.Enqueue(pathValue, totalCosts);
            }
        }

        private void ExpandZeroH(NavConnection conn, NavGraphNode vert)
        {
            if (float.IsPositiveInfinity(conn.traversalCosts) || closedList.Contains(conn.end))
                return;

            float costSoFar = vert.pathValues[pathValueId].costSoFar + conn.traversalCosts;
            var pathValue = conn.end.pathValues[pathValueId];

            bool isOpen = openList.Contains(pathValue);
            if (isOpen && pathValue.costSoFar <= costSoFar)
            {
                // we know about this vert and have a faster way of accessing it
                return;
            }

            pathValue.parent = vert;
            pathValue.costSoFar = costSoFar;

            if (isOpen)
            {
                openList.UpdatePriority(pathValue, costSoFar);
            }
            else
            {
                openList.Enqueue(pathValue, costSoFar);
            }
        }

        private Path GatherPath(NavGraphNode startVert, NavGraphNode goalVert)
        {
            int segCount = 1;
            NavGraphNode vert = goalVert;
            PathSegment lastSeg = new PathSegment(goalVert.Position(), Vector2.zero, goalVert.cluster.owner.navSurface, null, vert.cluster);
            PathSegment prevSeg = lastSeg;

            while ((vert = vert.pathValues[pathValueId].parent) != startVert)
            {
                var seg = new PathSegment(
                    vert.Position(),
                    vert.LinkTarget.GetPositionAlongSegment(vert.LinkTargetT),
                    vert.cluster.owner.navSurface,
                    vert.link,
                    vert.cluster);
                seg.Next = prevSeg;
                prevSeg = seg;

                segCount++;
            }
            return new Path(prevSeg, lastSeg, startVert.Position(), segCount, goalVert.pathValues[pathValueId].costSoFar);
        }

        private float CheckHeuristicValidity(NavGraphNode startVert, NavGraphNode goalVert)
        {
            float totalCosts = goalVert.pathValues[pathValueId].costSoFar;
            NavGraphNode vert = goalVert;
            while ((vert = vert.pathValues[pathValueId].parent) != startVert)
            {
                if (vert.pathValues[pathValueId].estimatedFutherPath > totalCosts)
                {
                    return totalCosts;
                }
            }
            return 0;
        }

        private void UpdateClosestPositionToGoal(NavGraphNode node, Vector2 goalPos, ref float closestH)
        {
            float minDist = node.cluster.PointDistance(goalPos);
            if (minDist < closestH)
            {
                closestH = minDist;
                float t = node.cluster.DistanceOfPointAlongSegment(node.cluster.owner.WorldToLocal.MultiplyPoint3x4(goalPos));
                if (node.cluster.CanAgentReachPoint(agent, node.t, t))
                {
                    closestReachablePosition = new NavSegmentPositionPointer(node.cluster.owner.navSurface, node.cluster, t);
                }
            }
        }
    }
}
