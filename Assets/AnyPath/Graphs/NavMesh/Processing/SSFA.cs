using System;
using System.Runtime.CompilerServices;
using AnyPath.Native;
using Unity.Collections;
using Unity.Mathematics;

namespace AnyPath.Graphs.NavMesh
{
    /// <summary>
    /// Simple Stupid Funnel Algorithm implementations, used for 'straightening' a path on the NavMesh as much as possible.
    /// See http://digestingduck.blogspot.com/2010/03/simple-stupid-funnel-algorithm.html
    /// </summary>
    /// <remarks>
    /// It is possible to use this algorithm on other types of paths as well, as long as the segments implement <see cref="IUnrolledNavMeshGraphPortal"/>
    /// </remarks>
    public static class SSFA
    {
        /// <summary>
        /// Returns a target position to steer towards, based on a pre processed path by <see cref="NavMeshGraphUnroller"/>.
        /// This method should be called each update with the most recent position of the agent that traverses the path. The target position is dynamically
        /// calculated based on the current position of the agent. This allows for more fluid steering instead of following a fixed set of corner points.
        /// To get a steering direction, subtract the current position from the target position and normalize the result.
        /// </summary>
        /// <param name="portals">The processed path by <see cref="NavMeshGraphUnroller"/>. In a managed context, you can just pass in the <see cref="AnyPath.Managed.Results.Path{TSeg}"/> object.
        /// In ECS/Burst context, use <see cref="AnyPath.Native.NativeListWrapper{TSeg}"/>.</param>
        /// <param name="currentPosition">The current position of the agent traversing the path.</param>
        /// <param name="index">The index in the path. This value is automatically incremented based on the input position. Note that
        /// this value is never decreased, so if your agent deviates from the path too much, a new path may need to be calculated.</param>
        /// <typeparam name="T">The type of path, see <see cref="UnrolledNavMeshGraphPortal"/> and <see cref="AnyPath.Native.NativeListWrapper{TSeg}"/></typeparam>
        /// <returns>The ideal point to move towards, preserving the curvature of the mesh.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the index is out of range</exception>
        /// <remarks>
        /// <para>Be cautious with slow steering as that may cause the agent to move outside of the known path. Small deviations are usually OK, but
        /// if your agent ends up moving backwards in the path, the direction value may not be reliable anymore.
        /// </para>
        /// <para>
        /// This method works with full 3D curved worlds, but as such, the returned position is never further away than the next triangle in the path.
        /// Take care as to not move beyond the target position if your agent has a high velocity.
        /// If this is the case, move towards the target position and call this method again to get a new target position.
        /// </para>
        /// <para>
        /// It's recommended to combine this method of navigating with other forms of collision detection in your world.
        /// </para>
        /// </remarks>
        public static float3 GetSteerTargetPosition<T>(this T portals, float3 currentPosition, ref int index)
            where T : IPathSegments<UnrolledNavMeshGraphPortal>
        {
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index >= portals.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            #endif
            
            float2 portalApex = portals[index].TransformPoint(currentPosition);
            
            // dont take the last one because we +1 later and if it comes to that we just use the last position
            bool found = false;
            for (int i = index; i < portals.Length - 1; i++)
            {
                var portal = portals[i];
                float2 pLeft = portal.Left2D;
                float2 pRight = portal.Right2D;
                
                float d = ((pRight.x - pLeft.x) * (portalApex.y - pLeft.y) -
                           (pRight.y - pLeft.y) * (portalApex.x - pLeft.x));
                
                if (d < 0) // if we're still "behind" this portal, adjust the index
                {
                    index = i;
                    found = true;
                    break;
                }
            }

            // We're past all the portals, return the goal position
            if (!found)
            {
                index = portals.Length - 1;
                return portals[portals.Length - 1].Left3D;
            }

            int apexIndex = index;

            float2 portalLeft = portals[apexIndex + 1].Left2D;
            float2 portalRight = portals[apexIndex + 1].Right2D;

            for (int i = index + 2; i < portals.Length; i++)
            {
                var portal = portals[i];
                float2 pLeft = portal.Left2D;
                float2 pRight = portal.Right2D;
                
                if (TriArea2(portalApex, portalRight, pRight) <= 0f)
                {
                    if (Equals2(portalApex, portalRight) || TriArea2(portalApex, portalLeft, pRight) > 0.0f)
                    {
                        portalRight = pRight;
                    } 
                    else
                    {
                        
                        var skippedPortal = portals[index];
                        float3 intersectedPos = skippedPortal.Intersect(portalApex, portalLeft);

                        // If we're very close already to the target position, look further in the path to avoid
                        // coming to a standstill\
                        if (Equals3(intersectedPos, currentPosition) && index < portals.Length - 1)
                        {
                            index++;
                            return GetSteerTargetPosition(portals, currentPosition, ref index);
                        }
                        
                        return intersectedPos;
                    }
                }

                if (TriArea2(portalApex, portalLeft, pLeft) >= 0.0f)
                {
                    if (Equals2(portalApex, portalLeft) || TriArea2(portalApex, portalRight, pLeft) < 0.0f)
                    {
                        portalLeft = pLeft;
                    } 
                    else 
                    {
                        
                        var skippedPortal = portals[index];
                        float3 intersectedPos = skippedPortal.Intersect(portalApex, portalRight);
                        
                        // If we're very close already to the target position, look further in the path to avoid
                        // coming to a standstill
                        if (Equals3(intersectedPos, currentPosition) && index < portals.Length - 1)
                        {
                            index++;
                            return GetSteerTargetPosition(portals, currentPosition, ref index);
                        }

                        return intersectedPos;
                    }
                }
            }
            
            return portals[portals.Length-1].Left3D;
        }
        
