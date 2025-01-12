using System.Collections;
using UnityEngine;

namespace AnyPath.Examples
{
    // Used for cool visualization purposes
    public class CameraRotatorDistanceChanger : MonoBehaviour
    {
        public CameraRotator rotator;
        
        public float startDistance = 0;
        public float duration = 15f;
        
        private float endDistance;
        
        private void Start()
        {
            endDistance = rotator.distance;
            StartCoroutine(Anim());
        }

        IEnumerator Anim()
        {
            yield return null;

            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                rotator.distance = Mathf.Lerp(startDistance, endDistance, t / duration);
                yield return null;
            }
        }
    }
}