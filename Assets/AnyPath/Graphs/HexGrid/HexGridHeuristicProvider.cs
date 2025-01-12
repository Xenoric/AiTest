using AnyPath.Native;
using Unity.Mathematics;
using UnityEngine.Internal;
using static Unity.Mathematics.math;

namespace AnyPath.Graphs.HexGrid
{
    public struct HexGridHeuristicProvider : IHeuristicProvider<HexGridCell>
    {
        /// <summary>
        /// The grid type to base the heuristic value on. This should match the type set on the <see cref="HexGrid"/> itself.
        /// Defaults to UnityTileMap (0).
        /// </summary>
        public HexGridType gridType;

        private HexGridCell goal;

        /// <summary>
        /// Construct a heuristic provider for a hexagonal grid.
        /// </summary>
        /// <param name="gridType">Cell layout of the grid. This value should match the one defined on the <see cref="HexGrid"/></param>
        public HexGridHeuristicProvider(HexGridType gridType)
        {
            this.gridType = gridType;
            this.goal = default;
        }

        public void SetGoal(HexGridCell goal)
        {
            this.goal = goal;
        }
        
        /// <summary>
        /// Returns the distance between two hex locations on this grid
        /// </summary>
        public float Heuristic(HexGridCell a)
        {
            float3 cubeA;
            float3 cubeB;
            switch (gridType)
            {
                case HexGridType.OddR:
                    cubeA = OddRtoCube(a);
                    cubeB = OddRtoCube(goal);
                    break;
                case HexGridType.EvenR:
                    cubeA = EvenRtoCube(a);
                    cubeB = EvenRtoCube(goal);
                    break;
                case HexGridType.OddQ:
                    cubeA = OddQtoCube(a);
                    cubeB = OddQtoCube(goal);
                    break;
                default: //case HexGridType.EvenQ:
                    cubeA = EvenQtoCube(a);
                    cubeB = EvenQtoCube(goal);
                    break;
            }

            //  max(|dy|, |dx| + floor(|dy|/2) + penalty); penalty = ( (even(y1) && odd(y2) && (x1 < x2)) || (even(y2) && odd(y1) && (x2 < x1)) ) ? 1 : 0
                
            // manhattan distance halved on cube:
            return dot(abs(cubeA - cubeB), float3(1.0)) / 2;
        }
        
        /**
        * Conversion functions
        */
        
        [ExcludeFromDocs]
        public static float3 OddRtoCube(int2 hex)
        {
            float x = hex.x - (hex.y - (hex.y & 1)) / 2;
            float z = hex.y;
            float y = -x - z;
            return new float3(x,y,z);
        }
            
        [ExcludeFromDocs]
        public static float3 EvenRtoCube(int2 hex)
        {
            float x = hex.x - (hex.y + (hex.y & 1)) / 2;
            float z = hex.y;
            float y = -x - z;
            return new float3(x,y,z);
        }
            
        [ExcludeFromDocs]
        public static float3 OddQtoCube(int2 hex)
        {
            float x = hex.x;
            float z = hex.y - (hex.x - (hex.x & 1)) / 2;
            float y = -x - z;
            return new float3(x,y,z);
        }
            
        [ExcludeFromDocs]
        public static float3 EvenQtoCube(int2 hex)
        {
            float x = hex.x;
            float z = hex.y - (hex.x + (hex.x & 1)) / 2;
            float y = -x - z;
            return new float3(x,y,z);
        }
    }
}