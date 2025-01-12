using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Internal;

namespace AnyPath.Graphs.PlatformerGraph
{
    /// <summary>
    /// Makes it extremely easy to 'draw' a platformer graph. Just add lines/edges and close enough
    /// vertices will automatically be joined together.
    /// </summary>
    /// <example>
    /// <code>
    /// void Example()
    /// {
    ///     // create a drawer which merges vertices that are closer than .1f
    ///     var drawer = new PlatformerGraphDrawer(.1f);
    ///         
    ///     // draw a square
    ///     drawer.AddUndirected(new float2(0, 0), new float2(1, 0));
    ///     drawer.AddUndirected(new float2(1, 0), new float2(1, 1));
    ///     drawer.AddUndirected(new float2(1, 1), new float2(0, 1));
    ///     drawer.AddUndirected(new float2(0, 1), new float2(0, 0));
    /// 
    ///     // create our graph
    ///     var graph = new PlatformerGraph(Allocator.Persistent);
    ///         
    ///     // populate it with our square
    ///     graph.Populate(drawer.Vertices, drawer.UndirectedEdges);
    /// }
    /// </code>
    /// </example>
    public class PlatformerGraphDrawer
    {
        private Dictionary<int2, int> buckets = new Dictionary<int2, int>();
        private List<float2> vertices = new List<float2>();
        private List<PlatformerGraph.Edge> directedEdges = new List<PlatformerGraph.Edge>();
        private List<PlatformerGraph.Edge> undirectedEdges = new List<PlatformerGraph.Edge>();
        private readonly float thresholdMultiplier;

        /// <summary>
        /// Construct a platformer graph drawer
        /// </summary>
        /// <param name="weldThreshold">Approx maximum distance vertices can have in order to be joined/welded together</param>
        public PlatformerGraphDrawer(float weldThreshold)
        {
            this.thresholdMultiplier = 1 / weldThreshold;
        }

        /// <summary>
        /// Draw a directed edge with some additional traversal cost and flags associated with it
        /// </summary>
        public void AddDirected(float2 from, float2 to, float enterCost, int flags = 0)
        {
            PlatformerGraphWelder.ContinuousWeld(from, to, enterCost, flags, vertices, directedEdges, buckets, thresholdMultiplier);
        }

        
        /// <summary>
        /// Draw an undirected edge with some additional traversal cost and flags associated with it
        /// </summary>
        public void AddUndirected(float2 from, float2 to, float enterCost, int flags = 0)
        {
            PlatformerGraphWelder.ContinuousWeld(from, to, enterCost, flags, vertices, undirectedEdges, buckets, thresholdMultiplier);
        }
        
        /// <summary>
        /// Draw a directed edge
        /// </summary>
        public void AddDirected(float2 from, float2 to) => AddDirected(from, to, 0, 0);
        
        /// <summary>
        /// Draw an  undirected edge
        /// </summary>
        public void AddUndirected(float2 from, float2 to) => AddUndirected(from, to, 0, 0);
        
        [ExcludeFromDocs]
        public void AddDirected(Vector2 from, Vector2 to, float enterCost, int flags) => 
            AddDirected(new float2(from.x, from.y), new float2(to.x, to.y), enterCost, flags);

        [ExcludeFromDocs]
        public void AddDirected(Vector2 from, Vector2 to) => 
            AddDirected(new float2(from.x, from.y), new float2(to.x, to.y));
        
        [ExcludeFromDocs]
        public void AddUndirected(Vector2 from, Vector2 to, float enterCost, int flags) => 
            AddUndirected(new float2(from.x, from.y), new float2(to.x, to.y), enterCost, flags);

        [ExcludeFromDocs]
        public void AddUndirected(Vector2 from, Vector2 to) => 
            AddUndirected(new float2(from.x, from.y), new float2(to.x, to.y));
        
        /// <summary>
        /// The welded vertices, use to populate the graph
        /// </summary>
        public IReadOnlyList<float2> Vertices => vertices;
        
        /// <summary>
        /// All edges that have been added as undirected, use to populate the graph
        /// </summary>
        public IReadOnlyList<PlatformerGraph.Edge> UndirectedEdges => undirectedEdges;
        
        /// <summary>
        /// All edges that have been added as directed edges, use to populate the graph
        /// </summary>
        public IReadOnlyList<PlatformerGraph.Edge> DirectedEdges => directedEdges;

        /*
        void Example()
        {
            // create a drawer which merges vertices that are closer than .1f
            var drawer = new PlatformerGraphDrawer(.1f);
            
            // draw a square
            drawer.AddUndirected(new float2(0, 0), new float2(1, 0));
            drawer.AddUndirected(new float2(1, 0), new float2(1, 1));
            drawer.AddUndirected(new float2(1, 1), new float2(0, 1));
            drawer.AddUndirected(new float2(0, 1), new float2(0, 0));

            // create our graph
            var graph = new PlatformerGraph(Allocator.Persistent);
            
            // populate it with our square
            graph.Populate(drawer.Vertices, drawer.UndirectedEdges);
        }
        */
    }
}