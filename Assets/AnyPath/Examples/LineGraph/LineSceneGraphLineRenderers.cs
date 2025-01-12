using AnyPath.Graphs.Line.SceneGraph;
using UnityEditor;
using UnityEngine;

namespace AnyPath.Examples.Platformer
{
    public class LineSceneGraphLineRenderers : MonoBehaviour
    {
        public Material lineMaterial;
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(LineSceneGraphLineRenderers))]
    public class LineSceneGraphLineRenderersEditor : Editor
    {
        private LineSceneGraphLineRenderers sceneGraphLineRenderers;

        private void OnEnable()
        {
            sceneGraphLineRenderers = (LineSceneGraphLineRenderers) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Add Linerenderers"))
            {
                foreach (var edge in sceneGraphLineRenderers.GetComponentsInChildren<LineSceneGraphEdge>())
                {
                    var lr = edge.GetComponent<LineGraphSceneEdgeLineRenderer>();
                    if (lr == null)
                        lr = edge.gameObject.AddComponent<LineGraphSceneEdgeLineRenderer>();
                      
                    lr.SetMaterial(sceneGraphLineRenderers.lineMaterial);
                    
                    EditorUtility.SetDirty(edge);
                }
            }

            if (GUILayout.Button("Remove Linerenderers"))
            {
                foreach (var edge in sceneGraphLineRenderers.GetComponentsInChildren<LineSceneGraphEdge>())
                {
                    var go = edge.gameObject;
                    var edgeLineRenderer = edge.GetComponent<LineGraphSceneEdgeLineRenderer>();
                    if (edgeLineRenderer != null)
                    {
                        var lr = edgeLineRenderer.lineRenderer;
                        DestroyImmediate(edgeLineRenderer);   
                        DestroyImmediate(lr);
                    }
                    EditorUtility.SetDirty(go);
                }
            }
        }
    }
    #endif
}