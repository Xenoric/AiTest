using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Greets an agent if they are on the same segment.
    /// </summary>
    public class Greetings : MonoBehaviour
    {
        [SerializeField]
        NavAgent agent = null;
        [SerializeField]
        GameObject greetings = null;

        // Update is called once per frame
        void Update()
        {
            // could be handled without raycast
            // is more robust though, because to map a point it needs to be close to the collider
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1);
            if (hit.collider != null)
            {
                bool? onSameSegment = agent.IsOnSameSegmentAs(hit.point);
                if (onSameSegment.HasValue)
                {
                    greetings.SetActive(onSameSegment.Value);
                }
            }
        }
    }
}
