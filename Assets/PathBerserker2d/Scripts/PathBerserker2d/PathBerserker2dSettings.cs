using System;
using UnityEngine;

namespace PathBerserker2d
{
    /// <summary>
    /// Contains all project wide settings for PathBerserker2d.
    /// </summary>
    /// <remarks>
    /// Find it at *Edit/Project Settings/PathBerserker2d*
    /// </remarks>
    public class PathBerserker2dSettings : ScriptableObject
    {
        public const string GlobalSettingsFolder = "Assets/PathBerserker2d/Resources";
        public const string GlobalSettingsFile = "PathBerserker2dSettings";

        internal readonly static string[] buildinNavLinkTypeNames = new string[] {
            "corner", // -1
            "jump",
            "fall",
            "teleport",
            "climb",
            "elevator"
        };

        internal static PathBerserker2dSettings instance;

        /// <summary>
        /// Should unselected links be drawn? Only relevant when not in play mode.
        /// </summary>
        public static bool DrawUnselectedLinks { get { return instance.drawUnselectedLinks; } }

        /// <summary>
        /// Array of all link type names
        /// </summary>
        public static string[] NavLinkTypeNames { get { return instance.navLinkTypeNames; } }

        /// <summary>
        /// Array of all NavTag colors
        /// </summary>
        public static Color[] NavLinkTypeColors { get { return instance.navLinkTypeColors; } }

        /// <summary>
        /// Array of all NavTag names
        /// </summary>
        public static string[] NavTags { get { return instance.navTags; } }

        /// <summary>
        /// Array of all NavTag colors
        /// </summary>
        public static Color[] NavTagColors { get { return instance.navTagColors; } }

        /// <summary>
        /// Should unselected surfaces be drawn? Only relevant when not in play mode.
        /// </summary>
        public static bool DrawUnselectedSurfaces { get { return instance.drawUnselectedSurfaces; } }

        /// <summary>
        /// Should unselected substractors be drawn? Only relevant when not in play mode.
        /// </summary>
        public static bool DrawUnselectedSubstractors { get { return instance.drawUnselectedSubstractors; } }

        /// <summary>
        /// Should unselected area markers be drawn? Only relevant when not in play mode.
        /// </summary>
        public static bool DrawUnselectedAreaMarkers { get { return instance.drawUnselectedAreaMarkers; } }

        /// <summary>
        /// Maximum distance a point will try to be mapped to the NavGraph. Used in performance critical functions (e.g. mapping a NavAgents position) and should be as small as possible.
        /// </summary>
        public static float PointMappingDistance { get { return instance.pointMappingDistance; } }

        /// <summary>
        /// Time between NavGraph updates. NavGraph queues changes to apply them in batch at this interval. Lower values will lower performance.
        /// </summary>
        public static float InitiateUpdateInterval { get { return instance.initiateUpdateInterval; } }

        /// <summary>
        /// Amount of threads used for pathfinding. NOTE: WebGL doesn't support threads. If you build for WebGL this number is meaningless.
        /// </summary>
        public static int PathfinderThreadCount
        {
            get
            {
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                    return 1;
                return instance.pathfinderThreadCount;
            }
        }

        /// <summary>
        /// Draw the NavGraph while in play mode?
        /// </summary>
        public static bool DrawGraphWhilePlaying { get { return instance.drawGraphWhilePlaying; } }

        /// <summary>
        /// Maximum distance to search for the nearest NavGraph position to a point. Used in functions to e.g. map a mouse cursor click to a nav position. Make it as large as you want.
        /// </summary>
        [System.Obsolete]
        public static float ClosestToSegmentMaxDistance { get { return instance.closestToSegmentMaxDistance; } }

        /// <summary>
        /// Line width of NavGraph/NavSurface visualization.
        /// </summary>
        public static float NavSurfaceLineWidth { get { return instance.navSurfaceLineWidth; } }

        /// <summary>
        /// Line width of NavAreaMarker visualization (only visible in playmode)
        /// </summary>
        public static float NavAreaMarkerLineWidth { get { return instance.navAreaMarkerLineWidth; } }

        /// <summary>
        /// Whether to convert a PolygonCollider2d to a polygon with CreateMesh() or by reading its first Path.
        /// </summary>
        public static bool UsePolygonCollider2dPathsForBaking { get { return instance.usePolygonCollider2dPathsForBaking; } }

        /// <summary>
        /// Converts a link type name to its corresponding integer. Throws an ArgumentException if name is not a valid link type name.
        /// </summary>
        public static int GetLinkTypeFromName(string name)
        {
            var names = NavLinkTypeNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (name == names[i])
                    return i;
            }
            throw new ArgumentException(name + "is not a valid link type name. (case sensitive!)");
        }

        [Tooltip("Maximum distance a point will try to be mapped to the NavGraph. Used in performance critical functions (e.g. mapping a NavAgents position) and should be as small as possible.")]
        [Header("Pathfinding")]
        [SerializeField, HideInInspector]
        private float pointMappingDistance = 0.1f;

        [SerializeField, HideInInspector]
        private int pathfinderThreadCount = 1;

        [Tooltip("Time between NavGraph updates. NavGraph queues changes to apply them in batch at this interval. Lower values will lower performance.")]
        [SerializeField, HideInInspector]
        private float initiateUpdateInterval = 0.1f;

