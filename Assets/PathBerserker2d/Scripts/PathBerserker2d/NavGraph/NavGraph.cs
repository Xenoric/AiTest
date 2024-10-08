using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.XR.WSA;

namespace PathBerserker2d
{
    public enum NavGraphChange
    {
        NavSurfaceAdded,
        NavSurfaceRemoved,
        NavLinkAdded,
        NavLinkRemoved,
        SegmentModifierAdded,
        SegmentModifierRemoved,
        NavLinkMoved
    }

    public interface INavGraphChangeSource
    {
        /// <summary>
        /// Called after a group of changes has been applied to the NavGraph. When this is called, the changes are already applied and ready for use. For example NavSurfaceAdded means that that NavSurface is ready to be pathfinded on from now on, link mapping will work etc.
        /// This may be called multiple times for the segment modifier related events as one SegmentModifier GameObject can produce multiple SegmentModifier changes.
        /// Parameters:
        /// - Change type
        /// - the PBComponentId of the component (NavSurface, Link, SegmentModifier, ...) had. PBComponentId is a custom id that PathBerserker gives each component. Each component (NavSurface, Link, SegmentModifier, ...) has a PBComponentId property you can query to get its id.
        /// </summary>
        event Action<NavGraphChange, int> OnGraphChange;
    }

    internal class NavGraph : INavGraphChangeSource
    {
        public ReaderWriterLock graphLock = new ReaderWriterLock();
        public event Action<NavGraphChange, int> OnGraphChange;

        internal readonly Dictionary<NavSurface, NavSurfaceRecord> segmentTrees = new Dictionary<NavSurface, NavSurfaceRecord>();

        List<IGraphChange> changes = new List<IGraphChange>(20);
        List<AddNavSurfaceChange> stagedNewSurfaces = new List<AddNavSurfaceChange>(4);

        private int pathfinderThreadCount;

        public NavGraph(int pathfinderThreadCount)
        {
            this.pathfinderThreadCount = pathfinderThreadCount;
        }

        public void AddNavSurface(NavSurface surface)
        {
            var addSurface = new AddNavSurfaceChange(surface, pathfinderThreadCount);
            if (!stagedNewSurfaces.Any(item => item.surface == surface))
                stagedNewSurfaces.Add(addSurface);
            changes.Add(addSurface);
        }

        public void RemoveNavSurface(NavSurface surface)
        {
            changes.Add(new RemoveNavSurfaceChange(surface));
        }

        public void AddNavLink(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal)
        {
            changes.Add(new AddNavLinkChange(link, start, goal));
        }

        public void RemoveNavLink(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal)
        {
            changes.Add(new RemoveNavLinkChange(link, start, goal));
        }

        public void MoveNavLinkStart(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal, NavSegmentPositionPointer oldStart)
        {
            changes.Add(new MoveNavLinkStartChange(link, start, goal, oldStart));
        }

        public void MoveNavLinkGoal(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal, NavSegmentPositionPointer oldGoal)
        {
            changes.Add(new MoveNavLinkGoalChange(link, start, goal, oldGoal));
        }

        public void AddSegmentModifier(NavAreaMarkerInstance modifier)
        {
            changes.Add(new AddSegmentModifierChange(modifier));
        }

        public void RemoveSegmentModifier(NavAreaMarkerInstance modifier)
        {
            changes.Add(new RemoveSegmentModifierChange(modifier));
        }

        public void Update()
        {
            // update navsurface position matricies
            foreach (var entry in segmentTrees)
            {
                if (entry.Key != null)
                    entry.Value.LocalToWorld = entry.Key.LocalToWorldMatrix;
            }

            if (changes.Count + stagedNewSurfaces.Count == 0)
                return;

            graphLock.AcquireWriterLock(-1);
            try
            {
                for (int i = 0; i < changes.Count; i++)
                    changes[i].Apply(this);
            }
            finally
            {
                graphLock.ReleaseWriterLock();
            }

            // fire events
            if (OnGraphChange != null)
            {
                for (int i = 0; i < changes.Count; i++)
                {
                    try
                    {
                        OnGraphChange(changes[i].ChangeType, changes[i].ChangeSource);
                    }
                    catch (Exception e)
                    {
                        // we don't want listener exceptions to take down the pathfinding system with it
                        Debug.LogError(e);
                    }
                }
            }

            stagedNewSurfaces.Clear();
            changes.Clear();
        }

