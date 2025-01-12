using System;
using System.Collections.Generic;
using System.Diagnostics;
using AnyPath.Graphs.NavMesh;
using AnyPath.Managed;
using AnyPath.Managed.Finders;
using AnyPath.Managed.Results;
using AnyPath.Native;
using Unity.Collections;
using Unity.Collections.NotBurstCompatible;
using UnityEngine;
using UnityEngine.AI;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace AnyPath.Examples
{
    // Generated using AnyPath code generator
    // this finder has the NavMeshGraphCorners3D processor which straighens the path through the triangles
    public class NavMeshGraphPathFinder : PathFinder<NavMeshGraph, NavMeshGraphLocation, NavMeshGraphHeuristic, NoEdgeMod<NavMeshGraphLocation>, NavMeshGraphCorners3D, CornerAndNormal> { }

    /// <summary>
    /// This example demonstrates the usage of the built in NavMesh
    /// </summary>
    public class NavMeshGraphExample : MonoBehaviour
    {
        public MeshFilter meshFilter;
        public LineRenderer lineRenderer;

        public NavMeshGraph graph;
        public float lineWidth = .1f;
        public bool randomPaths;
        public float randomPathInterval = .25f;

        private Path<CornerAndNormal> currentPath;
        private NavMeshGraphLocation startLocation;
        private NavMeshGraphPathFinder finder;

        private bool hasStart;
        private float randomPathAcc;

        public static event Action<NavMeshGraphLocation, NavMeshGraphLocation> Travel;
        public static event Action<NavMeshGraphLocation, Path<CornerAndNormal>> RandomPath;

        private void Awake()
        {
            lineRenderer.widthCurve = AnimationCurve.Linear(0, 1, 1, 1);
            lineRenderer.widthMultiplier = lineWidth;
        }

        private void Start()
        {
            // Uncomment one of the following start methods

            StartDefault();
            //StartPopulateWithJob(); // <- faster but gives skewed results the first time if the job is not compiled yet
            //StartNavMesh(); // <- this only works when there is a Unity NavMesh in the scene!

            finder = new NavMeshGraphPathFinder();
            finder.Graph = graph; // assign our graph once

            Debug.Log("Turn on game view gizmos for this demo! Left click sets a starting position. Right click to make the sphere traverse the latest path.");
        }

        /// <summary>
        /// Demonstrates the most straightforward way to generate a NavMeshGraph from a regular mesh
        /// </summary>
        void StartDefault()
        {
            // NOTE: if your mesh is imported, make sure to check Read/Write enabled on the import settings
            var mesh = meshFilter.mesh;
            var verts = new List<Vector3>(mesh.vertices);
            var triangles = new List<int>(mesh.triangles);
            
            // A lot of 3D models don't have all vertices connected
            // this will "weld" duplicate vertices that are at the same position
            NavMeshWelder.Weld(verts, triangles, weldThreshold: .0001f);
            
            var sw = Stopwatch.StartNew();
            graph = new NavMeshGraph(verts, triangles, meshFilter.transform.localToWorldMatrix, Allocator.Persistent);

            Debug.Log("Creation in " + sw.Elapsed.TotalMilliseconds);
        }
        
        /// <summary>
        /// Demonstrates how to initialize the navmesh by doing all of the heavy lifting using Unity's Job System
        /// This can be benificial for large meshes or when you need to update your mesh frequently. As most of the work
        /// can be performed on another thread.
        /// </summary>
        void StartPopulateWithJob()
        {
            // pre allocate our navmesh graph:
            graph = new NavMeshGraph(Allocator.Persistent, trianglesPerOctant: 32, maxOctreeDepth: 8);
           
            // Obtain the vertices and triangle indices from our mesh:
            var mesh = meshFilter.mesh;
            NativeList<Vector3> vertices = new NativeList<Vector3>(Allocator.TempJob);
            vertices.CopyFromNBC(mesh.vertices);
            var indices = new NativeArray<int>(mesh.triangles, Allocator.TempJob);

            var sw = Stopwatch.StartNew();
            
            // We'll do both the welding of vertices and navmesh populating using the Job system
            // schedule a job that welds the vertices together
            var weldJobHandle = NavMeshWelder.ScheduleWeld(vertices, indices);
            
            // schedule a job that generates the navmesh data
            // we pass in the weld JobHandle to the populate job, as we want to populate the mesh with our welded verts
            // notice that we must pass in the vertices as a deffered job array, because at the time of schedule the list is still empty
            var populateHandle = NavMeshPopulator.SchedulePopulate(graph, 
                vertices.AsDeferredJobArray(), 
                indices,
                meshFilter.transform.localToWorldMatrix, // we want our navmesh to be in worldspace, supplying this matrix of the mesh's transform will fix this
                weldJobHandle);

            // after our navmesh is completed, we can dispose of these temp containers
            vertices.Dispose(populateHandle);
            indices.Dispose(populateHandle);

            populateHandle.Complete(); // or use Schedule and monitor when the job is done, afterwards the graph is usable

            Debug.Log("Creation in " + sw.Elapsed.TotalMilliseconds);
        }
        
        /// <summary>
        /// An example of how to use Unity's built in NavMesh baking system with anypath. Fill the scene with a baked
        /// navmesh and try this method out
        /// </summary>
        private void StartNavMesh()
        {
            // this is an example of how you could plug in Unity's built in NavMesh into AnyPath
            var triangulation = NavMesh.CalculateTriangulation();
            if (triangulation.vertices.Length == 0)
            {
                Debug.LogError("No navmesh found!");
                Destroy(this);
                return;
            }
            
            var verts = new List<Vector3>(triangulation.vertices);
            var triangles = new List<int>(triangulation.indices);
           
            
            // Usually Unity's NavMesh needs to be welded for usage with anypath. This will connect close enough vertices together
            NavMeshWelder.Weld(verts, triangles, .001f);

            graph = new NavMeshGraph(verts, triangles, Matrix4x4.identity, Allocator.Persistent);
            finder = new NavMeshGraphPathFinder();
            finder.Graph = graph; // assign our graph once
        }

        private void OnDestroy()
        {
            graph.DisposeGraph();
        }

        private void UpdateRandomPaths()
        {
            randomPathAcc += Time.deltaTime;
            if (randomPathAcc < randomPathInterval)
                return;
            randomPathAcc = 0;

            int tri1 = Random.Range(0, graph.TriangleCount);
            int tri2 = Random.Range(0, graph.TriangleCount);
            
            startLocation = graph.LocationFromTriangleIndex(tri1);
            var endLocation = graph.LocationFromTriangleIndex(tri2);
            hasStart = true;
            
            FindPath(startLocation, endLocation);
            if (currentPath != null)
                RandomPath?.Invoke(startLocation, currentPath);
        }

        private void Update()
        {
            if (randomPaths)
            {
                UpdateRandomPaths();
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // Perform a raycast on the navmesh
            bool raycastResult = graph.Raycast(ray, out var mouseLocation);
            if (Input.GetMouseButtonDown(0))
            {
                if (raycastResult)
                {
                    hasStart = true;
                    startLocation = mouseLocation;
                }
                else
                {
                    hasStart = false;
                }
            }


            if (hasStart)
            {
                // only find a new path when the mouse was on the mesh
                if (raycastResult)
                    FindPath(startLocation, mouseLocation);
              
                if (Input.GetMouseButtonDown(1))
                {
                    Travel?.Invoke(startLocation, mouseLocation);
                }
                
                if (currentPath != null)
                {
                    UpdatePathLines();
                }
            }
        }
        
        
        private void FindPath(NavMeshGraphLocation from, NavMeshGraphLocation to)
        {
            // Clear our finder before usage.
            // The graph has already been assigned in Start. We use ClearFinderFlags.KeepGraph
            // to indicate that we want to keep the graph as is.

            finder.Clear(ClearFinderFlags.KeepGraph);
            
            finder.PathProcessor = new NavMeshGraphCorners3D()
            {
                // this will merge corners that are really close together, causing the linerenderer to look ugly
                // on certain corners
                weldThreshold = 0.02f
            };

            finder.Stops.Add(from); // pathfinding start location
            finder.Stops.Add(to); // pathfinding end location

            finder.Run(); // run the query
            
            currentPath = finder.Result;
        }
        
        void UpdatePathLines()
        {
            if (currentPath == null || !currentPath.HasPath)
            {

                lineRenderer.positionCount = 0;
                return;
            }
            
            // Display our latest path via the line renderer
            float lw = lineRenderer.widthMultiplier;
            lineRenderer.positionCount = currentPath.Length + 1;
            
            //lineRenderer.SetPosition(0, startLocation.ExitPosition + .5f * lw * startLocation.Normal);
            lineRenderer.SetPosition(0, startLocation.ExitPosition);
            for (int i = 0; i < currentPath.Length; i++)
            {
                var seg = currentPath[i];
                lineRenderer.SetPosition(i + 1, seg.position + .5f * lw * seg.normal);
            }
        }
        
        /*
        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
            {
                //graph.DrawTrianglesGizmo();
                graph.DrawOctreeGizmo();
            }
        }
        */
    }
}