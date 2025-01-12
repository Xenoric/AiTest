using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;
using Object = UnityEngine.Object;

namespace AnyPath.Graphs.PlatformerGraph.SceneGraph
{
    /// <summary>
    /// Defines an edge between two vertices that is used to construct a Platformer graph.
    /// </summary>
    public class PlatformerSceneGraphEdge : MonoBehaviour
    {

        
        /// <summary>
        /// Endpoint node of this edge.
        /// </summary>
        public PlatformerSceneGraphNode a;
        
        /// <summary>
        /// Endpoint node of this edge.
        /// </summary>
        public PlatformerSceneGraphNode b;
        
        /// <summary>
        /// Extra cost associated with traversing this edge. A higher value discourages A* to use this edge in a path.
        /// </summary>
        [Min(0)] public float enterCost;
        
        /// <summary>
        /// Flags to associate with this edge. This can be used to filter raycast queries and to exclude edges from being considered.
        /// </summary>
        [HideInInspector] public int flags; // custom dropdown
        
        /// <summary>
        /// Gets mapped to the edge's Id and can be used to map back to some other object (like a MonoBehaviour)
        /// </summary>
        public int optionalId;
        
        /// <summary>
        /// Is the edge directional? If true, traversal can only happen from A to B.
        /// </summary>
        public bool directed;
        
        [ExcludeFromDocs]
        public bool IsValid()
        {
            return a != null && b != null;
        }
        
        /// <summary>
        /// Positions this edge at the center of it's endpoints to make selecting it easier
        /// </summary>
        [ExcludeFromDocs]
        public void Recenter()
        {
            if (IsValid())
                transform.position = .5f * (a.transform.position + b.transform.position);
        }
        
        
        private static List<Color> flagColorCache = new List<Color>();
        public static IEnumerable<(Vector2, Vector2, Color)> GetColoredSegments(PlatformerSceneGraph graph, int flags, Vector2 posA, Vector2 posB)
        {
            Vector2 l = posB - posA;
            GetFlagColors(graph, flags, flagColorCache);
            for (int i = 0; i < flagColorCache.Count; i++)
            {
                float t1 = (float) i / flagColorCache.Count;
                float t2 = (float) (i + 1) / flagColorCache.Count;
                yield return (posA + t1 * l, posA + t2 * l, flagColorCache[i]);
            }
        }

        public static void GetFlagColors(PlatformerSceneGraph graph, int flags, List<Color> colors)
        {
            colors.Clear();
            if (flags == 0)
            {
                colors.Add(graph.defaultColor);
                return;
            }
            
            for (int i = 0; i < 32; i++)
            {
                if ((flags & (1 << i)) != 0)
                {
                    colors.Add(graph.GetFlagColor(i));
                }
            }
        }

#if UNITY_EDITOR
        [NonSerialized] public PlatformerSceneGraph editorOnlyParentGraphCached;
        private void OnDrawGizmos()
        {
            if (!IsValid())
                return;

            Vector2 posA = a.transform.position;
            Vector2 posB = b.transform.position;
            Vector2 center = .5f * (posA + posB);
            
            if (editorOnlyParentGraphCached == null)
                editorOnlyParentGraphCached = GetComponentInParent<PlatformerSceneGraph>();
            if (editorOnlyParentGraphCached == null)
                return;

            if (flags == 0)
            {
                Gizmos.color = editorOnlyParentGraphCached.defaultColor;
                Gizmos.DrawLine(posA, posB);
            }
            else
            {
                // Draw colored segments based on flags
                foreach (var (segA, segB, color) in GetColoredSegments(editorOnlyParentGraphCached, flags, posA, posB))
                {
                    Gizmos.color = color;
                    Gizmos.DrawLine(segA, segB);
                }
            }
            
            if (directed)
            {

                float arrowHalfSize = editorOnlyParentGraphCached.nodeSize / 2;
                
                Vector2 dir = (posB - posA).normalized;
                Vector2 normal = new Vector2(-dir.y, dir.x);
                Vector2 arrowEnd = center + dir * arrowHalfSize;
                Gizmos.DrawLine(center - dir * arrowHalfSize + normal * arrowHalfSize, arrowEnd);
                Gizmos.DrawLine(center - dir * arrowHalfSize - normal * arrowHalfSize, arrowEnd);
            }
            
            if (enterCost > 0)
            {
                Handles.Label(center, $"+{enterCost}");
            }
        }

        private void OnValidate()
        {
            if (IsValid())
            {
                if (!a.editorOnlyEdges.Contains(this))
                    a.editorOnlyEdges.Add(this);
                if (!b.editorOnlyEdges.Contains(this))
                    b.editorOnlyEdges.Add(this);
            }
            Recenter();
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(PlatformerSceneGraphEdge)), ExcludeFromDocs]
    [CanEditMultipleObjects]
    public class SceneGraphEdgeEditor : Editor
    {
        private PlatformerSceneGraph sceneGraph;
        private PlatformerSceneGraphEdge[] edges;
        private static int[] bitCountCache = new int[32];

        private void OnEnable()
        {
            edges = new PlatformerSceneGraphEdge[targets.Length];
            for (int i = 0; i < edges.Length; i++)
                edges[i] = (PlatformerSceneGraphEdge)targets[i];
        }