        public void ForceApplyChanges()
        {
            for (int i = 0; i < changes.Count; i++)
                changes[i].Apply(this);

            stagedNewSurfaces.Clear();
            changes.Clear();
        }

        public bool TryMapAgent(Vector2 position, NavSegmentPositionPointer pointer, NavAgent agent, out NavSegmentPositionPointer result)
        {
            if (!pointer.IsInvalid() && agent.CouldBeLocatedAt(pointer))
            {
                NavGraphNodeCluster cluster;
                if (TryGetClusterAt(pointer, out cluster))
                {
                    Vector2 localPos = pointer.surface.WorldToLocal(position);
                    float t = cluster.DistanceOfPointAlongSegment(localPos);
                    Vector2 proj = cluster.GetPositionAlongSegment(t);
                    float dist = (proj - localPos).sqrMagnitude;

                    if (dist < 0.001f)
                    {
                        result = new NavSegmentPositionPointer(pointer.surface, pointer.cluster, cluster.DistanceOfPointAlongSegment(localPos));
                        return true;
                    }
                }
            }

            return TryMapPoint(position, agent.CouldBeLocatedAt, out result);
        }

        public bool TryMapPoint(Vector2 position, out NavSegmentPositionPointer pointer)
        {
            return TryMapPoint(position, (p) => true, out pointer);
        }

        public bool TryMapPoint(Vector2 position, Func<NavSegmentPositionPointer, bool> pointFilter, out NavSegmentPositionPointer pointer)
        {
            return TryMapPoint(position, pointFilter, PathBerserker2dSettings.PointMappingDistance, out pointer);
        }

        public bool TryMapPoint(Vector2 position, Func<NavSegmentPositionPointer, bool> pointFilter, float pointMapDistance, out NavSegmentPositionPointer result)
        {
            float halfPointMapDistance = pointMapDistance / 2f;
            Rect queryAABB = new Rect(position - new Vector2(halfPointMapDistance, halfPointMapDistance), new Vector2(pointMapDistance, pointMapDistance));
            float minDistance = pointMapDistance;
            result = NavSegmentPositionPointer.Invalid;
            foreach (var pair in segmentTrees)
            {
                TryMapPointSurface(pair.Key, pair.Value.Clusters, queryAABB, position, pointFilter, ref minDistance, ref result);
            }

            return result.IsValid();
        }

        public bool TryMapPointWithStaged(Vector2 position, out NavSegmentPositionPointer result)
        {
            if (TryMapPoint(position, out result))
                return true;

            float pointMapDistance = PathBerserker2dSettings.PointMappingDistance;
            float halfPointMapDistance = pointMapDistance / 2f;
            Rect queryAABB = new Rect(position - new Vector2(halfPointMapDistance, halfPointMapDistance), new Vector2(pointMapDistance, pointMapDistance));
            float minDistance = pointMapDistance;
            result = NavSegmentPositionPointer.Invalid;
            foreach (var change in stagedNewSurfaces)
            {
                TryMapPointSurface(change.surface, change.record.Clusters, queryAABB, position, (p) => true, ref minDistance, ref result);
            }

            return result.IsValid();
        }

