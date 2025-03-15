using UnityEngine;
using System;
using System.Collections.Generic;

namespace PathBerserker2d
{
    /// <summary>
    /// Represents a pathfinding entity.
    /// </summary>
    /// <remarks>
    /// This components handles the interaction with the asynchronous pathfinding system.
    /// It assumes the agent is a point located at <c>transform.position</c>. 
    /// Automatic movement will directly modify the transform this script is attached to. See \ref navagent_movement "NavAgent's build-in movement" for more detail on movement.
    /// ## States
    /// At heart the NavAgent is a state machine with the following states:
    /// <list type="bullet">
    ///     <item>
    ///         <c>Idle</c>\n
    ///         <description>
    ///         In this state the agent does nothing and is ready to path to a new location.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <c>Planning</c>\n
    ///         <description>
    ///         The agent has made a <see cref="PathRequest">path request</see> and is now waiting for its result.
    ///         A call to <see cref="PathTo"/> for example would make the agent switch to this state.
    ///         If the path calculation succeeded, the agent switches into the <c>FollowPath</c> state.
    ///         If it didn't succeed however, the agent will switch back into the <c>Idle</c> state.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <c>FollowPath</c>\n
    ///         <description>
    ///         The agent will follow the a previously calculated path. Depending on whether <see cref="autoSegmentMovement"/> or <see cref="autoLinkMovement"/> is set,
    ///         the path will be followed automatically. The agent has build-in ways to traverse the build-in link types. They don't make use of the physics system.
    ///         The path will be recalculated at a set interval determined by <see cref="autoRepathIntervall"/>. This is to ensure the path is up to date with changes in the world.
    ///         No reactions to changes in the world between recalculations is possible.
    ///
    ///         Following a path is further subdivided into three states:
    ///         <list>
    ///             <item>
    ///                <c>Segment movement</c>\n
    ///                <description>
    ///                The agent moves on a line segment.
    ///                If you move the agent manually, call <see cref="CompleteSegmentTraversal"/> to switch to the next state.
    ///                </description>
    ///             </item>
    ///             <item>
    ///                <c>Wait for link</c>\n
    ///                <description>
    ///                This state is only entered if after the agent finshes moving on a segment, the link it wants to take is currently not traversable.
    ///                The agent will wait for the link to become traversable again.
    ///                </description>
    ///             </item>
    ///             <item>
    ///                <c>Traverse link</c>\n
    ///                <description>
    ///                The agent will move on the link.
    ///                If you move the agent manually, call <see cref="CompleteLinkTraversal"/> to begin traversing the next segment.
    ///                </description>
    ///             </item>
    ///         </list>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <c>LostPath</c>\n
    ///         <description>
    ///         The agent was previously in the <c>FollowPath</c> state, but <see cref="LostPath"/> was called.
    ///         In this state, the agent will periodically attempt to find a path to its last goal.
    ///         This is useful, for if the agent unexpectedly was moved of its previously followed path, it can still attempt to reach its goal. 
    ///         This state is only entered, when in state <c>FollowPath</c> and <see cref="LostPath"/> is called. 
    ///         </description>
    ///     </item>
    /// </list>
    /// ## Pathfinding properties
    /// A NavAgent has a few properties relevant to the pathfinder.
    /// <list>
    ///     <item>
    ///         <see cref="height"/>\n
    ///         <description>
    ///         Only segments and links with enough free space are considered.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <see cref="maxSlopeAngle"/>\n
    ///         <description>
    ///         Only segments that don't exceed this angle are considered traversable.
    ///         0° = ground, 90° = straight walls and 180° = ceiling.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <see cref="linkTraversalCostMultipliers"/>\n
    ///         <description>
    ///         For each link one multiplier from this array is applied to its traversal costs.
    ///         With multiplier values <= 0 you can completely exclude certain link types from traversal.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <see cref="navTagTraversalCostMultipliers"/>\n
    ///         <description>
    ///         Links and parts of segments can be tagged with a NavTag. NavTag with index 0 is considered the default and applied to all segments
    ///         that don't have another NavTag.
    ///         As with <see cref="linkTraversalCostMultipliers"/> multiplier values <= 0 completely exclude NavTags from traversal.
    ///         </description>
    ///     </item>
    /// </list>
    /// </remarks>
    [AddComponentMenu("PathBerserker2d/Nav Agent")]
    [ScriptExecutionOrder(-50)]
    public class NavAgent : MonoBehaviour
    {
        public enum State
        {
            /// Agent is doing nothing.
            Idle,
            /// Agent is following a path.
            FollowPath,
        }

