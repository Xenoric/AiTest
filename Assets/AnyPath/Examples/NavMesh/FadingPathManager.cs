using AnyPath.Graphs.NavMesh;
using AnyPath.Managed.Results;
using UnityEngine;

namespace AnyPath.Examples
{
    // Manages the different FadingPath's so that they're visible longer than one frame
    public class FadingPathManager : MonoBehaviour
    {
        public NavMeshGraphExample example;
        public FadingPath[] fadingPaths;

        private int currentIndex = 0;

        private void Start()
        {
            NavMeshGraphExample.RandomPath += NavMeshGraphExampleOnRandomPath;
        }
        
        private void OnDestroy()
        {
            NavMeshGraphExample.RandomPath -= NavMeshGraphExampleOnRandomPath;
        }

        private void NavMeshGraphExampleOnRandomPath(NavMeshGraphLocation start, Path<CornerAndNormal> path)
        {
            if (!path.HasPath)
                return;
            
            fadingPaths[currentIndex].UpdatePathLines(start, path);
            currentIndex = (currentIndex + 1) % fadingPaths.Length;
        }
    }
}