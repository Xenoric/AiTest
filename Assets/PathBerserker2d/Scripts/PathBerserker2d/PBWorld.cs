using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PathBerserker2d
{
    /// <summary>
    /// Singleton managing the global NavGraph instance. 
    /// </summary>
    /// <remarks>
    /// Is instantiated automatically on scene load.
    ///
    /// Use it to access the NavGraph, which is the graph the pathfinder works on.
    /// </remarks>
    [ScriptExecutionOrder(-100), AddComponentMenu("")]
    public class PBWorld : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            PathBerserker2dSettings.instance = Resources.Load<PathBerserker2dSettings>(PathBerserker2dSettings.GlobalSettingsFile);
            var world = new GameObject("PBWorld").AddComponent<PBWorld>();
            DontDestroyOnLoad(world);
            instance = world;
        }

        private static PBWorld instance;

        internal static NavGraph NavGraph { get; set; }
        public static INavGraphChangeSource NavGraphChangeSource => NavGraph;

        // threading stuff
        CancellationTokenSource pathfinderThreadCancelationSource;
        ConcurrentQueue<PathRequest> pathRequestQueue;

        private float lastGraphUpdate = -100;

        #region UNITY_METHODS
        private void Awake()
        {
            SceneManager.sceneUnloaded += SceneManager_sceneUnloaded;
        }

        private void SceneManager_sceneUnloaded(Scene arg0)
        {
            // cycle the navgraph one more time to remove all unloaded navgraphs in case the scene is just reloaded
            NavGraph.ForceApplyChanges();
        }

        private void OnEnable()
        {
            NavGraph = new NavGraph(PathBerserker2dSettings.PathfinderThreadCount);

            // create pathfinder threads
            pathRequestQueue = new ConcurrentQueue<PathRequest>();
            pathfinderThreadCancelationSource = new CancellationTokenSource();
            StartPathfinderThreads();
        }

        private void Start()
        {
            NavGraph.Update();
        }

        private void OnDisable()
        {
            pathfinderThreadCancelationSource.Cancel();
        }

        private void Update()
        {
            if (Time.time - lastGraphUpdate > PathBerserker2dSettings.InitiateUpdateInterval)
            {
                NavGraph.Update();
                lastGraphUpdate = Time.time;
            }
        }

        private void Reset()
        {
            Debug.LogError("This component should not be added manually. It will be automatically instantiated at runtime.");
            DestroyImmediate(this);
        }
        #endregion

        /// <summary>
        /// Tries to map a point to a navigation position. The distance a point can be away from the nearest segment is specified by <see cref="PathBerserker2dSettings"/>.<c>PointMappingDistance</c>
        /// </summary>
        /// <param name="position">Position to map</param>
        /// <param name="pointer">Pointer to mapped position or an invalid pointer, if mapping failed.</param>
        /// <returns>True, if mapping succeeded</returns>
        public static bool TryMapPoint(Vector2 position, out NavSegmentPositionPointer pointer)
        {
            return NavGraph.TryMapPoint(position, out pointer);
        }

        /// <summary>
        /// Tries to map a point to a navigation position. The distance a point can be away is determined by searchRadius
        /// </summary>
        /// <param name="position">Position to map</param>
        /// <param name="pointer">Pointer to mapped position or an invalid pointer, if mapping failed.</param>
        /// <param name="searchRadius">Maxium search distance when trying to map.</param>
        /// <returns>True, if mapping succeeded</returns>
        public static bool TryMapPoint(Vector2 position, float searchRadius, out NavSegmentPositionPointer pointer)
        {
            return NavGraph.TryMapPoint(position, p => true, searchRadius, out pointer);
        }

        /// <summary>
        /// Tries to map a point to a navigation position. The distance a point can be away is determined by searchRadius. Also makes sure that the point is traversable for the supplied NavAgent.
        /// </summary>
        /// <param name="position">Position to map</param>
        /// <param name="pointer">Pointer to mapped position or an invalid pointer, if mapping failed.</param>
        /// <param name="searchRadius">Maxium search distance when trying to map.</param>
        /// <param name="agent">Maxium search distance when trying to map.</param>
        /// <returns>True, if mapping succeeded</returns>
        public static bool TryMapPoint(Vector2 position, float searchRadius, NavAgent agent, out NavSegmentPositionPointer pointer)
        {
            return NavGraph.TryMapPoint(position, p => agent.CouldBeLocatedAt(p), searchRadius, out pointer);
        }

        /// <summary>
        /// Tries to map an Agent to a navigation position. Uses last mapping to speed up search. Will only map the agent to a surface it could traverse.
        /// </summary>
        /// <param name="position">Position to map</param>
        /// <param name="lastMapping">Pointer with the previously mapped position. Taken as start for search.</param>
        /// <returns>True, if mapping succeeded</returns>
        internal static bool TryMapAgent(Vector2 position, NavSegmentPositionPointer lastMapping, NavAgent agent, out NavSegmentPositionPointer result)
        {
            return NavGraph.TryMapAgent(position, lastMapping, agent, out result);
        }

        internal static bool TryMapPointWithStaged(Vector2 position, out NavSegmentPositionPointer pointer)
        {
            return NavGraph.TryMapPointWithStaged(position, out pointer);
        }

        /// <returns>Random point on NavGraph. Might not be reachable.</returns>
        public static Vector2 GetRandomPointOnGraph()
        {
            return NavGraph.GetRandomPointOnGraph();
        }

        /// <summary>
        /// Enqueues a <typeparamref name="PathRequest">. The path will be solved async. Use the PathRequest object to check its status.
        /// </summary>
        /// <returns>PathRequest object representing the pathfinding job.</returns>
        public static void PathTo(PathRequest pathRequest)
        {
            pathRequest.SetToPending();
#if PBDEBUG
            Debug.Log($"PathRequest from: {pathRequest.start.GetPosition()} to: {pathRequest.goals[0].GetPosition()}");
#endif
            instance.pathRequestQueue.Enqueue(pathRequest);
        }

        /// <summary>
        /// Get all segments intersecting a rotated box. Segments can additionally be filtered by angle.
        /// </summary>
        /// <param name="rect">Intersection rect.</param>
        /// <param name="rotation">The rotation of the rect around its center.</param>
        /// <param name="filterFromAngle">Minium angle in degree of returned segments.</param>
        /// <param name="filterToAngle">Maximum angle in degree of returned segments.</param>
        public static List<NavSubsegmentPointer> BoxCast(Rect rect, float rotation, float filterFromAngle, float filterToAngle)
        {
            filterFromAngle = ClampAngle(filterFromAngle);
            filterToAngle = ClampAngle(filterToAngle);
            return NavGraph.BoxCast(rect, rotation, filterFromAngle, filterToAngle);
        }

        internal static List<NavSubsegmentPointer> BoxCastWithStaged(Rect rect, float rotation, float filterFromAngle, float filterToAngle)
        {
            return NavGraph.BoxCastWithStaged(rect, rotation, filterFromAngle, filterToAngle);
        }

        /// <summary>
        /// Similar to TryMapPoint, but meant for mapping goal positions. Has a configurable search radius.
        /// </summary>
        /// <param name="position">Position to map</param>
        /// <param name="maxMappingDistance">Maximum distance away from position a mapping is allowed to be.</param>
        /// <param name="pointer">Mapped pointer</param>
        /// <returns>True, if successfully mapped.</returns>
        [Obsolete("Use TryMapPoint instead")]
        public static bool TryFindClosestPointTo(Vector2 position, float maxMappingDistance, out NavSegmentPositionPointer pointer)
        {
            return NavGraph.TryMapPoint(position, (p) => true, maxMappingDistance, out pointer);
        }

        /// <summary>
        /// Get the position the <paramref name="pointer"/> is pointing at.
        /// </summary>
        internal static NavGraphNodeCluster GetClusterFromPositionPointer(NavSegmentPositionPointer pointer)
        {
            NavGraphNodeCluster cluster = null;
            NavGraph.TryGetClusterAt(pointer, out cluster);
            return cluster;
        }

        private static int nextFreeComponentId = 1;
        internal static int GeneratePBComponentId()
        {
            return nextFreeComponentId++;
        }

        private void StartPathfinderThreads()
        {
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                PathfinderThread p = new PathfinderThread(pathfinderThreadCancelationSource.Token, pathRequestQueue, NavGraph, 0);
                StartCoroutine(p.CoroutineRun());
            }
            else
            {
                for (int i = 0; i < PathBerserker2dSettings.PathfinderThreadCount; i++)
                {
                    PathfinderThread p = new PathfinderThread(pathfinderThreadCancelationSource.Token, pathRequestQueue, NavGraph, i);
                    Thread t = new Thread(p.Run);
                    t.Start();
                }
            }
        }

        private static float ClampAngle(float angle)
        {
            while (angle > 360)
                angle -= 360;
            while (angle < 0)
                angle += 360;
            return angle;
        }
    }
}
