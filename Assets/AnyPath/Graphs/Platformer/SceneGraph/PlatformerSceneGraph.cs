using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;
using Random = UnityEngine.Random;

namespace AnyPath.Graphs.PlatformerGraph.SceneGraph
{
    /// <summary>
    /// A simple tool to build PlatformerGraphs directly into your Unity scene. This can be used as a starting point for your level editor
    /// or just used as is.
    /// <para>
    /// Use GameObject->2D Objects->PlatformerSceneGraph to create one in your scene.
    /// By default, two nodes will be created with an edge connecting them. Adding nodes is done by dragging the cirle that appears when the node
    /// is selected. Dragging it onto another node will create an edge connecting the two nodes. Edges are also gameObjects and their properties can
    /// be adjusted by selecting them.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Make sure gizmos are on when editing a PlatformerSceneGraph.
    /// </para>
    /// <para>
    /// Don't forget to click the Build button in the inspector when you're done
    /// editing the graph. This will convert the data into a suitable format which can be used to create the native PlatformerGraph
    /// </para>
    /// </remarks>
    public class PlatformerSceneGraph : MonoBehaviour
    {
        /// <summary>
        /// All vertices that make up the graph. This can be injected in the <see cref="PlatformerGraph"/> constructor.
        /// This data is automatically generated when clicking the Build button.
        /// </summary>
        [HideInInspector] public List<float2> vertices;
        
        /// <summary>
        /// All of the directed that make up the graph. This can be injected in the <see cref="PlatformerGraph"/> constructor.
        /// This data is automatically generated when clicking the Build button.
        /// </summary>
        [HideInInspector] public List<PlatformerGraph.Edge> directedEdges;
        
        /// <summary>
        /// All of the udirected that make up the graph. This can be injected in the <see cref="PlatformerGraph"/> constructor.
        /// This data is automatically generated when clicking the Build button.
        /// </summary>
        [HideInInspector] public List<PlatformerGraph.Edge> undirectedEdges;

        [HideInInspector, ExcludeFromDocs] public string[] flagNames;
        [HideInInspector, ExcludeFromDocs] public Color defaultColor = Color.white;
        [HideInInspector, ExcludeFromDocs] public Color[] flagColors;

        [HideInInspector, ExcludeFromDocs] public Color nodeColor = Color.gray;
        [HideInInspector, ExcludeFromDocs] public Color selectedNodeColor = Color.white;
        [HideInInspector, ExcludeFromDocs] public float nodeSize = .24f;

        
        [ExcludeFromDocs] public Color GetFlagColor(int bitIndex) => bitIndex >= 0 && bitIndex < flagColors.Length ? flagColors[bitIndex] : Color.white;
        [ExcludeFromDocs] public string GetFlagName(int bitIndex) => bitIndex >= 0 && bitIndex < flagNames.Length ? flagNames[bitIndex] : string.Empty;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (flagNames == null || flagNames.Length != 32)
            {
                flagNames = new string[32];
            }

            if (flagColors == null || flagColors.Length != 32)
            {
                flagColors = new Color[32];
                flagColors[0] = Color.red;
                flagColors[1] = Color.green;
                flagColors[2] = Color.blue;
                for (int i = 3; i < 32; i++)
                {
                    flagColors[i] = Random.ColorHSV(0, 1);
                }
            }
        }

        /// <summary>
        /// Creates the native graph struct. See <see cref="PlatformerGraph"/> for details.
        /// </summary>
        /// <param name="allocator">Allocator to use</param>
        /// <param name="directedEdgesRaycastable">Should direct edges be hit by raycast queries? See <see cref="PlatformerGraph"/> for more information.</param>
        /// <returns></returns>
        public PlatformerGraph CreateGraph(Allocator allocator, bool directedEdgesRaycastable = false)
        {
            return new PlatformerGraph(vertices, undirectedEdges, directedEdges, allocator, directedEdgesRaycastable);
        }

