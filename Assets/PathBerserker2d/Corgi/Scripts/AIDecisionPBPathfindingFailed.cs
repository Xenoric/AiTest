using MoreMountains.Tools;

namespace PathBerserker2d.Corgi
{
    /// <summary>
    /// Returns true, if NavAgent fires OnFailedToFindPath event.
    /// </summary>
    public class AIDecisionPBPathfindingFailed : AIDecision
    {
        private bool failedToFindPath;

        public override void Initialization()
        {
            base.Initialization();
            var agent = this.GetComponentInParent<NavAgent>();
            agent.OnFailedToFindPath += Agent_OnFailedToFindPath;
        }

        private void Agent_OnFailedToFindPath(NavAgent obj)
        {
            failedToFindPath = true;
        }

        public override void OnEnterState()
        {
            failedToFindPath = false;
        }

        public override bool Decide()
        {
            if (failedToFindPath)
            {
                failedToFindPath = false;
                return true;
            }
            return false;
        }
    }
}