        public List<NavSubsegmentPointer> BoxCast(Rect rect, float rotation, float filterAngleFrom, float filterAngleTo)
        {
            List<NavSubsegmentPointer> results = new List<NavSubsegmentPointer>();

            // find bb of rect
            Vector2[] rotCorn = ExtendedGeometry.RotateRectangle(rect, rotation);

            Vector2 min = Vector2.Min(Vector2.Min(Vector2.Min(rotCorn[0], rotCorn[1]), rotCorn[2]), rotCorn[3]);
            Vector2 max = Vector2.Max(Vector2.Max(Vector2.Max(rotCorn[0], rotCorn[1]), rotCorn[2]), rotCorn[3]);
            Rect boundingRect = new Rect(min, max - min);

            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation));

            foreach (var pair in segmentTrees)
            {
                BoxCastSurface(pair.Key, pair.Value.Clusters, rect, boundingRect, rotationMatrix, filterAngleFrom, filterAngleTo, ref results);
            }
            return results;
        }

        public List<NavSubsegmentPointer> BoxCastWithStaged(Rect rect, float rotation, float filterAngleFrom, float filterAngleTo)
        {
            List<NavSubsegmentPointer> results = BoxCast(rect, rotation, filterAngleFrom, filterAngleTo);

            // find bb of rect
            Vector2[] rotCorn = ExtendedGeometry.RotateRectangle(rect, rotation);

            Vector2 min = Vector2.Min(Vector2.Min(Vector2.Min(rotCorn[0], rotCorn[1]), rotCorn[2]), rotCorn[3]);
            Vector2 max = Vector2.Max(Vector2.Max(Vector2.Max(rotCorn[0], rotCorn[1]), rotCorn[2]), rotCorn[3]);
            Rect boundingRect = new Rect(min, max - min);

            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, rotation));

            foreach (var change in stagedNewSurfaces)
            {
                BoxCastSurface(change.surface, change.record.Clusters, rect, boundingRect, rotationMatrix, filterAngleFrom, filterAngleTo, ref results);
            }
            return results;
        }

        [Obsolete("Use TryMapPoint instead")]
        public bool TryFindClosestPointTo(Vector2 position, float maxMappingDistance, out NavSegmentPositionPointer pointer)
        {
            return TryMapPoint(position, (p) => true, maxMappingDistance, out pointer);
        }

        public Vector2 GetRandomPointOnGraph()
        {
            if (segmentTrees.Count == 0)
                return Vector2.zero;

            var surf = Utility.WeightedRandomChoice(segmentTrees.Keys,
                segmentTrees.Keys.Select(s => s.TotalLineLength));

            var seg = Utility.WeightedRandomChoice(surf.NavSegments,
                surf.NavSegments.Select(s => s.Length), surf.TotalLineLength);

            var t = UnityEngine.Random.Range(0, seg.Length);

            return surf.LocalToWorld(seg.GetPositionAlongSegment(t));
        }

        private void TryMapPointSurface(NavSurface surface, B2DynamicTree<NavGraphNodeCluster> tree, Rect queryAABB, Vector2 position, Func<NavSegmentPositionPointer, bool> filter, ref float minDistance, ref NavSegmentPositionPointer result)
        {
            if (!surface.WorldBounds.Overlaps(queryAABB))
            {
                return;
            }

            Vector2 localPoint = surface.WorldToLocal(position);
            Rect localQuery = new Rect(surface.WorldToLocal(queryAABB.position), queryAABB.size);

            var iterator = tree.Query(localQuery);
            while (iterator.MoveNext())
            {
                var clusterCandidate = tree.GetUserData(iterator.Current);
                float t = clusterCandidate.DistanceOfPointAlongSegment(localPoint);
                Vector2 proj = clusterCandidate.GetPositionAlongSegment(t);
                float dist = (proj - localPoint).magnitude;

                if (dist >= minDistance)
                    // closest point is already to far away
                    continue;

                if (filter(new NavSegmentPositionPointer(surface, clusterCandidate, t)))
                {
                    minDistance = dist;
                    result = new NavSegmentPositionPointer(surface, clusterCandidate, t);
                }
                else
                {
                    // sub sample segment
                    for (t = 0; t < clusterCandidate.Length; t += 0.5f)
                    {
                        if (filter(new NavSegmentPositionPointer(surface, clusterCandidate, t)))
                        {
                            proj = clusterCandidate.GetPositionAlongSegment(t);
                            dist = (proj - localPoint).magnitude;

                            if (dist >= minDistance)
                                continue;

                            result = new NavSegmentPositionPointer(surface, clusterCandidate, t);
                            minDistance = dist;
                            break;
                        }
                    }
                }
            }
        }

        private void BoxCastSurface(NavSurface surface, B2DynamicTree<NavGraphNodeCluster> tree, Rect rect, Rect boundingRect, Matrix4x4 rotationMatrix, float filterAngleFrom, float filterAngleTo, ref List<NavSubsegmentPointer> results)
        {
            if (!surface.WorldBounds.Overlaps(boundingRect))
                return;

            Rect localQuery = new Rect(boundingRect);
            localQuery.center = surface.WorldToLocal(localQuery.center);

            Rect localCast = new Rect(rect);
            //localCast.center = surface.WorldToLocal(localCast.center);

            float u1, u2;
            var iterator = tree.Query(localQuery);
            while (iterator.MoveNext())
            {
                var segCandidate = tree.GetUserData(iterator.Current);

                float angle = Vector2.SignedAngle(segCandidate.Tangent, Vector2.up);
                if (!ExtendedGeometry.IsAngleBetweenAngles(filterAngleFrom, filterAngleTo, angle))
                    continue;

                // test for rect intersection
                Vector2 rotatedStart = rotationMatrix * surface.LocalToWorld(segCandidate.Start);
                Vector2 rotatedEnd = rotationMatrix * surface.LocalToWorld(segCandidate.End);
                if (ExtendedGeometry.RectLineIntersection(localCast, rotatedStart, rotatedEnd, out u1, out u2))
                {
                    u1 = u1 < 0 ? 0 : u1;
                    u2 = u2 > 1 ? 1 : u2;
                    u1 *= segCandidate.Length;
                    u2 *= segCandidate.Length;

                    results.Add(new NavSubsegmentPointer(surface, iterator.Current, u1, u2 - u1));
                }
            }
        }

        private void InternalAddNavSurface(NavSurface surface,
            NavSurfaceRecord record, int[] proxyIndecies)
        {
            if (segmentTrees.ContainsKey(surface))
            {
#if PBDEBUG
                Debug.Log("AddNavSurface called but surface was already added!");
#endif
                return;
            }

            // add corner links for connected segments
            for (int iSeg = 0; iSeg < surface.NavSegments.Count; iSeg++)
            {
                var seg = surface.NavSegments[iSeg];
                var cluster = record.Clusters.GetUserData(proxyIndecies[iSeg]);

                if (seg.HasNext)
                {
                    var goalCluster = record.Clusters.GetUserData(proxyIndecies[seg.NextSegmentIndex]);

                    AddCornerLink(cluster, cluster.Length, surface, goalCluster, 0);
                }
                if (seg.HasPrev)
                {
                    var goalCluster = record.Clusters.GetUserData(proxyIndecies[seg.PrevSegmentIndex]);

                    AddCornerLink(cluster, 0, surface, goalCluster, goalCluster.Length);
                }
            }

            // add corner links for touching segments
            for (int iSeg = 0; iSeg < surface.NavSegments.Count; iSeg++)
            {
                var seg = surface.NavSegments[iSeg];
                int proxyIndex = proxyIndecies[iSeg];
                var startCluster = record.Clusters.GetUserData(proxyIndex);

                CreateCornerLinksForClosePoints(seg.Start, startCluster, proxyIndex, record.Clusters, proxyIndecies, surface);
                CreateCornerLinksForClosePoints(seg.End, startCluster, proxyIndex, record.Clusters, proxyIndecies, surface);
            }

            segmentTrees.Add(surface, record);
        }

        private void AddCornerLink(NavGraphNodeCluster startCluster, float startT, NavSurface startSurface, NavGraphNodeCluster goalCluster, float goalT)
        {
            float angle = Vector2.Angle(startCluster.Normal, goalCluster.Normal);
            angle = angle < 0 ? 360 - angle : angle;

            var start = new NavSegmentPositionPointer(startSurface, startCluster, startT);
            var goal = new NavSegmentPositionPointer(startSurface, goalCluster, goalT);

            startCluster.AddNode(pathfinderThreadCount, start.t, goal.cluster, goal.t, new CornerLink(start, goal, angle));
        }

        private void CreateCornerLinksForClosePoints(Vector2 point, NavGraphNodeCluster pointOwner, int pointOwnerProxyIndex, B2DynamicTree<NavGraphNodeCluster> clusterTree, int[] proxyIndicies, NavSurface surface)
        {
            int prevSegProxyIndex = pointOwner.HasPrev ? proxyIndicies[pointOwner.PrevSegmentIndex] : -1;

            Vector2 queryRectSize = new Vector2(0.01f, 0.01f);
            Rect query = new Rect(point - queryRectSize, point + queryRectSize);
            var iterator = clusterTree.Query(query);

            while (iterator.MoveNext())
            {
                if (prevSegProxyIndex == iterator.Current || iterator.Current == pointOwnerProxyIndex)
                    continue;

                var goalCluster = clusterTree.GetUserData(iterator.Current);
                if (goalCluster.PointDistance(point) < 0.01f)
                {
                    float startT = pointOwner.DistanceOfPointAlongSegment(point);
                    float goalT = goalCluster.DistanceOfPointAlongSegment(point);

                    AddCornerLink(pointOwner, startT, surface, goalCluster, goalT);
                    AddCornerLink(goalCluster, goalT, surface, pointOwner, startT);
                }
            }
        }

        private bool DoesLinkExist(INavLinkInstance link, NavSegmentPositionPointer start)
        {
            NavGraphNodeCluster cluster;
            if (!TryGetClusterAt(start, out cluster))
            {
                return false;
            }
            return cluster.DoesNodeExist(link);
        }

        private void InternalRemoveNavSurface(NavSurface surface)
        {
            NavSurfaceRecord record = null;
            try
            {
                record = segmentTrees[surface];

            }
            catch (KeyNotFoundException)
            {
#if PBDEBUG
                Debug.Log("RemoveNavSurface called but surface doesn't exist!");
#endif
                return;
            }
            record.Destroy(this);
            segmentTrees.Remove(surface);
        }

        private void InternalAddNavLink(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal)
        {
            start.cluster.AddNode(
                 pathfinderThreadCount,
                 start.t,
                 goal.cluster,
                 goal.t,
                 link);

            CreateSoftRefLink(link, start.surface);
            if (start.surface != goal.surface)
            {
                CreateSoftRefLink(link, goal.surface);
            }
        }

        private void CreateSoftRefLink(INavLinkInstance link, NavSurface targetSurface)
        {
            try
            {
                segmentTrees[targetSurface].AddSoftRefLink(link);
            }
            catch (KeyNotFoundException)
            {
                // must be still staged
                for (int i = 0; i < stagedNewSurfaces.Count; i++)
                {
                    if (stagedNewSurfaces[i].surface == targetSurface)
                    {
                        stagedNewSurfaces[i].record.AddSoftRefLink(link);
                        return;
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        private void RemoveSoftRefLink(INavLinkInstance link, NavSurface targetSurface)
        {
            if (targetSurface == null)
                return;
            try
            {
                segmentTrees[targetSurface].RemoveSoftRefLink(link);
            }
            catch (KeyNotFoundException)
            {
                // must be still staged
                for (int i = 0; i < stagedNewSurfaces.Count; i++)
                {
                    if (stagedNewSurfaces[i].surface == targetSurface)
                    {
                        stagedNewSurfaces[i].record.RemoveSoftRefLink(link);
                        return;
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        internal void InternalRemoveNavLink(INavLinkInstance link, NavSegmentPositionPointer start,
        NavSegmentPositionPointer goal)
        {
            if (start.surface == null || !DoesLinkExist(link, start))
                return;

            start.cluster.RemoveNode(link);
            RemoveSoftRefLink(link, start.surface);
            if (start.surface != goal.surface && segmentTrees.ContainsKey(goal.surface))
                RemoveSoftRefLink(link, goal.surface);

            link.OnRemove();
        }

        private void InternalMoveNavLinkStart(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal, NavSegmentPositionPointer oldStart)
        {
            if (oldStart.surface == start.surface && oldStart.cluster == start.cluster)
            {
                oldStart.cluster.MoveNode(link, start.t);
            }
            else
            {
                oldStart.cluster.RemoveNode(link);
                RemoveSoftRefLink(link, oldStart.surface);
                if (oldStart.surface != goal.surface)
                    RemoveSoftRefLink(link, goal.surface);

                InternalAddNavLink(link, start, goal);
            }
        }

        private void InternalMoveNavLinkGoal(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal, NavSegmentPositionPointer oldGoal)
        {
            var node = start.cluster.GetNode(link);
            if (oldGoal.surface != goal.surface || oldGoal.cluster != goal.cluster)
                node.LinkTarget = goal.cluster;
            node.LinkTargetT = goal.t;

            if (goal.surface != oldGoal.surface)
            {
                if (start.surface != oldGoal.surface)
                    RemoveSoftRefLink(link, oldGoal.surface);

                if (start.surface != goal.surface)
                    CreateSoftRefLink(link, goal.surface);
            }
        }

        private void InternalAddSegmentModifier(NavAreaMarkerInstance mod)
        {
            if (TryGetClusterAt(mod.position, out var cluster))
                cluster.AddNodeClusterModifier(mod);
        }

        private void InternalRemoveSegmentModifier(NavAreaMarkerInstance mod)
        {
            if (TryGetClusterAt(mod.position, out var cluster))
                cluster.RemoveNodeClusterModifier(mod);
        }

        public bool TryGetClusterAt(NavSegmentPositionPointer tPoint, out NavGraphNodeCluster cluster)
        {
            if (tPoint.IsInvalid() || !segmentTrees.TryGetValue(tPoint.surface, out _))
            {
                cluster = null;
                return false;
            }

            cluster = tPoint.cluster;
            return true;
        }

        public bool TryGetClusterAt(NavSubsegmentPointer tPoint, out NavGraphNodeCluster cluster)
        {
            NavSurfaceRecord navRec;
            if (tPoint.IsInvalid() || !segmentTrees.TryGetValue(tPoint.surface, out navRec))
            {
                cluster = null;
                return false;
            }

            cluster = navRec.Clusters.GetUserData(tPoint.proxyDataIndex);
            return true;
        }

        interface IGraphChange
        {
            NavGraphChange ChangeType { get; }
            int ChangeSource { get; }
            void Apply(NavGraph graph);
        }

        class AddNavSurfaceChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.NavSurfaceAdded;
            public int ChangeSource { get; }

            public readonly NavSurfaceRecord record;
            public readonly NavSurface surface;
            int[] proxyIndecies;

            public AddNavSurfaceChange(NavSurface surface, int threadCount)
            {
                this.surface = surface;
                this.ChangeSource = surface.PBComponentId;

                var tree = new B2DynamicTree<NavGraphNodeCluster>(surface.NavSegments.Count + 10);

                proxyIndecies = new int[surface.NavSegments.Count];
                record = new NavSurfaceRecord(tree, surface.LocalToWorldMatrix, surface);
                int fill = 0;
                foreach (var seg in surface.NavSegments)
                {
                    proxyIndecies[fill++] = tree.CreateProxy(seg.AABB, new NavGraphNodeCluster(seg, threadCount, record));
                }
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Added surface to graph");
#endif
                graph.InternalAddNavSurface(surface, record, proxyIndecies);
            }
        }

        class RemoveNavSurfaceChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.NavSurfaceRemoved;
            public int ChangeSource { get; }

            NavSurface surface;
            GameObject changeSource;

            public RemoveNavSurfaceChange(NavSurface surface)
            {
                this.surface = surface;
                this.ChangeSource = surface.PBComponentId;
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Removed surface from graph");
#endif
                graph.InternalRemoveNavSurface(surface);
            }
        }

        class AddNavLinkChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.NavLinkAdded;
            public int ChangeSource { get; }

            INavLinkInstance link;
            NavSegmentPositionPointer start;
            NavSegmentPositionPointer goal;

            public AddNavLinkChange(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal)
            {
                this.link = link;
                this.start = start;
                this.goal = goal;
                ChangeSource = link.PBComponentId;
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Added link to graph");
#endif
                graph.InternalAddNavLink(link, start, goal);
            }
        }

        class RemoveNavLinkChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.NavLinkRemoved;
            public int ChangeSource { get; }

            INavLinkInstance link;
            NavSegmentPositionPointer start;
            NavSegmentPositionPointer goal;

            public RemoveNavLinkChange(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal)
            {
                this.link = link;
                this.start = start;
                this.goal = goal;
                ChangeSource = link.PBComponentId;
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Removed link from graph");
#endif
                graph.InternalRemoveNavLink(link, start, goal);
            }
        }

        class MoveNavLinkStartChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.NavLinkMoved;
            public int ChangeSource { get; }

            INavLinkInstance link;
            NavSegmentPositionPointer start;
            NavSegmentPositionPointer goal;
            NavSegmentPositionPointer oldStart;

            public MoveNavLinkStartChange(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal, NavSegmentPositionPointer oldStart)
            {
                this.link = link;
                this.start = start;
                this.goal = goal;
                this.oldStart = oldStart;
                ChangeSource = link.PBComponentId;
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Moved link start in graph");
#endif
                graph.InternalMoveNavLinkStart(link, start, goal, oldStart);
            }
        }

        class MoveNavLinkGoalChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.NavLinkMoved;
            public int ChangeSource { get; }

            INavLinkInstance link;
            NavSegmentPositionPointer start;
            NavSegmentPositionPointer goal;
            NavSegmentPositionPointer oldGoal;

            public MoveNavLinkGoalChange(INavLinkInstance link, NavSegmentPositionPointer start, NavSegmentPositionPointer goal, NavSegmentPositionPointer oldGoal)
            {
                this.link = link;
                this.start = start;
                this.goal = goal;
                this.oldGoal = oldGoal;
                ChangeSource = link.PBComponentId;
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Moved link goal in graph");
#endif
                graph.InternalMoveNavLinkGoal(link, start, goal, oldGoal);
            }
        }

        class AddSegmentModifierChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.SegmentModifierAdded;
            public int ChangeSource { get; }

            NavAreaMarkerInstance mod;

            public AddSegmentModifierChange(NavAreaMarkerInstance mod)
            {
                this.mod = mod;
                ChangeSource = mod.PBComponentId;
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Added modifier to graph");
#endif
                graph.InternalAddSegmentModifier(mod);
            }
        }

        class RemoveSegmentModifierChange : IGraphChange
        {
            public NavGraphChange ChangeType => NavGraphChange.SegmentModifierRemoved;
            public int ChangeSource { get; }

            NavAreaMarkerInstance mod;

            public RemoveSegmentModifierChange(NavAreaMarkerInstance mod)
            {
                this.mod = mod;
                ChangeSource = mod.PBComponentId;
            }

            public void Apply(NavGraph graph)
            {
#if PBDEBUG
                Debug.Log("Removed modifier to graph");
#endif
                graph.InternalRemoveSegmentModifier(mod);
            }
        }
    }
}
