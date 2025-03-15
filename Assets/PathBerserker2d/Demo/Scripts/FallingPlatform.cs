using UnityEngine;
using System;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Moves a platform downwards. The attached GameObject will be destroyed, after this platforms falls below a certain y-level.
    /// Also shows, how to instantiate jump links. On start this platform will try to connect each of its jumpAnchorPoints with every other FallingPlatform in range.
    /// </summary>
    public class FallingPlatform : MonoBehaviour
    {
        [NonSerialized]
        public float fallSpeed;
        [NonSerialized]
        public float deleteBelowYLevel;
        [NonSerialized]
        public float maxJumpDistance;

        [SerializeField]
        public Vector2[] jumpAnchorPoints;
       

        private void Start()
        {
            // connect jump links to other platforms
            var otherFps = FindObjectsOfType<FallingPlatform>();
            Vector2 ownPos = transform.position;
            foreach (var fp in otherFps)
            {
                if (fp == this || Vector2.Distance(fp.transform.position, ownPos) > maxJumpDistance)
                    continue;

                // find closest connection
                Vector2 otherPos = fp.transform.position;
                float smallestDist = float.MaxValue;
                int smallestDistIndexOwn = -1;
                int smallestDistIndexOther = -1;
                for (int i = 0; i < jumpAnchorPoints.Length; i++)
                {
                    for (int k = 0; k < fp.jumpAnchorPoints.Length; k++)
                    {
                        float dist = (ownPos + jumpAnchorPoints[i] - (otherPos + fp.jumpAnchorPoints[k])).sqrMagnitude;

                        if (dist < smallestDist)
                        {
                            smallestDist = dist;
                            smallestDistIndexOwn = i;
                            smallestDistIndexOther = k;
                        }
                    }
                }

                if (smallestDistIndexOwn != -1)
                {
                    // create jump link
                    Vector2 from = jumpAnchorPoints[smallestDistIndexOwn];
                    Vector2 to = otherPos + fp.jumpAnchorPoints[smallestDistIndexOther];

                    var link = gameObject.AddComponent<NavLink>();
                    link.StartLocalPosition = from;
                    link.GoalWorldPosition = to;
                    link.IsBidirectional = true;
                    link.UpdateMapping();
                }
            }
        }

        void Update()
        {
            // delete if outside of screen
            if (transform.localPosition.y < deleteBelowYLevel)
            {
                Destroy(gameObject);
                return;
            }

            // move downwards (aka fall)
            var v = transform.localPosition;
            v.y -= Time.deltaTime * fallSpeed;
            transform.localPosition = v;
        }

        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < jumpAnchorPoints.Length; i++)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)jumpAnchorPoints[i], 0.1f);
            }
        }
    }
}
