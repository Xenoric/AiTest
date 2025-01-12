using AnyPath.Graphs.Line;
using AnyPath.Graphs.PlatformerGraph;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Internal;

namespace AnyPath.Examples
{
    /// <summary>
    /// Example code that shows how to construct the PlatformarGraph (2D) and LineGraph (3D) purely in code.
    /// </summary>
    [ExcludeFromDocs]
    public class LineGraphAndPlatformerGraphBuilding
    {
        /// <summary>
        /// Build a 3D graph by first defining vertices with a custom Id, and then linking them.
        /// </summary>
        void Example()
        {
            LineGraphBuilder builder = new LineGraphBuilder();
            
            // make a square, the first parameter is the ID of the vertex (can be any number)
            builder.SetVertex(0, new float3(0,0,0));
            builder.SetVertex(1, new float3(1,0,0));
            builder.SetVertex(2, new float3(1,0,1));
            builder.SetVertex(3, new float3(0,0,1));
            
            // Link the vertices using the Id's.
            builder.LinkDirected(0, 1);
            builder.LinkDirected(1, 2);
            builder.LinkDirected(2, 3);
            builder.LinkDirected(3, 0);

            // Diagonal both ways, with an additional cost value making it harder to traverse
            builder.LinkUndirected(0, 2, enterCost: 5);

            // construct the graph used for pathfinding
            builder.GetData(out var verts, out var undirectedIndices, out var directedIndices);
            var graph = new LineGraph(verts, undirectedIndices, directedIndices, Allocator.Persistent);
        }

        /// <summary>
        /// Build a 3D graph by adding edges, edges that share endpoints will be automatically connected.
        /// </summary>
        void Example2()
        {
            // the weld threshold defines how close vertices should be in order to be connected
            LineGraphDrawer drawer = new LineGraphDrawer(weldThreshold: .1f);
            
            // 2 way streets, they will be connected because they share endpoints
            drawer.AddUndirected(new float3(0,0,0), new float3(0,1,0));
            drawer.AddUndirected(new float3(0,1,0), new float3(1,1,0));
            drawer.AddUndirected(new float3(1,1,0), new float3(0,1,0));
            drawer.AddUndirected(new float3(0,1,0), new float3(0,0,0));

            // 1 way street diagonal:
            drawer.AddDirected(new float3(0,0,0), new float3(1,1,0), enterCost: 1);
            
            var graph = new LineGraph(
                drawer.Vertices, 
                drawer.UndirectedEdges, 
                drawer.DirectedEdges, Allocator.Persistent);
        }
        
        /// <summary>
        /// Build a 2D platformer graph by adding edges, edges that share endpoints will be automatically connected.
        /// </summary>
        void Example3()
        {
            // the weld threshold defines how close vertices should be in order to be connected
            PlatformerGraphDrawer drawer = new PlatformerGraphDrawer(weldThreshold: .1f);
            
            // 2 way streets, they will be connected because they share endpoints
            drawer.AddUndirected(new float2(0,0), new float2(0,1));
            drawer.AddUndirected(new float2(0,1), new float2(1,1));
            drawer.AddUndirected(new float2(1,1), new float2(0,1));
            drawer.AddUndirected(new float2(0,1), new float2(0,0));

            // 1 way street diagonal:
            drawer.AddDirected(new float2(0,0), new float2(1,1), enterCost: 1);
            
            var graph = new PlatformerGraph(
                drawer.Vertices, 
                drawer.UndirectedEdges, 
                drawer.DirectedEdges, Allocator.Persistent);
        }
    }
}