using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Used for demo.
    /// </summary>
    public class SelectTarget : MonoBehaviour
    {
        [SerializeField]
        NavAgent[] targets = null;
        
        [SerializeField]
        SmoothFollow cameraFollower = null;
        [SerializeField]
        Text uiDescription = null;
        [SerializeField]
        Text uiTarget = null;
        [SerializeField]
        Transform navTargetIndicator = null;
        [SerializeField]
        Vector3 overviewPos = Vector3.zero;
        [SerializeField]
        float overviewSize = 17;
        [SerializeField]
        float normalSize = 5;

        string[] targetDescriptions = new string[] {
            "Scout drone\n\nMoves fast, but is slow at climbing.\nCan't go through water or lava.\nIs able to walk on walls and ceilings.",
            "Heavy drone\n\nMoves rather slowly. Can withstand lava, but is not water proof.\nIs able to walk on walls and ceilings.",
            "Ant drone\n\nFast at moving and climbing. Doesn't mind lava or water.\nIs able to walk on walls and ceilings.",
            "Humanoid rectangle\n\nAverage at everything.\nCan't go through water or lava.\nCan't walk on ceilings or walls.",
            "Garbage monster\n\nVery slow, but indestructible in water or lava.\nHas no hands and can't climb.\nCan't walk on ceilings or walls.",
            "Zombie\n\nSlow and too stupid to use the teleporter or elevator.\nDoesn't mind water, but is not flame resistant.\nHas no hands and can't climb.\nCan't walk on ceilings or walls.",
            "Cultist\n\nWhile slow at moving, it performs jumps, climbs and falls almost instantly.\nIt's dark powers makes it invunerable to lava or water.\nHasn't yet mastered the art of manipulating gravity.",
            "Sentry\n\nFaster than it looks like.\nCan swim, but not in lava.\nHas no hands and can't climb.\nCan't walk on ceilings or walls."
        };

        int currentTarget = 0;

        private void Awake()
        {
            ChangeTarget();
        }

        // Update is called once per frame
        void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if(Keyboard.current.spaceKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.Space))
#endif
            {
                Camera.main.orthographicSize = overviewSize;
                transform.position = overviewPos;
                cameraFollower.target = transform;
            }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
#else
            else if (Input.GetKeyUp(KeyCode.Space))
#endif
            {
                Camera.main.orthographicSize = normalSize;
                ChangeTarget();
            }
            else
            {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                if (Mouse.current.leftButton.wasPressedThisFrame)
#else
                if (Input.GetMouseButtonDown(0))
#endif
                {
                    currentTarget++;
                    if (currentTarget >= targets.Length)
                        currentTarget = 0;
                    ChangeTarget();
                }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                if (Mouse.current.rightButton.wasPressedThisFrame)
#else
                else if (Input.GetMouseButtonDown(1))
#endif
                {
                    currentTarget--;
                    if (currentTarget < 0)
                        currentTarget = targets.Length - 1;
                    ChangeTarget();
                }
            }

            navTargetIndicator.position = targets[currentTarget].PathGoal ?? Vector2.zero;
        }

        void ChangeTarget()
        {
            float z = cameraFollower.transform.position.z;
            Vector3 v = targets[currentTarget].transform.position;
            v.z = z;
            cameraFollower.transform.position = v;
            cameraFollower.target = targets[currentTarget].transform;

            uiTarget.text = string.Format("Target {0}/{1}", currentTarget + 1, targets.Length);
            uiDescription.text = targetDescriptions[currentTarget];
        }
    }
}
