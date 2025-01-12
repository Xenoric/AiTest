using AnyPath.Graphs.SquareGrid;
using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;

namespace AnyPath.Examples
{
    /// <summary>
    /// This shows a use case for processing during the A* algorithm. We can discourage paths to get close to a certain
    /// location by increasing the cost of that cells. This struct is passed on to each request and can have different values
    /// per request. Thereby providing a way to alter the results without creating an entirely new grid.
    /// </summary>
    public struct AvoidModifier : IEdgeMod<SquareGridCell>
    {
        public int2 center;
        public float severity;
        public readonly float maxCost;

        public AvoidModifier(int2 pos, float severity, float maxCost)
        {
            this.center = pos;
            this.severity = severity;
            
            // we dont want the cost to exceed the width/height of the grid itself, as A* will then
            // go all out of bounds because it always thinks going further away from our avoid spot
            // gives a shorter path
            this.maxCost = maxCost;
        }

        public bool ModifyCost(in SquareGridCell @from, in SquareGridCell to, ref float cost)
        {
            // The closer the location is to our avoid position, the higher the cost
            cost = math.min(maxCost, cost + (severity / (1 + math.distancesq(to.Position, center))));
            return true;
        }

        public void ModifyEdgeBuffer(in SquareGridCell from, ref NativeList<Edge<SquareGridCell>> edgeBuffer) { }
    }
}