        public override void OnInspectorGUI()
        {
            if (edges.Any(e => !e.IsValid()))
            {
                EditorGUILayout.HelpBox(edges.Length == 1 ? "Edge is invalid." : "Selection contains invalid edges", MessageType.Error);
            }
            
            base.OnInspectorGUI();

            if (edges.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                DoSplit();
                DoFlip();
                EditorGUILayout.EndHorizontal();
                
                if (GUILayout.Button("Select Graph"))
                {
                    var graph = edges[0].GetComponentInParent<PlatformerSceneGraph>();
                    if (graph != null)
                    {
                        Selection.activeGameObject = graph.gameObject;
                    }
                }
                
                EditorGUILayout.Space();
                DoFlags();
            }
        }

        /*
        private void OnSceneGUI()
        {
            // Drawing thicker outlines for selected
            // But somehow this also seems to make the edges be much easier to select by just clicking on the line
            // We shorten the thicker line a bit to make the endpoints easier to select

            const float width = 3;
            foreach (var edge in edges)
            {
                if (edge.IsValid())
                {
                    Vector3 posA = edge.a.transform.position;
                    Vector3 posB = edge.b.transform.position;
                    posA.z = 10;
                    posB.z = 10;
                    // shorten a bit
                    // posA = Vector2.Lerp(posA, posB, .25f);
                    // posB = Vector2.Lerp(posB, posA, .25f);
                    
                    if (edge.flags == 0)
                    {
                        Handles.color = Color.white;
                        Handles.DrawLine(posA, posB, width);
                        continue;
                    }
                    
                    if (edge.editorOnlyParentGraphCached == null)
                        edge.editorOnlyParentGraphCached = edge.GetComponentInParent<PlatformerSceneGraph>();
                    if (edge.editorOnlyParentGraphCached == null)
                        continue;
                    
                    foreach (var (segA, segB, color) in SceneGraphEdgeEditor.GetColoredSegments(
                        edge.editorOnlyParentGraphCached, 
                        edge.flags, posA, posB))
                    {
                        Handles.color = color;
                        Handles.DrawLine(segA, segB, width);
                    }
                }
            }
        }
        */

        void DoFlip()
        {
            if (!edges.Any(edge => edge.directed))
                return;

            if (!GUILayout.Button("Flip"))
                return;
            
            for (int i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                if (!edge.directed)
                    continue;
                
                var tmp = edge.a;
                edge.a = edge.b;
                edge.b = tmp;
                EditorUtility.SetDirty(edge);
            }
        }

        void DoSplit()
        {
            if (!GUILayout.Button("Split"))
                return;

            List<Object> newNodes = new List<Object>();
            for (int i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                if (!edge.IsValid())
                    continue;

                var oldA = edge.a;
                var oldB = edge.b;
                Vector2 center = .5f * (Vector2)(edge.a.transform.position + edge.b.transform.position);
                
                var newEdge1 = Instantiate(edge, edge.transform.parent);
                var newEdge2 = Instantiate(edge, edge.transform.parent);
                Undo.RegisterCreatedObjectUndo(newEdge1.gameObject, "Split Edge");
                Undo.RegisterCreatedObjectUndo(newEdge2.gameObject, "Split Edge");

                var newNode = SceneGraphNodeEditor.CreateNode(edge.transform.parent, center);
                newEdge1.a = oldA;
                newEdge1.b = newNode;
                newEdge2.a = oldB;
                newEdge2.b = newNode;
                
                newNodes.Add(newNode.gameObject);

                Undo.DestroyObjectImmediate(edge.gameObject);
            }

            if (newNodes.Count > 0)
            {
                Selection.objects = newNodes.ToArray();
            }
        }
        
        void DoFlags()
        {
            EditorGUILayout.LabelField("Flags", EditorStyles.boldLabel);
            
            if (sceneGraph == null)
                sceneGraph = edges[0].GetComponentInParent<PlatformerSceneGraph>();
            
            EditorGUI.BeginChangeCheck();

 
            int newFlags = 0;
            var style = new GUIStyle();
            
            CountBits();
            for (int i = 0; i < 32; i++)
            {
                bool isSet = bitCountCache[i] > 0;
                EditorGUI.showMixedValue = isSet && bitCountCache[i] != edges.Length;
                style.normal.textColor = sceneGraph.GetFlagColor(i);
                isSet = EditorGUILayout.ToggleLeft($"{(i + 1).ToString()}. {sceneGraph.GetFlagName(i)}", isSet, style);
                if (isSet)
                    newFlags |= 1 << i;
            }
            
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < edges.Length; i++)
                {
                    var edge = edges[i];
                    edge.flags = newFlags;
                    EditorUtility.SetDirty(edge);
                }
            }
        }

        void CountBits()
        {
            Array.Clear(bitCountCache, 0, bitCountCache.Length);
            foreach (var edge in edges)
            {
                for (int i = 0; i < 32; i++)
                {
                    if ((edge.flags & (1 << i)) != 0)
                        bitCountCache[i]++;
                }
            }
        }
        
        public static GameObject CreateEdge(PlatformerSceneGraphNode a, PlatformerSceneGraphNode b)
        {
            var newGO = new GameObject("Edge");
            newGO.transform.SetParent(a.transform.parent);
            newGO.transform.position = .5f * (a.transform.position + b.transform.position);
            newGO.transform.SetAsLastSibling();
            
            var newEdge = newGO.AddComponent<PlatformerSceneGraphEdge>();
            newEdge.a = a;
            newEdge.b = b;
            a.editorOnlyEdges.Add(newEdge);
            b.editorOnlyEdges.Add(newEdge);
            
            Undo.RegisterCreatedObjectUndo(newGO, "Create Edge");
            return newGO;
        }

    }
#endif
}