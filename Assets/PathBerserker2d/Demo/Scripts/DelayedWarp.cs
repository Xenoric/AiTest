using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Warps a NavAgent to the closest segment after a timer runs out.
    /// </summary>
    [RequireComponent(typeof(NavAgent))]
    public class DelayedWarp : MonoBehaviour
    {
        [SerializeField]
        float warpDelay = 1;

        NavAgent agent;

        private void Start()
        {
            agent = GetComponent<NavAgent>();
        }

        private void Update()
        {
            if (Time.time > warpDelay && agent.WarpToNearestSegment())
            {
                Destroy(this);
            }
        }
    }
}
