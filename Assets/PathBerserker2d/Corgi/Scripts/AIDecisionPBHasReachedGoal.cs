using MoreMountains.Tools;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Returns true, if NavAgent fires OnReachedGoal event.
    /// </summary>
    public class AIDecisionPBHasReachedGoal : AIDecision
    {
        private bool reachedGoal;

        public override void Initialization()
        {
            base.Initialization();
            var agent = this.GetComponentInParent<NavAgent>();
            agent.OnReachedGoal += Agent_OnReachedGoal;
        }

        private void Agent_OnReachedGoal(NavAgent obj)
        {
            reachedGoal = true;
        }

        public override void OnEnterState()
        {
            reachedGoal = false;
        }

        public override bool Decide()
        {
            if (reachedGoal)
            {
                reachedGoal = false;
                return true;
            }
            return false;
        }
    }
}