        [Tooltip("Maximum distance to search for the nearest NavGraph position to a point. Used in functions to e.g. map a mouse cursor click to a nav position. Make it as large as you want.")]
        [SerializeField, HideInInspector]
        private float closestToSegmentMaxDistance = 20;

        [Header("NavLinks")]
        [SerializeField, HideInInspector]
        private string[] navLinkTypeNames = new string[] { };
        [SerializeField, HideInInspector]
        private Color[] navLinkTypeColors = new Color[] { };

        [Header("NavSegments")]
        [SerializeField, HideInInspector]
        private string[] navTags = new string[] { "default" };
        [SerializeField, HideInInspector]
        private Color[] navTagColors = new Color[] { Color.clear };

        [Tooltip("Should unselected links be drawn? Only relevant when not in play mode.")]
        [Header("Visualization")]
        [SerializeField, HideInInspector]
        private bool drawUnselectedLinks = true;

        [Tooltip("Should unselected surfaces be drawn? Only relevant when not in play mode.")]
        [SerializeField, HideInInspector]
        private bool drawUnselectedSurfaces = true;

        [Tooltip("Should unselected substractors be drawn? Only relevant when not in play mode.")]
        [SerializeField, HideInInspector]
        private bool drawUnselectedSubstractors = true;

        [Tooltip("Should unselected area markers be drawn? Only relevant when not in play mode.")]
        [SerializeField, HideInInspector]
        private bool drawUnselectedAreaMarkers = true;

        [Tooltip("Draw the NavGraph while in play mode?")]
        [SerializeField, HideInInspector]
        private bool drawGraphWhilePlaying = true;

        [Tooltip("Line width of NavGraph/NavSurface visualization")]
        [SerializeField, HideInInspector]
        private float navSurfaceLineWidth = 0.04f;

        [Tooltip("Line width of NavAreaMarker visualization (only visible in playmode)")]
        [SerializeField, HideInInspector]
        private float navAreaMarkerLineWidth = 0.04f;

        [Tooltip("Whether to convert a PolygonCollider2d to a polygon with CreateMesh() or by reading its first Path.")]
        [SerializeField, HideInInspector]
        private bool usePolygonCollider2dPathsForBaking = false;

        internal void OnValidate()
        {
            ValidateLinkTypeNames();
            pointMappingDistance = Mathf.Max(0.001f, pointMappingDistance);
            pathfinderThreadCount = Mathf.Max(1, pathfinderThreadCount);
            initiateUpdateInterval = Mathf.Max(0.1f, initiateUpdateInterval);

            navTags[0] = "default";
            navTagColors[0] = Color.clear;
            if (navTags.Length > 32)
            {
                System.Array.Resize(ref navTags, 32);
            }
            if (navTagColors.Length != navTags.Length)
            {
                int oldLength = navTagColors.Length;
                System.Array.Resize(ref navTagColors, navTags.Length);
                for (int i = oldLength; i < navTags.Length; i++)
                    navTagColors[i] = DifferentColors.GetColor(i);
            }
        }

        internal void ValidateLinkTypeNames()
        {
            if (navLinkTypeNames.Length < buildinNavLinkTypeNames.Length)
            {
                navLinkTypeNames = new string[buildinNavLinkTypeNames.Length];
            }
            if (navLinkTypeNames.Length > 32)
            {
                System.Array.Resize(ref navLinkTypeNames, 32);
            }
            if (navLinkTypeColors.Length != navLinkTypeNames.Length)
            {
                int oldLength = navLinkTypeColors.Length;
                System.Array.Resize(ref navLinkTypeColors, navLinkTypeNames.Length);
                for (int i = oldLength; i < navLinkTypeNames.Length; i++)
                    navLinkTypeColors[i] = DifferentColors.GetColor(i);
            }

            for (int i = 0; i < buildinNavLinkTypeNames.Length; i++)
            {
                navLinkTypeNames[i] = buildinNavLinkTypeNames[i];
            }
        }

        /// <summary>
        /// Get the human readable name of a specific link type.
        /// </summary>
        public static string GetLinkTypeName(int linkType)
        {
            return instance.navLinkTypeNames[linkType];
        }

        /// <summary>
        /// Get color associated with specific link type.
        /// </summary>
        public static Color GetLinkTypeColor(int linkType)
        {
            return instance.navLinkTypeColors[linkType];
        }

        /// <summary>
        /// Set the color of a specific NavLinkType.
        /// </summary>
        internal static void SetLinkTypeColor(int navTag, Color color)
        {
            instance.navLinkTypeColors[navTag] = color;
        }

        /// <summary>
        /// Get the assigned color of a specific NavTag.
        /// </summary>
        public static Color GetNavTagColor(int navTag)
        {
            return instance.navTagColors[navTag];
        }

        /// <summary>
        /// If the NavTag does not exists, returns 0
        /// </summary>
        public static int EnsureNavTagExists(int navTag)
        {
            if (navTag < 0 || navTag >= NavTags.Length)
                return 0;
            return navTag;
        }

        /// <summary>
        /// Set the color of a specific NavTag.
        /// </summary>
        internal static void SetNavTagColor(int navTag, Color color)
        {
            instance.navTagColors[navTag] = color;
        }

        /// <summary>
        /// If the linkType does not exists, returns 0
        /// </summary>
        public static int EnsureNavLinkTypeExists(int linkType)
        {
            if (linkType < 0 || linkType >= NavLinkTypeNames.Length)
                return 1;
            return linkType;
        }
    }
}
