using AnyPath.Graphs.Extra;
using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;

namespace AnyPath.Graphs.PlatformerGraph
{
    /// <summary>
    /// Processes the raw A* result for a request on the platformer graph.
    /// This processing ensures that the resulting segments all point in the correct direction.
    /// </summary>
    /// <remarks>The resulting path type is of the same type as the locations themselves, as this gives better performance and (almost)
    /// all of the data contained in the location might still be relevant when navigating.</remarks>
    public struct PlatformerGraphProcessor : IPathProcessor<PlatformerGraphLocation, PlatformerGraphLocation>
    {
        private static bool Equals2(float2 a, float2 b)
        {
            return math.all(a == b);
        }

        private static void Flip(ref Line2D line)
        {
            var tmp = line.a;
            line.a = line.b;
            line.b = tmp;
        }
        
        public void ProcessPath(
            PlatformerGraphLocation queryStart,
            PlatformerGraphLocation queryGoal, NativeList<PlatformerGraphLocation> pathNodes, NativeList<PlatformerGraphLocation> appendTo)
        {
            if (pathNodes.Length == 0)
            {
                // start location was on the same edge as the goal, just create a straight line
                appendTo.Add(new PlatformerGraphLocation(
                    queryStart.edgeIndex,
                    queryStart.edgeId,
                    queryStart.Flags, 
                    new Line2D(queryStart.Position, queryGoal.Position), 0));
                return;
            }
            
            // derive the direction for the query's start
            var next = pathNodes[0];
            if (Equals2(queryStart.line.a, next.line.a) || Equals2(queryStart.line.a, next.line.b))
            {
                Flip(ref queryStart.line);
                queryStart.PositionT = 1 - queryStart.PositionT;
            }
            
            queryStart.line.a = queryStart.Position;
            
            // add the (possibly flipped) starting edge
            appendTo.Add(queryStart);
           
            // copy the path
            appendTo.AddRange(pathNodes.AsArray());
            
            // replace position on the goal edge in the path and redefine the line to match the exact query goal
            var prev = appendTo[appendTo.Length - 2]; // note that we can do -2 because we manually insert the starting edge
            if (Equals2(queryGoal.line.b, prev.line.a) || Equals2(queryGoal.line.b, prev.line.b))
            {
                Flip(ref queryGoal.line);
                queryGoal.PositionT = 1 - queryGoal.PositionT;
            }
            
            queryGoal.line.b = queryGoal.Position;
            appendTo[appendTo.Length - 1] = queryGoal;
        }

        public bool InsertQueryStart => false;
    }
}