        private enum MovementState
        {
            OnSegment,
            OnLink,
            WaitForLinkOnSegment,
        }

        public State CurrentStatus => status;
        public bool IsIdle => status == State.Idle && currentPathRequest?.Status == PathRequest.RequestState.Draft;
        public bool IsFollowingAPath => status == State.FollowPath;

        public float Height => height;
        public float MaxSlopeAngle => maxSlopeAngle;
        public bool IsOnLink => IsFollowingAPath && movementState == MovementState.OnLink;
        public bool IsMovingOnSegment => IsFollowingAPath && movementState == MovementState.OnSegment;
        public bool IsWaitingForLink => IsFollowingAPath && movementState == MovementState.WaitForLinkOnSegment;

        /// <summary>
        /// If true, either IsMovingOnSegment is true, or the agent is waiting to traverse an untraversable link.
        /// </summary>
        public bool IsOnSegment => IsFollowingAPath && movementState != MovementState.OnLink;

        /// <summary>
        /// Check, if the current mapped position of the agent is valid. The mapped position can only be valid, if the agent is close to the ground. (Close, not necessarily directly on the ground)
        /// </summary>
        public bool HasValidPosition => !currentMappedPosition.IsInvalid();

        /// <summary>
        /// True, if Stop() was called and agent hasn't yet stopped
        /// </summary>
        public bool IsStopping => stopRequested;

        /// <summary>
        /// True, if the agent is on the last segment of its path. 
        /// </summary>
        public bool IsOnGoalSegment => IsOnSegment && !Path.HasNext;

        /// <summary>
        /// Link of the path segment the agent is on, or null.
        /// </summary>
        [Obsolete("Use CurrentPathSegment.link")]
        public INavLinkInstance CurrentLink => currentPath?.Current.link ?? null;

        /// <summary>
        /// Link type of the path segment the agent is on, or ""
        /// </summary>
        [Obsolete("Use CurrentPathSegment.link.LinkTypeName")]
        public string CurrentLinkType => currentPath?.Current.link?.LinkTypeName ?? "";

        /// <summary>
        /// Link start of the path segment the agent is on, or Vector2.zero. Can change each frame, if the link start is on a moving platform.
        /// </summary>
        [Obsolete("Use CurrentPathSegment.LinkStart")]
        public Vector2 CurrentLinkStart => currentPath?.Current.LinkStart ?? Vector2.zero;

        /// <summary>
        /// Segment normal of the path segment the agent is or normal of the currently mapped position, or Vector2.up
        /// </summary>
        public Vector2 CurrentSegmentNormal => IsFollowingAPath ? currentPath.Current.Normal : (currentMappedPosition.IsValid() ? currentMappedPosition.Normal : Vector2.up);

        /// <summary>
        /// Current path segment if agent follows a path or null.
        /// </summary>
        public PathSegment CurrentPathSegment => currentPath?.Current;

        /// <summary>
        /// Segment normal of the next segment on the path, or Vector2.zero
        /// </summary>
        [Obsolete("Use CurrentPathSegment.Next.Normal")]
        public Vector2 NextSegmentNormal => currentPath?.NextSegment?.Normal ?? Vector2.zero;

        /// <summary>
        /// Current subgoal the agent is moving towards. May either be a link start or a link end. If it lies on a moving platform, the value may change from frame to frame.
        /// </summary>
        public Vector2 PathSubGoal
        {
            get
            {
                if (IsFollowingAPath)
                {
                    if (movementState == MovementState.OnLink)
                        return currentPath.Current.LinkEnd;
                    else
                        return currentPath.Current.LinkStart;
                }
                return Vector2.zero;
            }
        }
        /// <summary>
        /// Overall goal of the path the agent is on
        /// </summary>
        public Vector2? PathGoal => currentPath?.Goal;

