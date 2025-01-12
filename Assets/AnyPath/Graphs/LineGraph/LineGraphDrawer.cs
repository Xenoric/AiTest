using System.Collections.Generic;
using Unity.Mathematics;

namespace AnyPath.Graphs.Line
{
    /// <summary>
    /// Makes it extremely easy to 'draw' a road graph. Just add lines/edges and close enough
    /// vertices will automatically be joined together.
    /// </summary>
    /// <example>
    /// <code>
    /// void Example()
    /// {
    ///     // create a drawer which merges vertices that are closer than .1f
    ///     var drawer = new LineGraphDrawer(.1f);
    ///         
    ///     // draw a square
    ///     drawer.AddUndirected(new float3(0, 0, 0), new float3(1, 0, 0));
    ///     drawer.AddUndirected(new float3(1, 0, 0), new float3(1, 1, 0));
    ///     drawer.AddUndirected(new float3(1, 1, 0), new float3(0, 1, 0));
    ///     drawer.AddUndirected(new float3(0, 1, 0), new float3(0, 0, 0));
    /// 
    ///     // create our graph
    ///     var graph = new LineGraph(Allocator.Persistent);
    ///         
    ///     // populate it with our square
    ///     graph.Populate(drawer.Vertices, drawer.UndirectedEdges);
    /// }
    /// </code>
    /// </example>
    public class LineGraphDrawer
    {
        private Dictionary<int3, int> buckets = new Dictionary<int3, int>();
        private List<float3> vertices = new List<float3>();
        private List<LineGraph.Edge> directedEdges = new List<LineGraph.Edge>();
        private List<LineGraph.Edge> undirectedEdges = new List<LineGraph.Edge>();
        private readonly float thresholdMultiplier;

        /// <summary>
        /// Construct a LineGraphDrawer
        /// </summary>
        /// <param name="weldThreshold">Approx maximum distance vertices can have in order to be joined/welded together</param>
        public LineGraphDrawer(float weldThreshold)
        {
            this.thresholdMultiplier = 1 / weldThreshold;
        }

        /// <summary>
        /// Draw a directed edge with some additional traversal cost and flags associated with it
        /// </summary>
        public void AddDirected(float3 from, float3 to, float enterCost, int flags = 0, int id = 0)
        {
            LineGraphWelder.ContinuousWeld(from, to, enterCost, flags, id, vertices, directedEdges, buckets, thresholdMultiplier);
        }

        
        /// <summary>
        /// Draw an undirected edge with some additional traversal cost and flags associated with it
        /// </summary>
        public void AddUndirected(float3 from, float3 to, float enterCost, int flags = 0, int id = 0)
        {
            LineGraphWelder.ContinuousWeld(from, to, enterCost, flags, id, vertices, undirectedEdges, buckets, thresholdMultiplier);
        }
        
        /// <summary>
        /// Draw a directed edge
        /// </summary>
        public void AddDirected(float3 from, float3 to) => AddDirected(from, to, 0, 0);
        
        /// <summary>
        /// Draw an  undirected edge
        /// </summary>
        public void AddUndirected(float3 from, float3 to) => AddUndirected(from, to, 0, 0);
   
        
        /// <summary>
        /// The welded vertices, use to populate the graph
        /// </summary>
        public IReadOnlyList<float3> Vertices => vertices;
        
        /// <summary>
        /// All edges that have been added as undirected, use to populate the graph
        /// </summary>
        public IReadOnlyList<LineGraph.Edge> UndirectedEdges => undirectedEdges;
        
        /// <summary>
        /// All edges that have been added as directed edges, use to populate the graph
        /// </summary>
        public IReadOnlyList<LineGraph.Edge> DirectedEdges => directedEdges;
    }
}