        /// <summary>
        /// Converts a NavMesh path into a set of points that form a path as straight as possible.
        /// </summary>
        /// <param name="portals">The raw path from the NavMesh.</param>
        /// <param name="appendTo">List to append the corner points to</param>
        /// <typeparam name="TProj"></typeparam>
        /// <remarks>This method works for 'flat' worlds. See <see cref="NavMeshGraphCorners"/></remarks>
        public static void AppendCorners<TProj>(NativeSlice<TProj> portals, NativeList<float3> appendTo) 
            where TProj : unmanaged, IUnrolledNavMeshGraphPortal
        {
            int apexIndex = 0;
            int leftIndex = 1;
            int rightIndex = 1;
                 
            // first is real starting point
            var firstSeg = portals[apexIndex];
            float2 portalApex = .5f * (firstSeg.Left2D + firstSeg.Right2D);
            float2 portalLeft = portals[leftIndex].Left2D;
            float2 portalRight = portals[rightIndex].Right2D;

            for (int i = 2; i < portals.Length; i++)
            {
                var portal = portals[i];
                float2 pLeft = portal.Left2D;
                float2 pRight = portal.Right2D;

                if (TriArea2(portalApex, portalRight, pRight) <= 0f)
                {
                    if (Equals2(portalApex, portalRight) || TriArea2(portalApex, portalLeft, pRight) > 0.0f)
                    {
                        portalRight = pRight;
                        rightIndex = i;
                    } 
                    else
                    {
                        portalApex = portalRight = portalLeft;
                        i = apexIndex = rightIndex = leftIndex;
                        
                        appendTo.Add(portals[apexIndex].Left3D);
                        continue;
                    }
                }

                if (TriArea2(portalApex, portalLeft, pLeft) >= 0.0f)
                {
                    if (Equals2(portalApex, portalLeft) || TriArea2(portalApex, portalRight, pLeft) < 0.0f)
                    {
                        portalLeft = pLeft;
                        leftIndex = i;
                    } 
                    else 
                    {
                        portalApex = portalLeft = portalRight;
                        i = apexIndex = leftIndex = rightIndex;

                        appendTo.Add(portals[apexIndex].Right3D);
                    }
                }
            }

            // only add final (narrowed) portal if we didn't split at it
            if (apexIndex < portals.Length-1)
            {
                // add narrowed goal
                appendTo.Add(portals[portals.Length-1].Left3D);
            }
        }