        /// <summary>
        /// Shorthand for transform.position
        /// </summary>
        public Vector2 Position
        {
            get => transform.position;
            set => transform.position = new Vector3(value.x, value.y, transform.position.z);
        }

        /// <summary>
        /// Combination of all NavTags at the position of the agent.
        /// </summary>
        public int CurrentNavTagVector => IsFollowingAPath ? currentPath.Current.GetTagVector(Position) : (currentMappedPosition.cluster?.GetNavTagVector(currentMappedPosition.t) ?? 0);

        /// <summary>
        /// Time agent spend on link. Does not include waiting for link to become traversable.
        /// </summary>
        public float TimeOnLink { get; private set; }


        public delegate void FailedToFindPathDelegate(NavAgent agent);

        /// <summary>
        /// Fired when agent begins moving on a link.
        /// </summary>
        public event Action<NavAgent> OnStartLinkTraversal;

        /// <summary>
        /// Fired when agent is moving on a link.
        /// </summary>
        public event Action<NavAgent> OnLinkTraversal;

        /// <summary>
        /// Fired when agent start moving on a segment.
        /// </summary>
        public event Action<NavAgent> OnStartSegmentTraversal;

        /// <summary>
        /// Fired when agent is moving on a segment.
        /// </summary>
        public event Action<NavAgent> OnSegmentTraversal;

        /// <summary>
        /// Fired when the agent fails to find a path
        /// </summary>
        public event Action<NavAgent> OnFailedToFindPath;

        /// <summary>
        /// Fired when agent stops after Stop() or ForceStop() was called. For ForceStop() this happens instantly. For Stop() this happens after the agent stopped.
        /// </summary>
        public event Action<NavAgent> OnStop;

        /// <summary>
        /// Fired when agent reaches its current goal.
        /// </summary>
        public event Action<NavAgent> OnReachedGoal;

        /// <summary>
        /// Called when the agent starts following a new path.
        /// NOTE: Also called, after successfully recalculating a path, even if the path itself does not change.
        /// </summary>
        public event Action<NavAgent> OnStartFollowingNewPath;

        internal Path Path => currentPath;
        internal int NavTagMask => navTagMask;



        [Header("Pathplanning")]
        [SerializeField]
        // protect! should not be changeable, unless not making a path request
        private float height = 1;
        [SerializeField]
        // protect! should not be changeable, unless not making a path request
        // 90 = doesnt matter
        [Range(0, 180)]
        [Tooltip("Maximum slope the agent can walk on. 180 = unlimited")]
        private float maxSlopeAngle = 180;

        /// <summary>
        /// Delay in seconds between recalculations of current path. This enables the agent to react to changes in the world. Higher values are better for performance.
        /// </summary>
        [Tooltip("Interval at which an agent will recalculate its current path to react to world changes in seconds. Higher value improves performance.")]
        [SerializeField]
        float autoRepathIntervall = 1f;
        [Tooltip("Maximum distance an agent can be from the start of a calculated path, to start following it. If the distance is to large, the path is thrown out.")]
        [SerializeField]
        float maximumDistanceToPathStart = 0.7f;

        [SerializeField]
        float[] linkTraversalCostMultipliers;

        /// <summary>
        /// If true and no path exists between start and goal, the NavAgent will try to find a path to the closest reachable position instead. Does not work with multiple targets!
        /// </summary>
        [Tooltip("If true and no path exists between start and goal, will try to find a path to the closest reachable position instead. Does not work with multiple targets!")]
        [SerializeField]
        bool allowCloseEnoughPath = false;

        [Obsolete("Moved to out and into a separate component. Only for migration purposes.", true)]
        [Tooltip("Speed on segments in unit/s.")]
        [SerializeField]
        public float movementSpeed = 5;


        [Obsolete("Moved to out and into a separate component. Only for migration purposes.", true)]
        [Tooltip("Speed on corner links in degrees/s.")]
        [SerializeField]
        public float cornerSpeed = 100;

        [Obsolete("Moved to out and into a separate component. Only for migration purposes.", true)]
        [Tooltip("Speed on jump links in unit/s.")]
        [SerializeField]
        public float jumpSpeed = 5;

