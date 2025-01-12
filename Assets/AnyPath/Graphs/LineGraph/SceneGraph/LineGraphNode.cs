using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;

namespace AnyPath.Graphs.Line.SceneGraph
{
    /// <summary>
    /// Defines a node (vertex) that is used to construct a line graph.
    /// </summary>
    [ExecuteAlways, SelectionBase]
    public class LineGraphNode : MonoBehaviour
    {
        [ExcludeFromDocs]
        public float3 GetPosition()
        {
            var pos = transform.position;
            return pos;
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
                RecenterMyEdges();
        }
#endif

#if UNITY_EDITOR
        [NonSerialized] public LineSceneGraph editorOnlyParentGraphCached;
        
        // somehow neccessary to make this node selectable
        private void OnDrawGizmos()
        {
            if (editorOnlyParentGraphCached == null)
                editorOnlyParentGraphCached = GetComponentInParent<LineSceneGraph>();
            if (editorOnlyParentGraphCached == null)
                return;
            
            Gizmos.color = Selection.Contains(this.gameObject) ? editorOnlyParentGraphCached.selectedNodeColor : editorOnlyParentGraphCached.nodeColor;
            Gizmos.DrawCube(transform.position, editorOnlyParentGraphCached.nodeSize * Vector3.one);
        }

        /// <summary>
        /// This is purely here to recenter edges when this node gets moved in the editor
        /// </summary>
        [HideInInspector] public List<LineSceneGraphEdge> editorOnlyEdges = new List<LineSceneGraphEdge>();
        void RecenterMyEdges()
        {
            for (int i = editorOnlyEdges.Count - 1; i >= 0; i--)
            {
                if (editorOnlyEdges[i] == null)
                {
                    editorOnlyEdges.RemoveAt(i);
                    continue;
                }
                
                editorOnlyEdges[i].Recenter();
            }
        }
#endif
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(LineGraphNode))]
    [CanEditMultipleObjects]
    public class SceneGraphNodeEditor : Editor
    {
        private LineGraphNode node;
        private static readonly Vector2 PickRectSize = new Vector3(50, 50); // In pixels
        private static readonly Color ConnectionColor = new Color(1, 1, 1, 1);
        //private static float handleSize = .25f;
        private Vector3 dragPosition;
        
        private bool didDrag;
        private Vector3 lastDraggedToPosition;
        private LineGraphNode lastDraggedToNode;

        private void OnEnable()
        {
            node = (LineGraphNode) target;
        }

        public static LineGraphNode CreateNode(Transform parent, Vector3 position)
        {
            var newGO = new GameObject("Node");
            newGO.transform.SetParent(parent);
            newGO.transform.position = position;
            newGO.transform.SetAsFirstSibling();
            var newNode = newGO.AddComponent<LineGraphNode>();
            Undo.RegisterCreatedObjectUndo(newGO, "Create Node");
            return newNode;
        }

        private void OnSceneGUI()
        {
            if (Selection.count != 1)
                return;
            

            int controlId = GUIUtility.GetControlID(node.GetInstanceID(), FocusType.Passive);
            float handleSize = node.editorOnlyParentGraphCached != null ? node.editorOnlyParentGraphCached.nodeSize : .25f;
            var fmh_105_76_638673685992693890 = Quaternion.identity; dragPosition = Handles.FreeMoveHandle(controlId, dragPosition, handleSize, Vector3.zero, Handles.CircleHandleCap);
            if (GUIUtility.hotControl != controlId)
            {
                dragPosition = node.transform.position;

                if (lastDraggedToNode != null)
                {
                    if (lastDraggedToNode != null && lastDraggedToNode != node)
                    {
                        var newEdge = SceneGraphEdgeEditor.CreateEdge(node, lastDraggedToNode);
                        
                    }
                }
                else if (didDrag && Event.current.rawType == EventType.Used)
                {
                    var newNode = CreateNode(node.transform.parent, lastDraggedToPosition);
                    SceneGraphEdgeEditor.CreateEdge(node, newNode);
                    Selection.activeObject = newNode.gameObject;
                }

                lastDraggedToNode = null;
                didDrag = false;
            }
            else
            {
                didDrag = true;
                Handles.color = ConnectionColor;
               
                lastDraggedToPosition = dragPosition;
                
                // Look for a node under the mouse
                lastDraggedToNode = null;
                Rect rect = new Rect(Event.current.mousePosition - PickRectSize / 2, PickRectSize);
                var gos = HandleUtility.PickRectObjects(rect);
                foreach (var go in gos)
                {
                    if (go.TryGetComponent<LineGraphNode>(out var pickedNode) && pickedNode != node)
                    {
                        lastDraggedToNode = pickedNode;
                        Handles.DrawWireDisc(pickedNode.transform.position, Vector3.forward, handleSize);
                        break;
                    }
                }

                if (lastDraggedToNode == null)
                {
                    Handles.DrawDottedLine(node.transform.position, dragPosition, 1);
                }
                else
                {
                    Handles.DrawLine(node.transform.position, dragPosition, 1);
                }
            }
        }
    }
#endif
}