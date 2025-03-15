using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Opens/closes a door when the player presses "e"
    /// </summary>
    public class OpenDoor : MonoBehaviour
    {
        [SerializeField]
        NavAreaMarker navAreaMarker = null;

        bool doorOpen = false;
        Coroutine doorMovement;
        float openYLevel;
        float closedYLevel;

        private void Awake()
        {
            openYLevel = transform.localPosition.y + 1;
            closedYLevel = transform.localPosition.y;
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            if (Keyboard.current.eKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.E))
#endif
            {
                if (doorOpen)
                {
                    if (doorMovement != null)
                        StopCoroutine(doorMovement);

                    doorMovement = StartCoroutine(CloseDoorRoutine());
                }
                else
                {
                    if (doorMovement != null)
                        StopCoroutine(doorMovement);

                    doorMovement = StartCoroutine(OpenDoorRoutine());
                }
                doorOpen = !doorOpen;
            }
        }

        IEnumerator OpenDoorRoutine()
        {
            while (transform.localPosition.y < openYLevel)
            {
                var pos = transform.localPosition;
                pos.y += Time.deltaTime;
                transform.localPosition = pos;

                yield return null;
            }
            navAreaMarker.enabled = false;
        }

        IEnumerator CloseDoorRoutine()
        {
            navAreaMarker.enabled = true;
            while (transform.localPosition.y > closedYLevel)
            {
                var pos = transform.localPosition;
                pos.y -= Time.deltaTime;
                transform.localPosition = pos;

                yield return null;
            }
        }
    }
}