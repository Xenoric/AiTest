using System;
using AnyPath.Graphs.Line;
using AnyPath.Graphs.Line.SceneGraph;
using AnyPath.Managed;
using AnyPath.Managed.Finders;
using AnyPath.Managed.Results;
using AnyPath.Native;
using Unity.Collections;
using UnityEngine;

namespace AnyPath.Examples.LineGraphExample
{
    // Finder we're using, code generated with the AnyPath code generator
    public class LineGraphPathFinder : PathFinder<LineGraph, LineGraphLocation, LineGraphHeuristic, 
        NoEdgeMod<LineGraphLocation>, LineGraphProcessor, LineGraphLocation> { }

    /// <summary>
    /// This example demonstrates the use of the Line graph
    /// </summary>
    public class LineGraphExample : MonoBehaviour
    {
        public LineGraph graph;
        public LineSceneGraph sceneGraph;
        
        // line renderer hooks into this to display the path nicer
        public event Action<Path<LineGraphLocation>> PathFound; 

        private Path<LineGraphLocation> currentPath;
        private LineGraphLocation startLocation;
        private LineGraphPathFinder finder;
        private bool hasStart;

        private void Start()
        {
            // We construct our runtime graph from the data stored on the Platformer Scene Graph
            graph = new LineGraph(
                Allocator.Persistent, directedEdgesQueryable: true);
            
            // use the scene graph as the source
            graph.Populate(
                sceneGraph.vertices, 
                sceneGraph.undirectedEdges, 
                sceneGraph.directedEdges);

            // create our re-usable finder
            finder = new LineGraphPathFinder() { ReuseResult = true };
            Debug.Log("Turn on game view gizmo's for this demo!");
        }
        
        private void OnDestroy()
        {
            graph.DisposeGraph();
        }
        
        private void Update()
        {
            // First obtain the position of the mouse on the plane beneath the graph
            if (!GetLocationOnPlane(out Vector3 mousePositionOnPlane))
                return;
            
            // From that location, we obtain the closest location on the graph itself
            if (!graph.ClosestLocation(mousePositionOnPlane, float.PositiveInfinity, 
                
                // We can use this callback to exclude certain locations. Here, we check if here are no colliders
                // obstructing the line of sight between our mouse position and the edge/line on the graph.
                // this can be useful so that agents don't navigate through a wall to a nearest edge for instance
                (origin, location) => !Physics.Linecast(origin, location.Position), out var mouseLocation))
            {
                return;
            }
            
            // Set starting position of our pathfinding if the mouse is held
            if (Input.GetMouseButtonDown(0))
            {
                hasStart = true;
                startLocation = mouseLocation;
            }
            
            if (hasStart)
                FindPath(startLocation, mouseLocation);
        }

        private void FindPath(LineGraphLocation from, LineGraphLocation to)
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
            else
            {
                Debug.Log("No path");
            }
        }
        
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                DrawClosestEdgeGizmo();
        }
        
        /// <summary>
        /// Raycast from the camera down to the plane, then from there we get a position from which we
        /// obtain the nearest edge in the graph
        /// </summary>
        bool GetLocationOnPlane(out Vector3 position)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            Physics.Raycast(ray, out var hitInfo);
            if (hitInfo.collider == null)
            {
                position = default;
                return false;
            }

            position = hitInfo.point;
            return true;
        }
        
        private void DrawClosestEdgeGizmo()
        {
            // Raycast down from the mouse position
            if (!GetLocationOnPlane(out var mousePos))
                return;
            
            // True 'closest' location to the mouse:
            if (graph.ClosestLocation(mousePos, float.PositiveInfinity, (origin, location) => !Physics.Linecast(origin, location.Position), out var loc2))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(mousePos, loc2.Position);
            }
        }
        
        /*
          /// <summary>
          /// A little example on how to easily construct a graph using the <see cref="PlatformerGraphDrawer"/>
          /// </summary>
        
          void DrawerExample()
          {
              var drawer = new LineGraphDrawer(.1f);
              
              // a square
              drawer.AddUndirected(new float3(0, 0, 0), new float3(100, 0, 0));
              drawer.AddUndirected(new float3(100, 0, 0), new float3(100, 100, 0));
              drawer.AddUndirected(new float3(100, 100, 0), new float3(0, 100, 0));
              drawer.AddUndirected(new float3(0, 100, 0), new float3(0, 0,0));
              
              // anti-clockwise only square next to it
              drawer.AddDirected(new float3(100, 0, 0) + new float3(0, 0,0), new float3(100, 0,0) + new float3(100, 0,0));
              drawer.AddDirected(new float3(100, 0, 0) + new float3(100, 0,0), new float3(100, 0,0) + new float3(100, 100,0));
              drawer.AddDirected(new float3(100, 0, 0) + new float3(100, 100,0), new float3(100, 0,0) + new float3(0, 100,0));
              drawer.AddDirected(new float3(100, 0, 0) + new float3(0, 100,0), new float3(100, 0,0) + new float3(0, 0,0));

              graph = new Graphs.Line.LineGraph(Allocator.Persistent, directedEdgesQueryable: true);
              graph.Populate(drawer.Vertices, drawer.UndirectedEdges, drawer.DirectedEdges);
          }
        */
    }
}