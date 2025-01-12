using Unity.Mathematics;
using UnityEngine;

namespace AnyPath.Graphs.Extra
{
    /// <summary>
    /// Represents a triangle in 3D space that supports raycasting
    /// </summary>
    public struct Triangle
    {
        public float3 a;
        public float3 b;
        public float3 c;
            
        public float3 Centroid => (a + b + c) / 3;
        
        public float3 Normal => math.normalize(math.cross(b - a, c - a));

        public bool IsDegenerate() => math.lengthsq(math.cross(b - a, c - a)) < float.Epsilon;

        public Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }


        /// <summary>
        /// Projects a point in 3D space onto the plane this triangle lies in
        /// </summary>
        /// <param name="point">The point to project</param>
        /// <returns>The closest point on the plane of this triangle, it's possible for the returned point to be outside of the triangle itself.</returns>
        public float3 ProjectPointOntoPlane(float3 point)
        {
            float3 normal = this.Normal;
            float distance = -math.dot(normal, a);
            float3 num = math.dot(normal, point) + distance;
            return point - normal * num;
        }
        
        /// <summary>
        /// Checks if the specified ray hits the triangle.
        /// Möller–Trumbore ray-triangle intersection algorithm implementation.
        /// </summary>
        /// <param name="ray">The ray to test hit for.</param>
        /// <param name="point">The point where the triangle was hit.</param>
        /// <returns><c>true</c> when the ray hits the triangle, otherwise <c>false</c></returns>
        public bool Raycast(Ray ray, out float3 point)
        {
            if (Raycast(ray, out float t))
            {
                point = ray.origin + ray.direction * t;
                return true;
            }

            point = float3.zero;
            return false;
        }
        
        /// <summary>
        /// Checks if the specified ray hits the triangle.
        /// Möller–Trumbore ray-triangle intersection algorithm implementation.
        /// </summary>
        /// <param name="ray">The ray to test hit for.</param>
        /// <param name="hitT">how far along the ray was the triangle hit</param>
        /// <returns><c>true</c> when the ray hits the triangle, otherwise <c>false</c></returns>
        public bool Raycast(Ray ray, out float hitT)
        {
            // Vectors from p1 to p2/p3 (edges)
            float3 e1, e2;  
 
            float3 p, q, t;
            float det, invDet, u, v;

            //Find vectors for two edges sharing vertex/point p1
            e1 = b - a;
            e2 = c - a;
 
            // calculating determinant 
            p = math.cross(ray.direction, e2);
 
            //Calculate determinat
            det = math.dot(e1, p);
 
            //if determinant is near zero, ray lies in plane of triangle otherwise not
            if (det > -float.Epsilon && det < float.Epsilon)
            {
                hitT = 0;
                return false;
            }
            invDet = 1.0f / det;
 
            //calculate distance from p1 to ray origin
            t = (float3)ray.origin - a;
 
            //Calculate u parameter
            u = math.dot(t, p) * invDet;
 
            //Check for ray hit
            if (u < 0 || u > 1)
            {
                hitT = 0;
                return false;
            }
 
            //Prepare to test v parameter
            q = math.cross(t, e1);
 
            //Calculate v parameter
            v =  math.dot(ray.direction, q) * invDet;
 
            //Check for ray hit
            if (v < 0 || u + v > 1)
            {
                hitT = 0;
                return false;
            }

            hitT = math.dot(e2, q) * invDet;
            return hitT > float.Epsilon;
        }
        
        /// <summary>
        /// Returns the closest point to p on this triangle
        /// </summary>
        /// <param name="p">The point to get the closest point to</param>
        /// <returns>The closest point on this triangle</returns>
        public float3 ClosestPoint(float3 p)
        {
            // Original implementation:
            // https://github.com/embree/embree/blob/master/tutorials/common/math/closest_point.h
            
            float3 ab = b - a;
            float3 ac = c - a;
            float3 ap = p - a;

            float d1 = math.dot(ab, ap);
            float d2 = math.dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f) return a; //#1

            float3 bp = p - b;
            float d3 = math.dot(ab, bp);
            float d4 = math.dot(ac, bp);
            if (d3 >= 0f && d4 <= d3) return b; //#2

            float3 cp = p - c;
            float d5 = math.dot(ab, cp);
            float d6 = math.dot(ac, cp);
            if (d6 >= 0f && d5 <= d6) return c; //#3

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                float v = d1 / (d1 - d3);
                
                // For some degenerate triangles d1-d3 is zero, yielding NaN. 
                if (!float.IsNaN(v))
                    return a + v * ab; //#4
            }
    
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                float v = d2 / (d2 - d6);
                return a + v * ac; //#5
            }
    
            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
            {
                float v = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + v * (c - b); //#6
            }

            {
                float denom = 1f / (va + vb + vc);
                float v = vb * denom;
                float w = vc * denom;
                return a + v * ab + w * ac; //#0
            }
        }
    }
}