        [Obsolete("Moved to out and into a separate component. Only for migration purposes.", true)]
        [Tooltip("Speed on fall links in unit/s.")]
        [SerializeField]
        public float fallSpeed = 5;

        [Obsolete("Moved to out and into a separate component. Only for migration purposes.", true)]
        [Tooltip("Speed on climb links in unit/s.")]
        [SerializeField]
        public float climbSpeed = 5;

        [Tooltip("If true, will print debug messages.")]
        [SerializeField]
        public bool enableDebugMessages = false;


        /// <summary>
        /// Traversal cost multipliers for nav tags. A value less or equal to 0 prohibits the agent from traversing that tag.
        /// </summary>
        [SerializeField]
        float[] navTagTraversalCostMultipliers;


        [SerializeField, ReadOnly]
        private State status;

        [SerializeField, HideInInspector]
        private int navTagMask;

        internal NavSegmentPositionPointer currentMappedPosition;
        internal PathRequest currentPathRequest;

        private Path currentPath = null;
        private PathRequest repathPathRequest;
        private float lastRepathTime;
        private MovementState movementState;
        private bool traversedLinkSinceLastRepath;
        private bool traversedLinkSinceLastPath;
        private bool stopRequested;

        #region UNITY_METHODS
        private void OnEnable()
        {
            status = State.Idle;
            currentPathRequest = new PathRequest(this);
            repathPathRequest = new PathRequest(this);
        }

        private void Start()
        {
            UpdateMappedPosition();
        }

        private void OnValidate()
        {
            if (linkTraversalCostMultipliers == null)
                linkTraversalCostMultipliers = new float[0];
            if (navTagTraversalCostMultipliers == null)
                navTagTraversalCostMultipliers = new float[0];

            if (linkTraversalCostMultipliers.Length != PathBerserker2dSettings.NavLinkTypeNames.Length)
            {
                Utility.ResizeWithDefault(ref linkTraversalCostMultipliers, PathBerserker2dSettings.NavLinkTypeNames.Length, 1);
            }
            if (navTagTraversalCostMultipliers.Length != PathBerserker2dSettings.NavTags.Length)
            {
                Utility.ResizeWithDefault(ref navTagTraversalCostMultipliers, PathBerserker2dSettings.NavTags.Length, 1);
            }

            navTagMask = GetNavTagMask();
        }

        private void Update()
        {
            UpdateMappedPosition();
            HandlePathRequest();

            switch (status)
            {
                case State.FollowPath:
                    Repath();

                    if (movementState == MovementState.OnLink)
                    {
                        //check if link still exists
                        if (CurrentPathSegment.link == null)
                        {
                            // link was destroyed. Wait for repath
                            break;
                        }

                        TimeOnLink += Time.deltaTime;
                        OnLinkTraversal?.Invoke(this);
                    }
                    else if (movementState == MovementState.OnSegment)
                    {
                        if (stopRequested)
                        {
                            status = State.Idle;
                            stopRequested = false;
                            OnStop?.Invoke(this);
                        }
                        else
                        {
                            OnSegmentTraversal?.Invoke(this);
                        }
                    }
                    else
                    {
                        if (currentPath.Current.link.IsTraversable)
                        {
                            StartTraversingLink();
                        }
                    }
                    break;
            }
        }
        #endregion

