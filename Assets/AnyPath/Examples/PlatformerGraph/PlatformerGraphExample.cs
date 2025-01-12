using System;
using AnyPath.Graphs.PlatformerGraph;
using AnyPath.Graphs.PlatformerGraph.SceneGraph;
using AnyPath.Managed;
using AnyPath.Managed.Finders;
using AnyPath.Managed.Results;
using AnyPath.Native;
using AnyPath.Native.Util;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AnyPath.Examples.Platformer
{
    // Finder we're using, code generated with the AnyPath code generator
    public class PlatformerGraphPathFinder : PathFinder<PlatformerGraph, PlatformerGraphLocation, PlatformerGraphHeuristic, NoEdgeMod<PlatformerGraphLocation>, PlatformerGraphProcessor, PlatformerGraphLocation> { }
    
    /// <summary>
    /// This example demonstrates the use of the Platformer graph
    /// </summary>
    public class PlatformerGraphExample : MonoBehaviour
    {
        public PlatformerSceneGraph sceneGraph;
        public PlatformerGraph graph;
        
        // line renderer hooks into this to display the path nicer
        public event Action<Path<PlatformerGraphLocation>> PathFound; 
        
        [SerializeField] private Color raycastColor;
        
        private Path<PlatformerGraphLocation> currentPath;
        private PlatformerGraphLocation startLocation;
        private PlatformerGraphPathFinder finder;
        private bool hasStart;

        private void Start()
        {
            // We construct our runtime graph from the data stored on the Platformer Scene Graph
            graph = new PlatformerGraph(
                Allocator.Persistent, directedEdgesRaycastable: true);
         
            // use the scene graph as the source
            graph.Populate(
                sceneGraph.vertices, 
                sceneGraph.undirectedEdges, 
                sceneGraph.directedEdges);
            
            // create our re-usable finder
            finder = new PlatformerGraphPathFinder();
            
            Debug.Log("Turn on game view gizmo's for this demo!");
        }
        

        private void OnDestroy()
        {
            graph.DisposeGraph();
        }

        private void Update()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Raycast down from the mouse position
            var ray = new Ray2D(mousePos, Vector2.down);
            if (!graph.Raycast(ray, out var mouseLocation))
                return;
            
            if (Input.GetMouseButtonDown(0))
            {
                hasStart = true;
                startLocation = mouseLocation;
            }
            
            if (hasStart)
                FindPath(startLocation, mouseLocation);
        }

        private void FindPath(PlatformerGraphLocation from, PlatformerGraphLocation to)
        {
            // clear our finder before usage
            finder.Clear();
            
            // Set start and goal location
            finder.Stops.Add(from);
            finder.Stops.Add(to);

            // Assign the graph
            finder.Graph = graph;
            finder.Run();
            
            if (finder.Result.HasPath)
            {
                currentPath = finder.Result;
                PathFound?.Invoke(currentPath);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                DrawClosestEdgeGizmo();
        }

        private void DrawClosestEdgeGizmo()
        {
            // Raycast down from the mouse position
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var ray = new Ray2D(mousePos, Vector2.down);
            
            
            if (graph.Raycast(ray, out var loc))
            {
                Gizmos.color = raycastColor;
                Gizmos.DrawLine(ray.origin, loc.Position.ToVec3());
            }

            // True 'closest' location to the mouse:
            if (graph.ClosestLocation(mousePos, float.PositiveInfinity, out var loc2))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(ray.origin, loc2.Position.ToVec3());
            }
        }

        /// <summary>
        /// A little example on how to easily construct a graph using the <see cref="PlatformerGraphDrawer"/>
        /// </summary>
        void DrawerExample()
        {
            var drawer = new PlatformerGraphDrawer(.1f);
            
            // a square
            drawer.AddUndirected(new float2(0, 0), new float2(100, 0));
            drawer.AddUndirected(new float2(100, 0), new float2(100, 100));
            drawer.AddUndirected(new float2(100, 100), new float2(0, 100));
            drawer.AddUndirected(new float2(0, 100), new float2(0, 0));
            
            // anti-clockwise only square next to it
            drawer.AddDirected(new float2(100, 0) + new float2(0, 0), new float2(100, 0) + new float2(100, 0));
            drawer.AddDirected(new float2(100, 0) + new float2(100, 0), new float2(100, 0) + new float2(100, 100));
            drawer.AddDirected(new float2(100, 0) + new float2(100, 100), new float2(100, 0) + new float2(0, 100));
            drawer.AddDirected(new float2(100, 0) + new float2(0, 100), new float2(100, 0) + new float2(0, 0));

            graph = new PlatformerGraph(Allocator.Persistent, directedEdgesRaycastable: true);
            graph.Populate(drawer.Vertices, drawer.UndirectedEdges, drawer.DirectedEdges);
        }
    }
}