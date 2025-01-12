using AnyPath.Graphs.NavMesh;
using AnyPath.Managed.Results;
using UnityEngine;

namespace AnyPath.Examples
{
    // Displays a path with a linerenderer and fades out the color over time
    public class FadingPath : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Color lineColor;
        [SerializeField] private float fadeSpeed = .1f;
        [SerializeField] private float lineWidth = .1f;
        
        private void Awake()
        {
            lineRenderer.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
            lineRenderer.widthMultiplier = lineWidth;
        }

        public void UpdatePathLines(NavMeshGraphLocation startLocation, Path<CornerAndNormal> currentPath)
        {
            lineRenderer.positionCount = currentPath.Length + 1;
            lineRenderer.SetPosition(0, startLocation.ExitPosition + .5f * lineWidth * startLocation.Normal);
            lineRenderer.startColor = lineRenderer.endColor = lineColor;
            
            for (int i = 0; i < currentPath.Length; i++)
            {
                var seg = currentPath[i];
                
                lineRenderer.SetPosition(i + 1, seg.position + .5f * lineWidth * seg.normal);
            }
        }

        private void Update()
        {
            Color c = lineRenderer.endColor;
            c.a = Mathf.Max(0, c.a - Time.deltaTime * fadeSpeed);
            lineRenderer.startColor = lineRenderer.endColor = c;
        }
    }
}