        /// <summary>
        /// Converts a NavMesh path into a set of points that form a path as straight as possible.
        /// </summary>
        /// <param name="portals">The raw path from the NavMesh.</param>
        /// <param name="appendTo">List to append the corner points to</param>
        /// <param name="weldThreshold">
        /// Weld corners that are below this distance together. This can prevent multiple corners at the same position where 3 or more triangles in the path intersect
        /// </param>
        /// <remarks>This method works for curved worlds. See <see cref="NavMeshGraphCorners3D"/></remarks>
        public static void AppendCornersUnrolled<T>(NativeSlice<T> portals, NativeList<CornerAndNormal> appendTo, float weldThreshold = .01f)
            where T : unmanaged, IUnrolledNavMeshGraphPortal
        {
            int apexIndex = 0;
            int leftIndex = 1;
            int rightIndex = 1;
            
            weldThreshold *= weldThreshold;

            // first is real starting point
            var firstSeg = portals[apexIndex];
            
            // Left2D == Right2D for the first segment
            float2 portalApex = firstSeg.Left2D;
            float2 portalLeft = portals[leftIndex].Left2D;
            float2 portalRight = portals[rightIndex].Right2D;
            float3 prevIntersectedPos = new float3(float.PositiveInfinity);

            for (int i = 2; i < portals.Length; i++)
            {
                var portal = portals[i];
                float2 pLeft = portal.Left2D;
                float2 pRight = portal.Right2D;

                if (TriArea2(portalApex, portalRight, pRight) <= 0f)
                {
                    if (Equals2(portalApex, portalRight) || TriArea2(portalApex, portalLeft, pRight) > 0.0f)
                    {
                        portalRight = pRight;
                        rightIndex = i;
                    } 
                    else
                    {
                        for (int j = apexIndex; j < leftIndex; j++)
                        {
                            var skippedPortal = portals[j];
                            float3 intersectedPos = skippedPortal.Intersect(portalApex, portalLeft);

                            if (math.distancesq(intersectedPos, prevIntersectedPos) > weldThreshold)
                            {
                                appendTo.Add(new CornerAndNormal()
                                {
                                    position = intersectedPos,
                                    normal = skippedPortal.Normal,
                                    
                                    // was there for debug:
                                    //left3D = skippedPortal.Left3D,
                                    //right3D = skippedPortal.Right3D
                                });
                                prevIntersectedPos = intersectedPos;
                            } 
                            
                           
                        }
                        
                        portalApex = portalRight = portalLeft;
                        i = apexIndex = rightIndex = leftIndex;

                        continue;
                    }
                }

                if (TriArea2(portalApex, portalLeft, pLeft) >= 0.0f)
                {
                    if (Equals2(portalApex, portalLeft) || TriArea2(portalApex, portalRight, pLeft) < 0.0f)
                    {
                        portalLeft = pLeft;
                        leftIndex = i;
                    } 
                    else
                    {
                        
                        for (int j = apexIndex; j < rightIndex; j++)
                        {
                            var skippedPortal = portals[j];
                            float3 intersectedPos = skippedPortal.Intersect(portalApex, portalRight);
                            if (math.distancesq(intersectedPos, prevIntersectedPos) > weldThreshold)
                            {
                                appendTo.Add(new CornerAndNormal()
                                {
                                    position = intersectedPos,
                                    normal = skippedPortal.Normal
                                });
                                prevIntersectedPos = intersectedPos;
                            }
                        }
                        
                        portalApex = portalLeft = portalRight;
                        i = apexIndex = leftIndex = rightIndex;
                    }
                }
            }

            // intersect all segments, except the last, since it's not really a portal but the final goal position
            float2 goalPosProj = portals[portals.Length - 1].Left2D;
            for (int i = apexIndex; i < portals.Length; i++)
            {
                var skippedPortal = portals[i];
                float3 intersectedPos = skippedPortal.Intersect(portalApex, goalPosProj);
                if (math.distancesq(intersectedPos, prevIntersectedPos) > weldThreshold)
                {
                    appendTo.Add(new CornerAndNormal()
                    {
                        position = intersectedPos,
                        normal = skippedPortal.Normal,
                    });
                    prevIntersectedPos = intersectedPos;
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals2(float2 a, float2 b)
        {
            return math.distancesq(a, b) < .001f * .001f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsZero(float2 x)
        {
            return math.lengthsq(x) < 0.001f * 0.001f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equals3(float3 a, float3 b)
        {
            //return math.all(a == b);
            return math.distancesq(a, b) < .001f * .001f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float TriArea2(float2 a, float2 b, float2 c)
        {
            float2 axy = b - a;
            float2 bxy = c - a;
            return bxy.x * axy.y - axy.x * bxy.y;
        }

        /// <summary>
        /// Takes the 2D line and calculates the intersection with a portal. Returns the original point in 3D space.
        /// </summary>
        public static float3 Intersect<T>(this T proj, float2 oldApex, float2 newApex) where T : IUnrolledNavMeshGraphPortal
        {
            float2 p = proj.Left2D;
            float2 p2 = proj.Right2D;
            float2 q = oldApex;
            float2 q2 = newApex;
                   
            var r = p2 - p;
            var s = q2 - q;
            var rxs = cross2D(r, s); 
            var qpxr = cross2D(q - p, r);
            float t;
       
            // check for colinearity or parallel. in either case, we return the point on the portal that is closest
            // to the new apex.
            if ((IsZero(rxs) && IsZero(qpxr)) ||
                (IsZero(rxs) && !IsZero(qpxr)))
            {
                t = math.dot(q2 - p, r) / math.dot(r, r);
            }
            else
            {
                t = cross2D(q - p, s) / rxs;
            }
            
            return math.lerp(proj.Left3D, proj.Right3D, math.saturate(t));
        }
        


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float cross2D(float2 v1, float2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }
    }
}