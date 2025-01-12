using AnyPath.Graphs.Line;
using AnyPath.Managed.Results;
using Unity.Mathematics;
using UnityEngine;

namespace AnyPath.Examples.LineGraphExample
{
    public class LineGraphPathRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LineGraphExample example;
        [SerializeField] private float lineWidth = 1.6f;
        [SerializeField] private Vector3 offset = new Vector3(0, 1, 0);

        private void Start()
        {
            example.PathFound += SetPath;
            lineRenderer.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
            lineRenderer.widthMultiplier = lineWidth;
        }

        private void OnDestroy()
        {
            example.PathFound -= SetPath;
        }

        public void SetPath(Path<LineGraphLocation> path)
        {
            if (!path.HasPath)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            lineRenderer.positionCount = path.Length + 1;
            for (int i = 0; i < path.Length; i++)
                lineRenderer.SetPosition(i, path[i].EnterPosition + (float3)offset);
            
            lineRenderer.SetPosition(path.Length, path[path.Length - 1].ExitPosition + (float3)offset);
        }
    }
}