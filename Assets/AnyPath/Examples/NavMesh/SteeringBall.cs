using AnyPath.Graphs.NavMesh;
using AnyPath.Managed;
using AnyPath.Managed.Finders;
using AnyPath.Managed.Results;
using AnyPath.Native;
using UnityEngine;

namespace AnyPath.Examples
{
    // This finder uses the NavMeshGraphUnroller processor, which we can use for realtime steering
    public class SteeringNavMeshGraphPathFinder : PathFinder<NavMeshGraph, NavMeshGraphLocation, NavMeshGraphHeuristic, NoEdgeMod<NavMeshGraphLocation>, NavMeshGraphUnroller, UnrolledNavMeshGraphPortal> { }

    
    /// <summary>
    /// When the user right clicked, this ball traverses the last known path using realtime steering
    /// </summary>
    public class SteeringBall : MonoBehaviour
    {
        public float speed = .1f;
        public float turnSpeed = .25f;
        
        public NavMeshGraphExample example;
        private Path<UnrolledNavMeshGraphPortal> currentPath;
        private int edgeIndex;
        private Vector3 steerTarget;
        private Vector3 currentDirection;
        private SteeringNavMeshGraphPathFinder finder;

        private void Awake()
        {
            NavMeshGraphExample.Travel += NavMeshGraphExampleOnTravel;
        }

        private void Start()
        {
            finder = new SteeringNavMeshGraphPathFinder();
        }

        private void OnDestroy()
        {
            NavMeshGraphExample.Travel -= NavMeshGraphExampleOnTravel;
        }

        private void NavMeshGraphExampleOnTravel(NavMeshGraphLocation start, NavMeshGraphLocation goal)
        {
            // Build query
            finder.Clear();
            finder.SetGraph(example.graph);
            finder.SetStartAndGoal(start, goal);
            finder.Run();
            
            // Left3D is our exact starting location (Right3D too for that matter)
            SetPath(start.Left3D, finder.Result);
        }

        public void SetPath(Vector3 start, Path<UnrolledNavMeshGraphPortal> path)
        {
            if (path.Length == 0)
                return;
            
            currentPath = path;
            transform.position = start;
            edgeIndex = 0;
            currentDirection = Vector3.zero;
        }
        
        private void Update()
        {
            if (currentPath == null || edgeIndex >= currentPath.Length)
                return;
            
            Vector3 currentPos = transform.position;
            
            // Get the "best" position to move towards
            steerTarget = SSFA.GetSteerTargetPosition(currentPath, currentPos, ref edgeIndex);
            
            // Convert it into a direction
            Vector3 targetDir = (steerTarget - currentPos).normalized;
            
            // Smoothy adjust the direction of the ball
            currentDirection = Vector3.RotateTowards(currentDirection, targetDir, turnSpeed * Time.deltaTime, 1);

            // Move
            currentPos += currentDirection * speed * Time.deltaTime;

            transform.position = currentPos;
        }
        
        private void OnDrawGizmos()
        {
            if (currentPath == null || currentPath.Length == 0) return;
  
            if (!Application.isPlaying)
                return;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, steerTarget);

            Gizmos.color = Color.black;
            for (int i = 0; i < currentPath.Length; i++)
            {
                var seg = currentPath[i];
                Gizmos.DrawLine(seg.Left3D, seg.Right3D);
            }


            // draw unrolled path for debug purpose
            for (int i = 0; i < currentPath.Length; i++)
            {
                var seg = currentPath[i];
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(new Vector3(seg.Left2D.x, 0, seg.Left2D.y), new Vector3(seg.Right2D.x, 0, seg.Right2D.y));
                Gizmos.color = Color.cyan;
            }
        }
    }
}