using MoreMountains.CorgiEngine;
using System.Reflection;
using UnityEngine;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Moves a NavAgent by using an attached CorgiController and its abilities. Requires the following abilities: 
    /// CharacterHorizontalMovement, CharacterJump, CharacterLadder
    /// </summary>
    public class CorgiBasedMovement : MonoBehaviour
    {
        // reflection to access ladder methods
        static MethodInfo ladderStartClimbingMethod = typeof(CharacterLadder).GetMethod("StartClimbing", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo ladderStartClimbingDownMethod = typeof(CharacterLadder).GetMethod("StartClimbingDown", BindingFlags.NonPublic | BindingFlags.Instance);
        static MethodInfo ladderClimbingMethod = typeof(CharacterLadder).GetMethod("Climbing", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo ladderVerticalInput = typeof(CharacterLadder).GetField("_verticalInput", BindingFlags.NonPublic | BindingFlags.Instance);

        /// <summary>
        /// Teleports the stuck navagent after this amount of time has passed to a better location. A negative number disables this.
        /// </summary>
        [Tooltip("Teleports the stuck navagent after this amount of time has passed to a better location. A negative number disables this.")]
        [SerializeField]
        float fallbackTeleportDelay = 4;

        NavAgent navAgent;
        Character character;
        CorgiController controller;

        CharacterHorizontalMovement horizontalMovementAbility;
        CharacterJump jumpAbility;
        CharacterLadder climbAbility;

        Vector2 lastFrameAgentPos;
        Vector2 initialMoveDir;
        float timeSinceStuck;

        private void OnEnable()
        {
            navAgent = GetComponentInParent<NavAgent>();
            character = GetComponentInParent<Character>();
            controller = GetComponentInParent<CorgiController>();

            horizontalMovementAbility = character.FindAbility<CharacterHorizontalMovement>();
            jumpAbility = character.FindAbility<CharacterJump>();
            climbAbility = character.FindAbility<CharacterLadder>();

            navAgent.OnStartSegmentTraversal += NavAgent_OnStartSegmentTraversal;
            navAgent.OnSegmentTraversal += NavAgent_OnSegmentTraversal;
            navAgent.OnLinkTraversal += NavAgent_OnLinkTraversal;
            navAgent.OnStartLinkTraversal += NavAgent_OnStartLinkTraversal;
            navAgent.OnStop += NavAgent_OnStop;
            navAgent.OnReachedGoal += NavAgent_OnReachedGoal;
        }

        private void OnDisable()
        {
            navAgent.OnStartSegmentTraversal -= NavAgent_OnStartSegmentTraversal;
            navAgent.OnSegmentTraversal -= NavAgent_OnSegmentTraversal;
            navAgent.OnLinkTraversal -= NavAgent_OnLinkTraversal;
            navAgent.OnStartLinkTraversal -= NavAgent_OnStartLinkTraversal;
            navAgent.OnStop -= NavAgent_OnStop;
        }

        private void Update()
        {
            // safe guard. Teleports agent to closest walkable surface, if it can't be mapped for a certain time
            if (!navAgent.IsOnLink && !navAgent.HasValidPosition && fallbackTeleportDelay >= 0)
            {
                timeSinceStuck += Time.deltaTime;
                if (timeSinceStuck > fallbackTeleportDelay)
                {
                    navAgent.WarpToNearestSegment();
                    timeSinceStuck = 0;
                }
            } else
            {
                timeSinceStuck = 0;
            }
        }

        private void NavAgent_OnReachedGoal(NavAgent obj)
        {
            horizontalMovementAbility.SetHorizontalMove(0);
            controller.SetHorizontalForce(0);
        }

        private void NavAgent_OnStop(NavAgent obj)
        {
            horizontalMovementAbility.SetHorizontalMove(0);
            controller.SetHorizontalForce(0);
        }

        private void NavAgent_OnStartSegmentTraversal(NavAgent agent)
        {
            lastFrameAgentPos = navAgent.Position;
        }

        private void NavAgent_OnSegmentTraversal(NavAgent agent)
        {
            if (character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Frozen)
                return;

            Vector2 closestToGoal = Geometry.ProjectPointOnLineSegment(agent.PathSubGoal, lastFrameAgentPos, agent.Position);

            if (Mathf.Abs(closestToGoal.x - agent.PathSubGoal.x) < 0.05f)
            {
                horizontalMovementAbility.SetHorizontalMove(0);
                agent.CompleteSegmentTraversal();
                return;
            }

            lastFrameAgentPos = agent.Position;
            Vector2 delta = agent.PathSubGoal - agent.Position;
            horizontalMovementAbility.SetHorizontalMove(Mathf.Sign(delta.x));
        }

        private void NavAgent_OnStartLinkTraversal(NavAgent agent)
        {
            Vector2 delta = agent.PathSubGoal - agent.Position;
            initialMoveDir = delta;
            switch (agent.CurrentPathSegment.link.LinkTypeName)
            {
                case "jump":
                    if (delta.y > -2)
                        jumpAbility.JumpStart();
                    break;
                case "corner":
                    agent.CompleteLinkTraversal();
                    break;
                case "climb":
                    try
                    {
                        if (delta.y > 0)
                            ladderStartClimbingMethod.Invoke(climbAbility, new object[] { });
                        else
                            ladderStartClimbingDownMethod.Invoke(climbAbility, new object[] { });
                    }
                    catch (System.Exception)
                    {
                        // could be anything, fallback to doing nothing
                        agent.ForceStop();
                    }
                    break;
            }
        }

        private void NavAgent_OnLinkTraversal(NavAgent agent)
        {
            if (character.ConditionState.CurrentState == CharacterStates.CharacterConditions.Frozen)
                return;

            Vector2 delta = agent.PathSubGoal - agent.Position;

            switch (agent.CurrentPathSegment.link.LinkTypeName)
            {
                case "fall":
                case "jump":
                    Jump(delta);
                    break;
                case "climb":
                    Climb(delta);
                    break;
                case "teleport":
                    navAgent.Position = agent.PathSubGoal;
                    break;
            }
        }

        private void Jump(Vector2 delta)
        {
            float horizontalMove = (Mathf.Sign(delta.x) + Mathf.Sign(initialMoveDir.x)) / 2.0f;
            horizontalMovementAbility.SetHorizontalMove(horizontalMove);

            // are we grounded but not at goal?
            if (controller.State.JustGotGrounded)
            {
                navAgent.CompleteLinkTraversal();
            }
            else if (controller.Speed.y <= 0 && delta.y > 0)
            {

                if (Mathf.Abs(horizontalMove) < 0.5f)
                {
                    navAgent.CompleteLinkTraversal();
                }
                else
                {
                    // are we below target height and are falling?
                    jumpAbility.JumpStart();
                }
            }
        }

        private void Climb(Vector2 delta)
        {
            if (Mathf.Sign(delta.y) != Mathf.Sign(initialMoveDir.y) || controller.State.JustGotGrounded)
            {
                // reached target height
                climbAbility.GetOffTheLadder();
                navAgent.CompleteLinkTraversal();
                return;
            }

            if (delta.y > 0)
            {
                ladderVerticalInput.SetValue(climbAbility, 1);
            }
            else
            {
                ladderVerticalInput.SetValue(climbAbility, -1);
            }
            try
            {
                ladderClimbingMethod.Invoke(climbAbility, new object[] { });
            }
            catch (System.Exception)
            {
                navAgent.ForceStop();
                climbAbility.GetOffTheLadder();
            }
        }
    }
}