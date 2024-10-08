using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Moves a NavAgent by manipulating its transform.
    /// </summary>
    public class TransformBasedMovement : MonoBehaviour
    {
        [System.Flags]
        public enum FeatureFlags
        {
            SegmentMovement = 1,
            JumpLinks = 2,
            CornerLinks = 4,
            FallLinks = 8,
            TeleportLinks = 16,
            ClimbLinks = 32,
            ElevatorLinks = 64,
            OtherLinks = 128,
        }

        [Tooltip("Speed on segments in unit/s.")]
        [SerializeField]
        public float movementSpeed = 5;

        [Tooltip("Speed on corner links in degrees/s.")]
        [SerializeField]
        public float cornerSpeed = 100;

        [Tooltip("Speed on jump links in unit/s.")]
        [SerializeField]
        public float jumpSpeed = 5;

        [Tooltip("Speed on fall links in unit/s.")]
        [SerializeField]
        public float fallSpeed = 5;

        [Tooltip("Speed on climb links in unit/s.")]
        [SerializeField]
        public float climbSpeed = 5;

        /// <summary>
        /// If false, agent will not be rotated.
        /// </summary>
        [Tooltip("Controls whether the default movement handler is allowed to rotate the agent.")]
        [SerializeField]
        public bool enableAgentRotation = true;

        /// <summary>
        /// Sets which links and segments this component will handle. Useful to override an Agents default behavior for a certain link type or segment.
        /// </summary>
        [Tooltip("Enable features by setting the flag.")]
        [SerializeField]
        public FeatureFlags enabledFeatures = (FeatureFlags)int.MaxValue;

        private float timeOnLink;
        private float timeToCompleteLink;
        private Vector2 direction;
        private int state = 0;
        private Transform elevatorTrans;
        private float deltaDistance;
        private bool handleLinkMovement;
        private int minNumberOfLinkExecutions;
        private Vector2 storedLinkStart;

        private void OnEnable()
        {
            var agent = GetComponent<NavAgent>();
            agent.OnStartLinkTraversal += Agent_StartLinkTraversalEvent;
            agent.OnStartSegmentTraversal += Agent_OnStartSegmentTraversal;
            agent.OnSegmentTraversal += Agent_OnSegmentTraversal;
            agent.OnLinkTraversal += Agent_OnLinkTraversal;
        }

        private void OnDisable()
        {
            var agent = GetComponent<NavAgent>();
            agent.OnStartLinkTraversal -= Agent_StartLinkTraversalEvent;
            agent.OnStartSegmentTraversal -= Agent_OnStartSegmentTraversal;
            agent.OnSegmentTraversal -= Agent_OnSegmentTraversal;
            agent.OnLinkTraversal -= Agent_OnLinkTraversal;
        }

        private void OnValidate()
        {
            if (jumpSpeed <= 0)
                jumpSpeed = 0.01f;
            if (fallSpeed <= 0)
                fallSpeed = 0.01f;
            if (climbSpeed <= 0)
                climbSpeed = 0.01f;
        }

        private void Agent_OnStartSegmentTraversal(NavAgent agent)
        {

        }

        private void Agent_OnSegmentTraversal(NavAgent agent)
        {
            if (!enabledFeatures.HasFlag(FeatureFlags.SegmentMovement))
                return;

            Vector2 newPos;
            bool reachedGoal = MoveAlongSegment(agent.Position, agent.PathSubGoal, agent.CurrentPathSegment.Point, agent.CurrentPathSegment.Tangent, Time.deltaTime * movementSpeed, out newPos);
            agent.Position = newPos;
            
            if (reachedGoal)
            {
                agent.CompleteSegmentTraversal();
            }
        }

        private void Agent_StartLinkTraversalEvent(NavAgent agent)
        {
            string linkType = agent.CurrentPathSegment.link.LinkTypeName;

            bool unknownLinkType = linkType != "corner" && linkType != "fall" && linkType != "jump" && linkType != "elevator" && linkType != "teleport" && linkType != "climb";

            handleLinkMovement =
                (unknownLinkType && enabledFeatures.HasFlag(FeatureFlags.OtherLinks)) || 
                (linkType == "corner" && enabledFeatures.HasFlag(FeatureFlags.CornerLinks)) ||
                (linkType == "fall" && enabledFeatures.HasFlag(FeatureFlags.FallLinks)) ||
                (linkType == "jump" && enabledFeatures.HasFlag(FeatureFlags.JumpLinks)) ||
                (linkType == "elevator" && enabledFeatures.HasFlag(FeatureFlags.ElevatorLinks)) ||
                (linkType == "teleport" && enabledFeatures.HasFlag(FeatureFlags.TeleportLinks)) ||
                (linkType == "climb" && enabledFeatures.HasFlag(FeatureFlags.ClimbLinks));

            if (!handleLinkMovement)
                return;

            timeOnLink = 0;
            Vector2 delta = agent.PathSubGoal - agent.CurrentPathSegment.LinkStart;
            deltaDistance = delta.magnitude;
            direction = delta / deltaDistance;
            minNumberOfLinkExecutions = 1;
            storedLinkStart = agent.CurrentPathSegment.LinkStart;

            float speed = 1;
            switch (agent.CurrentPathSegment.link.LinkTypeName)
            {
                case "corner":
                    if (!enableAgentRotation)
                    {
                        agent.CompleteLinkTraversal();
                        break;
                    }
                    speed = cornerSpeed;
                    deltaDistance = agent.CurrentPathSegment.link.TravelCosts(Vector2.zero, Vector2.zero);
                    break;
                case "fall":
                    speed = fallSpeed;
                    break;
                case "climb":
                    speed = climbSpeed;

                    Vector2 pos = agent.CurrentPathSegment.link.GameObject.transform.position;
                    Vector2 dir = agent.CurrentPathSegment.link.GameObject.transform.up;

                    Vector2 start = Geometry.ProjectPointOnLine(agent.CurrentPathSegment.LinkStart, pos, dir);
                    Vector2 end = Geometry.ProjectPointOnLine(agent.PathSubGoal, pos, dir);
                    deltaDistance = Vector2.Distance(start, pos) + Vector2.Distance(start, end) + Vector2.Distance(end, agent.PathSubGoal);

                    state = 0;
                    minNumberOfLinkExecutions = 3;
                    break;
                case "jump":
                    speed = jumpSpeed;
                    break;
                case "elevator":
                    speed = movementSpeed;
                    state = 0;
                    minNumberOfLinkExecutions = 4;
                    elevatorTrans = agent.CurrentPathSegment.link.GameObject.transform;
                    var childTrans = agent.CurrentPathSegment.link.GameObject.GetComponentsInChildren<Transform>();
                    foreach (var t in childTrans)
                    {
                        if (t.gameObject.layer == 8)
                        {
                            elevatorTrans = t;
                            break;
                        }
                    }
                    break;
            }

            if (agent.CurrentPathSegment.link.LinkTypeName == "elevator")
                timeToCompleteLink = float.PositiveInfinity;
            else
                timeToCompleteLink = (deltaDistance / speed);
        }

        private void Agent_OnLinkTraversal(NavAgent agent)
        {
            if (!handleLinkMovement)
                return;

            timeOnLink += Time.deltaTime;
            timeOnLink = Mathf.Min(timeToCompleteLink, timeOnLink);

            switch (agent.CurrentPathSegment.link.LinkTypeName)
            {
                case "corner":
                    Corner(agent);
                    break;
                case "jump":
                    Jump(agent);
                    break;
                case "fall":
                    Fall(agent);
                    break;
                case "teleport":
                    Teleport(agent);
                    timeOnLink = timeToCompleteLink + 1;
                    break;
                case "climb":
                    Climb(agent);
                    break;
                case "elevator":
                    Elevator(agent);
                    break;
                default:
                    Jump(agent);
                    break;
            }

            minNumberOfLinkExecutions--;
            if (timeOnLink >= timeToCompleteLink && minNumberOfLinkExecutions <= 0)
            {
                agent.CompleteLinkTraversal();
                return;
            }
        }

        private void Corner(NavAgent agent)
        {
            var from = Quaternion.LookRotation(Vector3.forward, agent.CurrentPathSegment.Normal);
            var to = Quaternion.LookRotation(Vector3.forward, agent.CurrentPathSegment.Next.Normal);


            agent.transform.rotation = Quaternion.Slerp(
                from,
                to,
                agent.TimeOnLink / (deltaDistance / cornerSpeed));
        }

        private void Jump(NavAgent agent)
        {
            Vector2 newPos = storedLinkStart + direction * timeOnLink * jumpSpeed;
            newPos.y += deltaDistance * 0.3f * Mathf.Sin(Mathf.PI * timeOnLink / timeToCompleteLink);
            agent.Position = newPos;
        }

        private void Fall(NavAgent agent)
        {
            Vector2 newPos = storedLinkStart + direction * timeOnLink * fallSpeed;
            agent.Position = newPos;
        }

        private void Climb(NavAgent agent)
        {
            Vector2 linkPos = agent.CurrentPathSegment.link.GameObject.transform.position;
            Vector2 linkDir = agent.CurrentPathSegment.link.GameObject.transform.up;

            Vector2 newPos = Vector2.zero;
            switch (state)
            {
                case 0:
                    Vector2 start = Geometry.ProjectPointOnLine(agent.CurrentPathSegment.LinkStart, linkPos, linkDir);
                    if (MoveTo(agent.Position, start, climbSpeed * Time.deltaTime, out newPos))
                    {
                        state = 1;
                    }
                    break;
                case 1:
                    Vector2 end = Geometry.ProjectPointOnLine(agent.PathSubGoal, linkPos, linkDir);
                    if (MoveTo(agent.Position, end, climbSpeed * Time.deltaTime, out newPos))
                    {
                        state = 2;
                    }
                    break;
                case 2:
                    if (MoveTo(agent.Position, agent.PathSubGoal, climbSpeed * Time.deltaTime, out newPos))
                    {
                        // force early exit
                        timeToCompleteLink = 0;
                    }
                    break;
            }
            agent.Position = newPos;
        }

        private void Elevator(NavAgent agent)
        {
            // 3 phase
            // 1. move on elevator
            // 2. wait to reach destination
            // 3. leave

            Vector2 newPos = agent.Position;
            switch (state)
            {
                case 0:
                    Vector2 target = elevatorTrans.position;
                    if (agent.CurrentPathSegment.link.IsTraversable && Mathf.Abs(newPos.y - target.y) < 0.1f)
                    {
                        state = 1;
                        newPos.y = target.y;
                        direction = Vector2.right * Mathf.Sign(target.x - storedLinkStart.x);
                    }
                    break;
                case 1:
                    newPos += movementSpeed * direction * Time.deltaTime;

                    float targetX = agent.CurrentPathSegment.link.GameObject.transform.position.x;
                    if ((newPos.x - targetX) * direction.x >= 0)
                    {
                        state = 2;
                        newPos.x = targetX;
                    }
                    break;
                case 2:
                    // wait till y matches elevation
                    // cast ray downwards to move with platform
                    float targetY = agent.PathSubGoal.y;
                    if (agent.CurrentPathSegment.link.IsTraversable && Mathf.Abs(newPos.y - targetY) < 0.1f)
                    {
                        state = 3;
                        newPos.y = targetY;
                        direction = Vector2.right * Mathf.Sign(agent.PathSubGoal.x - newPos.x);
                        timeOnLink = 0;
                        timeToCompleteLink = Mathf.Abs(agent.PathSubGoal.x - newPos.x) / movementSpeed;
                    }
                    break;
                case 3:
                    newPos += movementSpeed * direction * Time.deltaTime;
                    break;
            }
            agent.Position = newPos;
        }

        private void Teleport(NavAgent agent)
        {
            agent.Position = agent.PathSubGoal;
        }

        private static bool MoveAlongSegment(Vector2 pos, Vector2 goal, Vector2 segPoint, Vector2 segTangent, float amount, out Vector2 newPos)
        {
            pos = Geometry.ProjectPointOnLine(pos, segPoint, segTangent);
            goal = Geometry.ProjectPointOnLine(goal, segPoint, segTangent);
            return MoveTo(pos, goal, amount, out newPos);
        }

        private static bool MoveTo(Vector2 pos, Vector2 goal, float amount, out Vector2 newPos)
        {
            Vector2 dir = goal - pos;
            float distance = dir.magnitude;
            if (distance <= amount)
            {
                newPos = goal;
                return true;
            }

            newPos = pos + dir * amount / distance;
            return false;
        }
    }
}
