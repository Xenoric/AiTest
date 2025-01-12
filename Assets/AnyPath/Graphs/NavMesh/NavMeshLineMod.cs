using AnyPath.Graphs.Extra;
using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// <para>
    /// Encourages A* on a navmesh to follow a straight line from start to goal.
    /// </para>
    /// <para>
    /// Depending on the structure of your navmesh, this may be useful to get better looking straight paths. This is especially
    /// true for meshes that resemble a grid like structure.
    /// Because A* can only operate on the triangles itself, there may be many optimal paths to the destination that share the
    /// same cost (at the triangle level). If you use <see cref="SSFA"/>, then it may become apparent that the path that was chosen
    /// was not the most optimal after SSFA was performed.
    /// See this article for an in depth explanation:
    /// https://theory.stanford.edu/~amitp/GameProgramming/Heuristics.html#breaking-ties
    /// </para>
    /// <para>
    /// This edge modifier mitigates this by dynamically adding a cost penalty to triangles that lie further away
    /// from a straight line between the start and the goal location. Causing A* to always prefer paths that are as close to a
    /// straight line as possible at the triangle level.
    /// </para>
    /// <para>
    /// Note that when using this modifier, you must assign your pathfinding start and end locations manually before making the request!
    /// If you forget to do this, it will not work and produce unexpected paths. For this reason, this modifier is not suitable
    /// for pathfinding requests that have multiple stops in between.
    /// <code>
    /// finder.Stops.Add(from); // pathfinding start location
    /// finder.Stops.Add(to); // pathfinding end location
    /// finder.EdgeMod = new NavMeshStraightLineMod(from, to); // assign the straight line mod with our start+goal, obtained via a raycast
    /// finder.Run(); // run the query
    /// </code>
    /// </para>
    /// <remarks>
    /// You should test if this modifier is actually necessary for your use case, as in many cases and navmesh types, this offers
    /// no significant benefit and decreases performance slightly.
    /// </remarks>
    /// </summary>
    public struct NavMeshLineMod : IEdgeMod<NavMeshGraphLocation>
    {
        private Line3D startToEndLine;

        /// <summary>
        /// Construct the mod using a navmesh start and end location
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public NavMeshLineMod(NavMeshGraphLocation start, NavMeshGraphLocation end)
        {
            this.startToEndLine = new Line3D(start.ExitPosition, end.ExitPosition);
        }
        
        /// <summary>
        /// Construct to mod using an arbitrary line in space.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public NavMeshLineMod(float3 a, float3 b)
        {
            this.startToEndLine = new Line3D(a, b);
        }
        
        public bool ModifyCost(in NavMeshGraphLocation from, in NavMeshGraphLocation to, ref float cost)
        {
            // We add the distance from the triangle to the straight line from start to goal
            // to the cost of the triangle.
            
            // this encourages the algorithm to stay as close to the straight line as possible
            float3 closest = startToEndLine.GetClosestPoint(to.ExitPosition);
            cost += math.distance(closest, to.ExitPosition);
            return true;
        }

        public void ModifyEdgeBuffer(in NavMeshGraphLocation from, ref NativeList<Edge<NavMeshGraphLocation>> edgeBuffer) { }
    }
}