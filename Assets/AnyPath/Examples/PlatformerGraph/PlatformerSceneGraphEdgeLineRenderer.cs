using System.Collections.Generic;
using AnyPath.Graphs.PlatformerGraph.SceneGraph;
using AnyPath.Native.Util;
using UnityEngine;

namespace AnyPath.Examples.Platformer
{
    /// <summary>
    /// Uses a line renderer to draw an edge, purely for run time visualisation purposes
    /// </summary>
    [RequireComponent(typeof(PlatformerSceneGraphEdge), typeof(LineRenderer))]
    [ExecuteAlways]
    public class PlatformerSceneGraphEdgeLineRenderer : MonoBehaviour
    {
        public float lineWidth = .4f;
        public float arrowSize = .8f;
        
        public LineRenderer lineRenderer;
        public PlatformerSceneGraphEdge edge;
        private PlatformerSceneGraph sceneGraph;
        private Gradient gradient;
        private List<Color> gradientColors = new List<Color>();
        private GradientColorKey[] colorKeys;

        private void Update()
        {
            if (!Application.isPlaying)
                Refresh();
        }

        public void SetMaterial(Material mat)
        {
            Fetch();
            lineRenderer.sharedMaterial = mat;
        }
        
        void Fetch()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                    lineRenderer = this.gameObject.AddComponent<LineRenderer>();
            }

            if (edge == null)
            {
                edge = GetComponent<PlatformerSceneGraphEdge>();
                if (edge == null)
                    edge = this.gameObject.AddComponent<PlatformerSceneGraphEdge>();
            }

            if (sceneGraph == null)
                sceneGraph = GetComponentInParent<PlatformerSceneGraph>();
        }

        private void OnValidate()
        {
            Refresh();
        }

        void Refresh()
        {
            Fetch();
            if (!edge.IsValid())
            {
                lineRenderer.positionCount = 0;
                return;
            }

            lineRenderer.startWidth = lineRenderer.endWidth = 1;
            //lineRenderer.numCapVertices = 4;
            lineRenderer.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
            lineRenderer.widthMultiplier = lineWidth;

            if (edge.directed)
            {
               
                Vector3 center = .5f * (edge.a.GetPosition().ToVec3() + edge.b.GetPosition().ToVec3());
                Vector3 dir = Vector3.Normalize((edge.b.GetPosition() - edge.a.GetPosition()).ToVec3());
                Vector3 orthoDir = new Vector3(dir.y, -dir.x);
                
                Vector3 arrowA = center - dir * arrowSize;
                Vector3 arrowB = arrowA + orthoDir * arrowSize;
                Vector3 arrowC = center + dir * arrowSize;
                Vector3 arrowD = arrowA - orthoDir * arrowSize;
                Vector3 arrowE = arrowA;
                
                lineRenderer.positionCount = 7;
                lineRenderer.SetPositions(new Vector3[]
                {
                    edge.a.GetPosition().ToVec3(),
                    arrowA, arrowB, arrowC, arrowD, arrowE,
                    edge.b.GetPosition().ToVec3()
                });
            }
            else
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPositions(new Vector3[]
                {
                    edge.a.GetPosition().ToVec3(),
                    edge.b.GetPosition().ToVec3()
                });
            }


            if (gradient == null)
                gradient = new Gradient();
            
            PlatformerSceneGraphEdge.GetFlagColors(sceneGraph, edge.flags, gradientColors);
            if (colorKeys == null || colorKeys.Length != gradientColors.Count)
                colorKeys = new GradientColorKey[gradientColors.Count];

            for (int i = 0; i < colorKeys.Length; i++)
                colorKeys[i] = new GradientColorKey(gradientColors[i], (float) i / colorKeys.Length);

            gradient.colorKeys = colorKeys;
            lineRenderer.colorGradient = gradient;
        }
    }
}