        /// <summary>
        /// Repositions the agent at the nearest segment the agent could be standing at. Segments the agent could not be at do to its tag or slope will be ignored.
        /// </summary>
        /// <returns>True, if warping was successful</returns>
        public bool WarpToNearestSegment(float maximumWarpDistance = 10)
        {
            if (!currentMappedPosition.IsInvalid())
            {
                // already close enough
                this.Position = currentMappedPosition.Position;
                return true;
            }

            NavSegmentPositionPointer p;
            if (PBWorld.TryMapPoint(Position, maximumWarpDistance, this, out p))
            {
                this.Position = p.Position;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Starts the process of pathfinding to the closest of the given goals.
        /// NOTE: Do not call this method every frame. Calculating a path takes longer than a frame, so the agent will never start moving.
        /// </summary>
        /// <seealso cref="UpdatePath(Vector2[])"/>
        /// <param name="goals">Goals to pathfind to.</param>
        /// <returns>True, if the at least 1 goal and the agents own position could be mapped. This does not mean, that a path towards a goal exists.</returns>
        public bool PathTo(params Vector2[] goals)
        {
            Stop();
            return UpdatePath(goals);
        }

        /// <summary>
        /// Starts the process of pathfinding to the given goal.
        /// NOTE: Do not call this method every frame. Calculating a path takes longer than a frame, so the agent will never start moving.
        /// </summary>
        /// <seealso cref="UpdatePath(Vector2)"/>
        /// <param name="goals">Goals to pathfind to.</param>
        /// <returns>True, if the at least 1 goal and the agents own position could be mapped. This does not mean, that a path towards a goal exists.</returns>
        public bool PathTo(Vector2 goal)
        {
            Stop();
            return UpdatePath(goal);
        }

        /// <summary>
        /// Starts the process of pathfinding to the closest given goal.
        /// NOTE: Do not call this method every frame. Calculating a path takes longer than a frame, so the agent will never start moving.
        /// </summary>
        /// <returns>True, the agents own position could be mapped. This does not mean, that a path towards the goal exists.</returns>
        private bool PathTo(IList<NavSegmentPositionPointer> goalPs)
        {
            Stop();
            return UpdatePath(goalPs);
        }

        /// <summary>
        /// Starts the process of pathfinding to the closest of the given goals. Will continue moving until the calculations for the new path are completed.
        /// NOTE: Do not call this method every frame. Calculating a path takes longer than a frame, so the agent will never start moving.
        /// </summary>
        /// <seealso cref="PathTo(Vector2[])"/>
        /// <param name="goals">Goals to pathfind to.</param>
        /// <returns>True, if the at least 1 goal and the agents own position could be mapped. This does not mean, that a path towards a goal exists.</returns>
        public bool UpdatePath(params Vector2[] goals)
        {
            if (currentMappedPosition.IsInvalid())
                return false;

            List<NavSegmentPositionPointer> goalPs = new List<NavSegmentPositionPointer>(goals.Length);
            NavSegmentPositionPointer p;
            for (int i = 0; i < goals.Length; i++)
            {
                float maxDist = Vector2.Distance(Position, goals[i]) + 0.1f;
                if (PBWorld.TryMapPoint(goals[i], maxDist, out p) && (allowCloseEnoughPath || CouldBeLocatedAt(p)))
                {
                    goalPs.Add(p);
                }
            }
            return UpdatePath(goalPs);
        }

        /// <summary>
        /// Starts the process of pathfinding to the given goal. Will continue moving until the calculations for the new path are completed.
        /// NOTE: Do not call this method every frame. Calculating a path takes longer than a frame, so the agent will never start moving.
        /// </summary>
        /// <seealso cref="PathTo(Vector2)"/>
        /// <param name="goals">Goals to pathfind to.</param>
        /// <returns>True, if the at least 1 goal and the agents own position could be mapped. This does not mean, that a path towards a goal exists.</returns>
        public bool UpdatePath(Vector2 goal)
        {
            if (currentMappedPosition.IsInvalid())
                return false;

            float maxDist = Vector2.Distance(Position, goal) + 0.1f;
            NavSegmentPositionPointer p;
            if (!PBWorld.TryMapPoint(goal, maxDist, out p) || (!allowCloseEnoughPath && !CouldBeLocatedAt(p)))
                return false;

            return UpdatePath(new NavSegmentPositionPointer[] { p });
        }

        /// <summary>
        /// Simple distance check between agent and CurrentSubGoal.
        /// </summary>
        /// <returns>True, if distance is less than maxDist</returns>
        public bool HasReachedCurrentSubGoal(float maxDist = 0.05f)
        {
            Vector2 delta = PathSubGoal - Position;
            float distance = delta.magnitude;
            return distance < maxDist;
        }

        /// <summary>
        /// Starts the process of pathfinding to the closest given goal. Will continue moving until the calculations for the new path are completed.
        /// </summary>
        /// <returns>True, the agents own position could be mapped. This does not mean, that a path towards the goal exists.</returns>
        private bool UpdatePath(IList<NavSegmentPositionPointer> goalPs)
        {
            if (currentPathRequest.Status == PathRequest.RequestState.Pending)
                return false;

            if (goalPs.Count == 0)
                return false;

            if (currentMappedPosition.IsInvalid())
                return false;

            currentPathRequest.start = currentMappedPosition;
            currentPathRequest.goals = goalPs;
            PBWorld.PathTo(currentPathRequest);
            traversedLinkSinceLastPath = false;
            return true;
        }

        /// <summary>
        /// Start pathfinding to a random position on the NavGraph. It cannot grantee that this position is reachable. Does the agent might not move after this is called.
        /// </summary>
        /// <returns></returns>
        public bool SetRandomDestination()
        {
            if (currentMappedPosition.IsInvalid())
                return false;

            Vector2 goal = PBWorld.GetRandomPointOnGraph();
            return PathTo(goal);
        }


        /// <summary>
        /// If you implement link traversal yourself, call this to complete a link traversal.
        /// </summary>
        public void CompleteLinkTraversal()
        {
            if (IsOnLink)
            {
                currentPath.MoveNext();
                StartTraversingSegment();
            }
        }

        /// <summary>
        /// If you implement segment traversal yourself, call this to complete a segment traversal.
        /// </summary>
        public void CompleteSegmentTraversal()
        {
            if (movementState == MovementState.OnSegment)
            {
                if (currentPath.HasNext)
                {
                    if (CurrentPathSegment.link.IsTraversable)
                    {
                        StartTraversingLink();
                    }
                    else
                    {
                        movementState = MovementState.WaitForLinkOnSegment;
                    }
                }
                else
                {
                    status = State.Idle;
                    OnReachedGoal?.Invoke(this);
                }
            }
        }

        /// <summary>
        /// Determines, if in this agent is allowed to traverse the given link.
        /// </summary>
        public bool CanTraverseLink(INavLinkInstance link)
        {
            int linkType = link.LinkType;
            return linkType == -1 || (GetLinkTraversalMultiplier(linkType) > 0 && height <= link.Clearance);
        }

        /// <summary>
        /// Get the traversal cost multiplier for a given link type.
        /// </summary>
        public float GetLinkTraversalMultiplier(int linkType)
        {
            return linkTraversalCostMultipliers[linkType];
        }

        /// <summary>
        /// Get the traversal cost multiplier for a given nav tag.
        /// </summary>
        public float GetNavTagTraversalMultiplier(int navTag)
        {
            return navTagTraversalCostMultipliers[navTag] <= 0 ? float.PositiveInfinity : navTagTraversalCostMultipliers[navTag];
        }

        /// <summary>
        /// Whether the agents current position contains the given NavTag. NOTE: Does not work, if the agent is not currently moving on a path.
        /// </summary>
        /// <returns>True, if current position has supplied NavTag.</returns>
        public bool IsOnSegmentWithTag(int navTag)
        {
            if (IsOnSegment)
                return (CurrentNavTagVector & (1 << navTag)) != 0;
            else
                return false;
        }

        /// <summary>
        /// Stops the current path following at the first opportunity. Link traversal will be completed before the agent stops.
        /// </summary>
        public void Stop()
        {
            stopRequested = true;
            if (currentPathRequest.Status == PathRequest.RequestState.Pending)
                currentPathRequest = new PathRequest(this);
        }

        /// <summary>
        /// Stops the current path following instantly. Agent might stop wihle traversing a link (e.g. while jumping in mid air)
        /// </summary>
        public void ForceStop()
        {
            status = State.Idle;
            currentPathRequest = new PathRequest(this);
            OnStop?.Invoke(this);
        }

        /// <summary>
        /// Tries to map the "other" and checks if the agent is mapped to the same segment. 
        /// If "other" can't be mapped this will return null. 
        /// Agents on a link will always return false.
        /// If this agent currently can't be mapped this will return null.
        /// </summary>
        /// <param name="other"></param>
        public bool? IsOnSameSegmentAs(Vector2 other)
        {
            if (IsOnLink)
                return false;

            NavSegmentPositionPointer p;
            if (!PBWorld.TryMapPoint(other, out p) || currentMappedPosition.IsInvalid())
                return null;
            return currentMappedPosition.surface == p.surface && currentMappedPosition.cluster == p.cluster;
        }

        /// <summary>
        /// Enumerates the points on the currently followed path. Corner links will result in the same point being enumerated twice in a row. First point will be the agents current position.
        /// </summary>
        public IEnumerable<Vector2> PathPoints()
        {
            if (!IsFollowingAPath)
                yield break;

            yield return Position;

            var seg = currentPath.Current;
            if (IsOnSegment)
                yield return seg.LinkStart;

            if (seg.Next != null)
            {
                yield return seg.LinkEnd;

                seg = seg.Next;
                while (seg.Next != null)
                {
                    yield return seg.LinkStart;
                    yield return seg.LinkEnd;
                    seg = seg.Next;
                }

                yield return seg.LinkStart;
            }
        }



        /// <summary>
        /// Creates a pathrequest for this agent using the specified start and goal. The PathRequest is for your own use. The agent will take no further action. Use it to plan theoretical paths, without the agent moving. See also PBWorld.PathTo()
        /// </summary>
        /// <returns>A PathRequest or null, if start or goal couldn't be mapped.</returns>
        public PathRequest CreatePathRequest(Vector2 start, Vector2 goal)
        {
            float maxDist = Vector2.Distance(Position, goal) + 0.1f;
            NavSegmentPositionPointer startPointer;
            if (!PBWorld.TryMapPoint(start, maxDist, this, out startPointer))
                return null;

            NavSegmentPositionPointer goalPointer;
            if (!PBWorld.TryMapPoint(goal, maxDist, out goalPointer) || (!allowCloseEnoughPath && !CouldBeLocatedAt(goalPointer)))
                return null;

            PathRequest request = new PathRequest(this);
            request.start = startPointer;
            request.goals = new[] { goalPointer };

            return request;
        }

        /// <summary>
        /// Convenience function that will return if the agent could reach the given point from it's current location. It runs synchronously which is not optimal for performance.
        /// </summary>
        public bool CanReach(Vector2 goal)
        {
            var pr = CreatePathRequest(Position, goal);
            PBWorld.PathTo(pr);

            while (pr.Status != PathRequest.RequestState.Finished && pr.Status != PathRequest.RequestState.Failed)
            {
                // fast spinning
            }
            return pr.Status == PathRequest.RequestState.Finished;
        }

        private int GetNavTagMask()
        {
            int navTagMask = 0;
            for (int i = 0; i < navTagTraversalCostMultipliers.Length; i++)
            {
                if (navTagTraversalCostMultipliers[i] <= 0)
                    navTagMask |= 1 << i;
            }
            return ~navTagMask;
        }

        internal bool CanTraverseSegment(Vector2 segNormal, float minClearance)
        {
            return Vector2.Angle(Vector2.up, segNormal) <= maxSlopeAngle && minClearance >= height;
        }

        internal bool CouldBeLocatedAt(NavSegmentPositionPointer positionPointer)
        {
            return Vector2.Angle(Vector2.up, positionPointer.Normal) <= maxSlopeAngle && positionPointer.cluster.GetClearanceAlongSegment(positionPointer.t) >= height && (positionPointer.cluster.GetNavTagVector(positionPointer.t) & ~navTagMask) == 0;
        }

        private void StartTraversingLink()
        {
            movementState = MovementState.OnLink;
            traversedLinkSinceLastRepath = true;
            traversedLinkSinceLastPath = true;
            TimeOnLink = 0;
            OnStartLinkTraversal?.Invoke(this);
        }

        private void StartTraversingSegment()
        {
            movementState = MovementState.OnSegment;
            OnStartSegmentTraversal?.Invoke(this);
        }

        private void UpdateMappedPosition()
        {
            // probably not on ground when on link
            if (IsOnLink)
            {
                // make sure to set the path to invalid
                currentMappedPosition = NavSegmentPositionPointer.Invalid;
                return;
            }
            //if (Time.time >= timeToRemapPosition)
            //{
            // timeToRemapPosition = Time.time + 0.2f + UnityEngine.Random.value * 0.1f;

            PBWorld.TryMapAgent(Position, currentMappedPosition, this, out currentMappedPosition);

            // edge case fix
            // happens if the navagent is on a corner and mapping disagrees with path
            if (IsFollowingAPath && currentMappedPosition.cluster != currentPath.Current.cluster)
            {
                if (currentMappedPosition.t <= 0.05f)
                {
                    currentMappedPosition = new NavSegmentPositionPointer(currentMappedPosition.surface, currentPath.Current.cluster, 0);
                }
                else if (currentMappedPosition.t >= currentMappedPosition.cluster.Length - 0.05f)
                {
                    currentMappedPosition = new NavSegmentPositionPointer(currentMappedPosition.surface, currentPath.Current.cluster, currentPath.Current.cluster.Length);
                }
            }
        }

        private void Repath()
        {
            switch (repathPathRequest.Status)
            {
                case PathRequest.RequestState.Draft:
                    if (IsOnSegment && !currentMappedPosition.IsInvalid() && Time.time - lastRepathTime >= Mathf.Max(0.1f, autoRepathIntervall))
                    {
                        repathPathRequest.start = currentMappedPosition;
                        repathPathRequest.goals = currentPathRequest.goals;

                        PBWorld.PathTo(repathPathRequest);

                        lastRepathTime = Time.time;
                        traversedLinkSinceLastRepath = false;
                    }
                    break;
                case PathRequest.RequestState.Failed:
                    if (repathPathRequest.FailReason == PathRequest.RequestFailReason.NoPathFromStartToGoal
                        || repathPathRequest.FailReason == PathRequest.RequestFailReason.WorldWasDestroyed)
                    {
                        Stop();
                        OnFailedToFindPath?.Invoke(this);
                    }

                    repathPathRequest.Reset();
                    break;
                case PathRequest.RequestState.Finished:
                    if (!traversedLinkSinceLastRepath && currentPathRequest.goals == repathPathRequest.goals)
                    {
                        StartFollowingPath(repathPathRequest);
                    }

                    repathPathRequest.Reset();
                    break;
            }
        }

        private void HandlePathRequest()
        {
            switch (currentPathRequest.Status)
            {
                case PathRequest.RequestState.Failed:

                    if (!allowCloseEnoughPath)
                    {
                        Stop();

                        if (currentPathRequest.Status == PathRequest.RequestState.Failed && enableDebugMessages)
                            Debug.Log($"{name}: Pathrequest failed because: {currentPathRequest.FailReason}");

                        OnFailedToFindPath?.Invoke(this);
                        currentPathRequest.Reset();
                    }
                    else if (currentPathRequest.FailReason == PathRequest.RequestFailReason.NoPathFromStartToGoal && !currentPathRequest.closestReachablePosition.IsInvalid())
                    {
                        float maxDistance = 10;
                        float distance = Vector2.Distance(currentPathRequest.closestReachablePosition.Position, Position);
                        if (distance < maxDistance)
                        {
                            UpdatePath(new List<NavSegmentPositionPointer>() { currentPathRequest.closestReachablePosition });
                        }
                        OnFailedToFindPath?.Invoke(this);
                    }
                    break;
                case PathRequest.RequestState.Finished:
                    if (!traversedLinkSinceLastPath)
                    {
                        StartFollowingPath(currentPathRequest);
                        currentPathRequest.Reset();
                    }
                    else
                    {
                        traversedLinkSinceLastPath = false;
                        // retry
                        if (IsOnSegment && !currentMappedPosition.IsInvalid())
                            PathTo(currentPathRequest.goals);
                    }
                    break;
            }
        }

        private bool StartFollowingPath(PathRequest request)
        {
#if PBDEBUG
            Debug.Log("Pathrequest succeed. " + request.Path);
            Debug.Assert(request.Status == PathRequest.RequestState.Finished);
#endif
            // check that we are close to the path start
            if (Vector2.Distance(request.start.Position, Position) > maximumDistanceToPathStart)
            {
#if PBDEBUG
                Debug.Log("Moved to far away from path start. Not using that path");
#endif
                request.Fail(PathRequest.RequestFailReason.ToFarFromStart);
                return false;
            }

            stopRequested = false;
            lastRepathTime = Time.time;
            this.status = State.FollowPath;
            currentPath = request.Path;

            StartTraversingSegment();

            OnStartFollowingNewPath?.Invoke(this);
            return true;
        }
    }
}
