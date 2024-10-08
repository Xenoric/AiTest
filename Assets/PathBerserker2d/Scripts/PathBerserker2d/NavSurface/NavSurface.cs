using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// A collection of line segments to traverse on.
    /// </summary>
    /// <remarks>
    /// NavSurfaces are independent collections of segments. At runtime their data gets added to the pathfinder.
    /// You can load and unload NavSurfaces at runtime. This allows you to setup Prefabs with navigation data and stream them in at runtime.
    /// Loading and unloading is as simple as enabling and disabling a NavSurface script.
    /// ## Baking
    /// The bake process **only consideres colliders that are children of the NavSurface**.
    /// This also extends to the clearance calculation of segment cells.
    /// Baking is currently limited to editor mode only. You can't bake at runtime.
    /// ## Transformations
    /// All baked postion data is relative to the current NavSurface transformation.
    /// Segments in calculated paths remain relative to the NavSurface of origin. 
    /// If you pathfind on a NavSurface and then move it, the path will reflect that transformation, without any extra calculations.
    ///
    /// See also \ref cc_navagent "Core concepts: NavAgent".
    /// <remarks/>
    [ScriptExecutionOrder(-55)]
    [AddComponentMenu("PathBerserker2d/Nav Surface")]
    public class NavSurface : MonoBehaviour, INavSegmentCreationParamProvider
    {
        internal const int CurrentBakeVersion = 2;

        public Rect WorldBounds => Geometry.EnlargeRect(Geometry.TransformBoundingRect(localBoundingRect, LocalToWorldMatrix), PathBerserker2dSettings.PointMappingDistance);

        public float MaxClearance => maxClearance;

        public float MinClearance => minClearance;

        public float CellSize => cellSize;

        public LayerMask ColliderMask
        {
            get => includedColliders;
            set => includedColliders = value;
        }

        /// <summary>
        /// Length of all segments combined
        /// </summary>
        public float TotalLineLength => totalLineLength;

        /// <summary>
        /// Segments exceeding this angle are removed from the bake output. Should equal the highest slope angle of your NavAgents.
        /// </summary>
        public float MaxSlopeAngle => maxSlopeAngle;

        /// <summary>
        /// Parameter for the line simplifier (Ramer-Douglas-Peucker). Higher values reduce overall segment count at the expense of fitting the original collider shape.
        /// </summary>
        public float SmallestDistanceYouCareAbout => smallestDistanceYouCareAbout;

        /// <summary>
        /// Segments shorter than this will be removed from the bake output.
        /// </summary>
        public float MinSegmentLength => minSegmentLength;

        /// <summary>
        /// Gets fired after a BakeJob initiated by a call to Bake() completes.
        /// THIS DOES NOT mean that the changes are already available to the pathfinder. That will happen later.
        /// Use OnReadyToPathfind for that instead. This only really tells you when you may start a new bake.
        /// </summary>
        public event Action OnBakingCompleted;

        /// Called after the NavSurface has been added to the pathfinder. Usually sometime after OnBakingCompleted has been called. Only know are the changes from baking actually visible in the pathfinder.
        /// IF THERE ARE MULTIPLE NavSurface Components on this GameObject you are ass out. These events will fire for both of them always. Don't put 2 NavSurfaces on the same GameObject!
        public event Action OnReadyToPathfind;

        /// Called after the NavSurface is removed from the pathfinder. Usually after the NavSurface is deleted OR REBAKED! Removing the old NavSurface data to replace it with new baked data will trigger this event. When this event is called, you can be sure that the old data NavData is no longer in use for anything.
        /// IF THERE ARE MULTIPLE NavSurface Components on this GameObject you are ass out. These events will fire for both of them always. Don't put 2 NavSurfaces on the same GameObject!
        public event Action OnRemovedFromPathfinding;

        internal List<NavSegment> NavSegments => navSegments;
        internal NavSurfaceBakeJob BakeJob { get; private set; }
        internal bool hasDataChanged;
        internal int BakeVersion => bakeVersion;
        internal int BakeIteration => bakeIteration;
        internal int PBComponentId { get; private set; }

        [Header("Bake Settings")]
        [Tooltip("Maximum height that gets checked for potential obstructions. Should equal the height of your largest NavAgent.")]
        [SerializeField]
        float maxClearance = 1.8f;

        [Tooltip("Parts of segments with less unobstructed space will be erased. Should equal the height of your smallest NavAgent.")]
        [SerializeField]
        float minClearance = 0.1f;

        [Tooltip("Size of a single segment part. Smaller numbers increase the accuracy of obstruction calculations at the expense of both bake and runtime performance.")]
        [SerializeField]
        float cellSize = 0.1f;

        [Tooltip("Colliders to consider for the bake process.")]
        [SerializeField]
        LayerMask includedColliders = ~0;


        [Tooltip("Use only colliders from gameobjects marked static.")]
        [SerializeField]
        bool onlyStaticColliders = false;

        [Tooltip("Segments exceeding this angle are removed from the bake output. Should equal the highest slope angle of your NavAgents.")]
        [SerializeField]
        [Range(0, 180)]
        float maxSlopeAngle = 180f;

        [Tooltip("Parameter for the line simplifier (Ramer-Douglas-Peucker). Higher values reduce overall segment count at the expense of fitting the original collider shape.")]
        [SerializeField]
        float smallestDistanceYouCareAbout = 0.1f;

        [Tooltip("Segments shorter than this will be removed from the bake output.")]
        [SerializeField]
        float minSegmentLength = 0.1f;

        [SerializeField]
        private List<NavSegment> navSegments = new List<NavSegment>();

        [SerializeField, HideInInspector]
        private Rect localBoundingRect;
        [SerializeField, HideInInspector]
        private float totalLineLength;
        [SerializeField, HideInInspector]
        // version of bake algorithm this surface was last baked with
        private int bakeVersion = 0;
        [SerializeField, HideInInspector]
        // number of distinct bakes
        private int bakeIteration = 0;

        Matrix4x4 localToWorldMat;

        #region Unity_Methods
        private void Awake()
        {
            UpdateLocalMatrix();
            PBComponentId = PBWorld.GeneratePBComponentId();
            PBWorld.NavGraphChangeSource.OnGraphChange += NavGraphChangeSource_OnGraphChange;
        }

        private void OnEnable()
        {
            if (navSegments != null && navSegments.Count > 0)
            {
                PBWorld.NavGraph.AddNavSurface(this);
            }
        }

        private void OnDisable()
        {
            PBWorld.NavGraph.RemoveNavSurface(this);
        }

        private void OnValidate()
        {
            if (minClearance <= 0)
                minClearance = 0.1f;
            if (maxClearance <= minClearance)
                maxClearance = minClearance + 0.1f;
            if (cellSize <= 0)
                cellSize = 0.1f;

            BakeJob = new NavSurfaceBakeJob(this);
            hasDataChanged = true;
        }

        private void Update()
        {
            UpdateLocalMatrix();
        }

        #endregion

        public Vector2 LocalToWorld(Vector2 pos)
        {
            return LocalToWorldMatrix.MultiplyPoint3x4(pos);
        }

        public Matrix4x4 LocalToWorldMatrix
        {
            get
            {
                return localToWorldMat;
            }
        }

        internal Matrix4x4 LocalToWorldMatrixEditor
        {
            get
            {
                return Matrix4x4.TRS(transform.position, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z), Vector3.one); ;
            }
        }

        public Vector2 WorldToLocal(Vector2 pos)
        {
            return WorldToLocalMatrix.MultiplyPoint3x4(pos);
        }

        public Matrix4x4 WorldToLocalMatrix => LocalToWorldMatrix.inverse;

        /// <summary>
        /// Updates NavSurface baked data. Baking largely runs in a different thread. This function should be run as a Coroutine. NavSurface will be removed from World first and added back, when baking is completed.
        /// Calling this function before the previous bake job completed, will abort the previous job.
        /// </summary>
        public IEnumerator Bake()
        {
            PBWorld.NavGraph.RemoveNavSurface(this);
            StartBakeJob();

            while (!BakeJob.IsFinished)
            {
                yield return null;
            }
            UpdateInternalData(BakeJob.navSegments, BakeJob.bounds);
            PBWorld.NavGraph.AddNavSurface(this);

            OnBakingCompleted?.Invoke();
        }

        internal void StartBakeJob()
        {
            UpdateLocalMatrix();
            if (BakeJob == null)
                BakeJob = new NavSurfaceBakeJob(this);
            else
                BakeJob.AbortJoin();

            var subtractors = GetComponentsInChildren<NavSegmentSubstractor>();
            Tuple<Rect, Vector2>[] substractorRects = new Tuple<Rect, Vector2>[subtractors.Length];
            for (int i = 0; i < subtractors.Length; i++)
            {
                var rT = subtractors[i].GetComponent<RectTransform>();
                var rect = rT.rect;
                Vector2 scaleFactor = rT.lossyScale * rect.size * 0.5f;
                Vector2 center = rect.center;

                rect.min = center - scaleFactor + (Vector2)rT.position;
                rect.max = center + scaleFactor + (Vector2)rT.position;

                substractorRects[i] = new Tuple<Rect, Vector2>(rect, new Vector2(subtractors[i].fromAngle, subtractors[i].toAngle));
            }

            var filter = new ColliderLayerFilter(includedColliders, onlyStaticColliders);
            var allColliders = filter.Filter(this.GetComponentsInChildren<Collider2D>()).ToArray();

            var it = new IntersectionTester(this, WorldToLocalMatrix);
            Polygon[][] polygons = new Polygon[allColliders.Length][];
            for (int i = 0; i < allColliders.Length; i++)
            {
                var col = allColliders[i];
                polygons[i] = it.ColliderToPolygon(col);
            }

            BakeJob.Start(polygons, it, substractorRects, WorldToLocalMatrix);
        }

        internal NavSegment GetSegment(int index)
        {
            return navSegments[index];
        }

        internal void UpdateInternalData(List<NavSegment> segments, Rect bounds)
        {
            if (segments == null)
            {
                Debug.LogError("Updating NavSurface failed. Got null as segments");
                return;
            }

            this.localBoundingRect = Geometry.TransformBoundingRect(bounds, WorldToLocalMatrix);

            this.totalLineLength = 0;
            foreach (var seg in segments)
                totalLineLength += seg.Length;

            this.navSegments = segments;
            bakeVersion = CurrentBakeVersion;
            hasDataChanged = true;
            bakeIteration++;
        }

        /// <summary>
        /// Filters and rethrows change events that have this NavSurface as target. It's a convenience feature.
        /// You may also just subscribe to PBWorld.NavGraphChangeSource.OnGraphChange and filter the changes yourself.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void NavGraphChangeSource_OnGraphChange(NavGraphChange arg1, int srcCompId)
        {
            if (srcCompId == PBComponentId)
            {
                switch (arg1)
                {
                    case NavGraphChange.NavSurfaceAdded:
                        OnReadyToPathfind?.Invoke();
                        break;
                    case NavGraphChange.NavSurfaceRemoved:
                        OnRemovedFromPathfinding?.Invoke();
                        break;
                    default:
                        // this could happen if there is another component on this gameobject which the change event was directed at. Just ignore it.
                        break;
                }
            }
        }

        private void UpdateLocalMatrix()
        {
            localToWorldMat = Matrix4x4.TRS(transform.position, Quaternion.Euler(0, 0, transform.rotation.eulerAngles.z), Vector3.one);
        }
    }
}