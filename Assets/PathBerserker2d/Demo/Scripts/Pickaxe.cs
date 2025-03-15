using UnityEngine;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Enables a NavAgent to break a Breakable
    /// </summary>
    public class Pickaxe : MonoBehaviour
    {
        [SerializeField]
        NavAgent agent = null;

        private void Start()
        {
            agent.OnLinkTraversal += Agent_OnLinkTraversal;
        }

        private void Agent_OnLinkTraversal(NavAgent agent)
        {
            // overriding link type elevator to avoid adding more build-in link types
            if (agent.CurrentPathSegment.link.LinkTypeName == "elevator")
            {
                if (agent.CurrentPathSegment.link.GameObject.GetComponent<Breakable>().Break())
                {
                    agent.CompleteLinkTraversal();
                }
            }
        }
    }
}
