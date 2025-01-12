using System.Collections;
using UnityEngine;

namespace AnyPath.Examples
{
    public class ScreenCaptureZoom : MonoBehaviour
    {
        public Camera mainCamera;
        
        public float startOrtho = 5;
        public float duration = 15f;
        
        private float endOrtho;
        
        private void Start()
        {
            endOrtho = mainCamera.orthographicSize;
            StartCoroutine(Anim());
        }

        IEnumerator Anim()
        {
            yield return null;

            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                mainCamera.orthographicSize = Mathf.Lerp(startOrtho, endOrtho, t / duration);
                yield return null;
            }
        }
    }
}