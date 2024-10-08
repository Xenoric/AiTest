using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PathBerserker2d.Demo
{
    /// <summary>
    /// Displays an agents current path using a line renderer
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class AgentPathRenderer : MonoBehaviour
    {
        [SerializeField]
        NavAgent agent = null;

        LineRenderer lineRend;

        void Awake()
        {
            lineRend = GetComponent<LineRenderer>();
        }

        void Update()
        {
            var points = agent.PathPoints().Select(v => (Vector3)v).ToArray();
            lineRend.positionCount = points.Length;
            lineRend.SetPositions(points);
        }
    }
}
