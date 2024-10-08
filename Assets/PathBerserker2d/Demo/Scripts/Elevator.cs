using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// An Elevator that vertically moves to a series of y positions.
    /// </summary>
    /// <remarks>
    /// ## Requirements
    /// <list>
    ///     <item>
    ///     Levels need to sorted from highest Y level at index 0 to lowest Y level at last index.
    ///     </item>
    ///     <item>
    ///     Needs to be the child of a NavLinkCluster.
    ///     Manages this NavLinkCluster to only allow traversal on links that match the y level the elevator is at.
    ///     </item>
    /// </list>
    /// </remarks>
    public class Elevator : MonoBehaviour
    {
        [Tooltip("Different y-levels the elevator will stop at. NEEDS TO BE ORDERED FROM HIGHEST Y TO LOWEST Y.")]
        [SerializeField]
        Transform[] levels = null;

        [SerializeField]
        float speed = 1;

        [Tooltip("Time in seconds spend pausing on each level.")]
        [SerializeField]
        float waitTimeOnLevel = 3;

        [SerializeField]
        bool startMovingDown = true;

        [Tooltip("Corresponding link. NEEDS TO BE THIS SCRIPTS PARENT.")]
        [SerializeField]
        NavLinkCluster navLinkCluster = null;

        int state = 0;
        float waitStartTime;
        int nextLevel = 0;

        // Start is called before the first frame update
        void Awake()
        {
            if (levels.Length <= 1)
            {
                Debug.LogError("Elevator needs at least 2 levels to function.");
                this.enabled = false;
            }

            float prevY = float.MaxValue;
            foreach (var l in levels)
            {
                if (prevY < l.position.y)
                {
                    Debug.LogError("Elevator levels need to be ordered from top to bottom y-level.");
                    this.enabled = false;
                    return;
                }
                prevY = l.position.y;
            }
            if (navLinkCluster != null && GetComponentInParent<NavLinkCluster>() != navLinkCluster)
            {
                Debug.LogError("An elevator must be the child of the assigned cluster link.");
            }

            FindNextLevel();
            state = startMovingDown ? 1 : 0;
        }

        private void OnEnable()
        {
            LeavesLevel();
        }

        /// <summary>
        /// Find the level the elevator should visit next
        /// </summary>
        private void FindNextLevel()
        {
            float y = transform.position.y;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i].position.y < y)
                {
                    if (startMovingDown)
                    {
                        nextLevel = i;
                    }
                    else
                    {
                        if (i == 0)
                        {
                            startMovingDown = true;
                            nextLevel = i;
                        }
                        else
                        {
                            nextLevel = i - 1;
                        }
                    }
                    return;
                }
            }
            startMovingDown = false;
            nextLevel = levels.Length - 1;
        }

        void Update()
        {
            switch (state)
            {
                case 0:
                    // move up to destination
                    transform.position += Vector3.up * Time.deltaTime * speed;
                    if (transform.position.y >= levels[nextLevel].position.y)
                    {
                        var v = transform.position;
                        v.y = levels[nextLevel].position.y;
                        transform.position = v;

                        state = 2;
                        waitStartTime = Time.time;
                        ReachedLevel();
                    }
                    break;
                case 1:
                    // move down to destination
                    transform.position += Vector3.down * Time.deltaTime * speed;
                    if (transform.position.y <= levels[nextLevel].position.y)
                    {
                        var v = transform.position;
                        v.y = levels[nextLevel].position.y;
                        transform.position = v;

                        state = 3;
                        waitStartTime = Time.time;
                        ReachedLevel();
                    }
                    break;
                case 2:
                    // wait at destination up
                    if (Time.time - waitStartTime > waitTimeOnLevel)
                    {
                        nextLevel--;
                        if (nextLevel < 0)
                        {
                            nextLevel = levels.Length - 2;
                            state = 1;
                        }
                        else
                        {
                            state = 0;
                        }
                        LeavesLevel();
                    }
                    break;
                case 3:
                    // wait at destination down
                    if (Time.time - waitStartTime > waitTimeOnLevel)
                    {
                        nextLevel++;
                        if (nextLevel >= levels.Length)
                        {
                            nextLevel = 1;
                            state = 0;
                        }
                        else
                        {
                            state = 1;
                        }
                        LeavesLevel();
                    }
                    break;
            }
        }

        void ReachedLevel()
        {
            if (navLinkCluster != null)
            {
                // set the links at our y level traversable
                navLinkCluster.SetLinksTraversable((start, goal) =>
                {
                    return Mathf.Abs(start.y - levels[nextLevel].position.y) < 0.1f ||
                    Mathf.Abs(goal.y - levels[nextLevel].position.y) < 0.1f;
                });
            }
        }

        void LeavesLevel()
        {
            if (navLinkCluster != null)
            {
                // set all links to be untraversable as we aren't at any level
                navLinkCluster.SetLinksTraversable((start, goal) => false);
            }
        }
    }
}