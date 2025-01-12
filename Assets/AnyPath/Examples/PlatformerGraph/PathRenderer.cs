using AnyPath.Graphs.PlatformerGraph;
using AnyPath.Managed.Results;
using AnyPath.Native.Util;
using UnityEngine;

namespace AnyPath.Examples.Platformer
{
    public class PathRenderer : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private PlatformerGraphExample example;
        [SerializeField] private float lineWidth = 1.6f;

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

        public void SetPath( Path<PlatformerGraphLocation> path)
        {
            if (!path.HasPath)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            lineRenderer.positionCount = path.Length + 1;
            for (int i = 0; i < path.Length; i++)
                lineRenderer.SetPosition(i, path[i].EnterPosition.ToVec3(-1));
            
            lineRenderer.SetPosition(path.Length, path[path.Length - 1].ExitPosition.ToVec3(-1));
        }
    }
}