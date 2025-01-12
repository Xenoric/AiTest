using AnyPath.Graphs.PlatformerGraph.SceneGraph;
using UnityEditor;
using UnityEngine;

namespace AnyPath.Examples.Platformer
{
    public class SceneGraphLinerRenderers : MonoBehaviour
    {
        public Material lineMaterial;
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(SceneGraphLinerRenderers))]
    public class SceneGraphLinerRenderersEditor : Editor
    {
        private SceneGraphLinerRenderers sceneGraphLinerRenderers;

        private void OnEnable()
        {
            sceneGraphLinerRenderers = (SceneGraphLinerRenderers) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Add Linerenderers"))
            {
                foreach (var edge in sceneGraphLinerRenderers.GetComponentsInChildren<PlatformerSceneGraphEdge>())
                {
                    var lr = edge.GetComponent<PlatformerSceneGraphEdgeLineRenderer>();
                    if (lr == null)
                        lr = edge.gameObject.AddComponent<PlatformerSceneGraphEdgeLineRenderer>();
                      
                    lr.SetMaterial(sceneGraphLinerRenderers.lineMaterial);
                    
                    EditorUtility.SetDirty(edge);
                }
            }

            if (GUILayout.Button("Remove Linerenderers"))
            {
                foreach (var edge in sceneGraphLinerRenderers.GetComponentsInChildren<PlatformerSceneGraphEdge>())
                {
                    var go = edge.gameObject;
                    var edgeLineRenderer = edge.GetComponent<PlatformerSceneGraphEdgeLineRenderer>();
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