        /// <summary>
        /// Creates a basic scene graph
        /// </summary>
        [MenuItem("GameObject/2D Object/Anypath Platformer Scene Graph", false, 10)]
        [ExcludeFromDocs]
        public static void CreateGraph()
        {
            var graphGO = new GameObject("Platformer Graph");
            graphGO.AddComponent<PlatformerSceneGraph>();
          
            var nodeGO = new GameObject("Node");
            nodeGO.transform.SetParent(graphGO.transform);
            nodeGO.transform.localPosition = new Vector3(-5, 0, 0);
            var nodeA = nodeGO.AddComponent<PlatformerSceneGraphNode>();
            
            var nodeGO2 = new GameObject("Node");
            nodeGO2.transform.SetParent(graphGO.transform);
            nodeGO2.transform.localPosition = new Vector3(5, 0, 0);
            var nodeB = nodeGO2.AddComponent<PlatformerSceneGraphNode>();

            var edgeGO = new GameObject("Edge");
            edgeGO.transform.SetParent(graphGO.transform);
            var edge = edgeGO.AddComponent<PlatformerSceneGraphEdge>();
            edge.transform.position = .5f * (nodeA.transform.position + nodeB.transform.position);
            edge.a = nodeA;
            edge.b = nodeB;
            
            Undo.RegisterCreatedObjectUndo(graphGO, "Create " + graphGO.name);
            Selection.activeGameObject = graphGO;
        }
#endif
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(PlatformerSceneGraph)), ExcludeFromDocs]
    public class SceneGraphEditor : Editor
    {
        private PlatformerSceneGraph sceneGraph;

        private void OnEnable()
        {
            sceneGraph = (PlatformerSceneGraph) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (sceneGraph.vertices != null)
            {
                EditorGUILayout.HelpBox($"{sceneGraph.vertices.Count} vertices\n{(sceneGraph.directedEdges.Count + sceneGraph.undirectedEdges.Count)} edges", MessageType.Info);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Build"))
            {
                BuildData();
            }
            
            if (GUILayout.Button("Clean"))
            {
                Clean();
            }

            EditorGUILayout.Space();
            DoSceneVisibility();
            EditorGUILayout.Space();
            DoFlags();
        }

        void DoSceneVisibility()
        {
            EditorGUILayout.LabelField("Selection Mode", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("All"))
                SceneVisibilityManager.instance.EnableAllPicking();

            if (GUILayout.Button("Edges Only"))
            {
                var gos = sceneGraph.GetComponentsInChildren<PlatformerSceneGraphNode>().Select(node => node.gameObject).ToArray();
                SceneVisibilityManager.instance.DisablePicking(gos, false);
                gos = sceneGraph.GetComponentsInChildren<PlatformerSceneGraphEdge>().Select(edge => edge.gameObject).ToArray();
                SceneVisibilityManager.instance.EnablePicking(gos, false);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DoFlags()
        {
            if (sceneGraph.flagColors.Length != 32)
                Array.Resize(ref sceneGraph.flagColors, 32);
            if (sceneGraph.flagNames.Length != 32)
                Array.Resize(ref sceneGraph.flagNames, 32);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.LabelField("Node Handle Size", EditorStyles.boldLabel);
            sceneGraph.nodeSize = Mathf.Clamp(EditorGUILayout.FloatField(sceneGraph.nodeSize), 0, 100);
            EditorGUILayout.LabelField("Node Color", EditorStyles.boldLabel);
            sceneGraph.nodeColor = EditorGUILayout.ColorField(sceneGraph.nodeColor);
            EditorGUILayout.LabelField("Selected Node Color", EditorStyles.boldLabel);
            sceneGraph.selectedNodeColor = EditorGUILayout.ColorField(sceneGraph.selectedNodeColor);
            
            EditorGUILayout.LabelField("Default Color", EditorStyles.boldLabel);
            sceneGraph.defaultColor = EditorGUILayout.ColorField(sceneGraph.defaultColor);

            EditorGUILayout.LabelField("Flag Settings", EditorStyles.boldLabel);
           
            for (int i = 0; i < 32; i++)
            {
                var col = sceneGraph.flagColors[i];
                string name = sceneGraph.flagNames[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField((i+1).ToString(), GUILayout.MaxWidth(32));
                name = EditorGUILayout.TextField(name);
                sceneGraph.flagNames[i] = name;
                col = EditorGUILayout.ColorField(col,GUILayout.MaxWidth(64));
                sceneGraph.flagColors[i] = col;

                EditorGUILayout.EndHorizontal();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(sceneGraph);
            }
        }

        void Clean()
        {
            int invalid = 0;
            int duplicate = 0;
            HashSet<int2> duplicates = new HashSet<int2>();
            foreach (var node in sceneGraph.GetComponentsInChildren<PlatformerSceneGraphNode>())
            {
                node.editorOnlyEdges.Clear();
            }
            
            foreach (var edge in sceneGraph.GetComponentsInChildren<PlatformerSceneGraphEdge>())
            {
                if (!edge.IsValid())
                {
                    invalid++;
                    DestroyImmediate(edge.gameObject);
                    continue;
                }

                int idA = edge.a.GetInstanceID();
                int idB = edge.b.GetInstanceID();
                int2 sortedId = new int2(math.min(idA, idB), math.max(idA, idB));
                if (!duplicates.Add(sortedId))
                {
                    duplicate++;
                    DestroyImmediate(edge.gameObject);
                    continue;
                }
                
                edge.a.editorOnlyEdges.Add(edge);
                edge.b.editorOnlyEdges.Add(edge);
                edge.Recenter();
            }
            
            Debug.Log($"Removed {invalid} invalid and {duplicate} duplicate edges.");
            EditorUtility.SetDirty(sceneGraph);
        }

        void BuildData()
        {
            var builder = new PlatformerGraphBuilder();
            foreach (var node in sceneGraph.GetComponentsInChildren<PlatformerSceneGraphNode>())
            {
                builder.SetVertex(node.GetInstanceID(), node.GetPosition());
            }

            foreach (var edge in sceneGraph.GetComponentsInChildren<PlatformerSceneGraphEdge>())
            {
                if (!edge.IsValid())
                    continue;
                
                if (edge.directed)
                {
                    builder.LinkDirected(edge.a.GetInstanceID(), edge.b.GetInstanceID(), edge.enterCost, edge.flags, edge.optionalId);
                }
                else
                {
                    builder.LinkUndirected(edge.a.GetInstanceID(), edge.b.GetInstanceID(), edge.enterCost, edge.flags, edge.optionalId);
                }
            }
            
            builder.GetData(out sceneGraph.vertices, out sceneGraph.undirectedEdges, out sceneGraph.directedEdges);
            EditorUtility.SetDirty(sceneGraph);
           
            Debug.Log("Graph data updated.");
        }
    }
    #endif
}