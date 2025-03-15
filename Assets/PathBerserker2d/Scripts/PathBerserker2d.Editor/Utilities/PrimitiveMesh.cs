using UnityEditor;
using UnityEngine;

namespace PathBerserker2d
{
    static class PrimitiveMesh
    {
        static Mesh quad;

        public static Mesh Quad
        {
            get
            {
                if (quad == null)
                {
                    quad = new Mesh();
                    quad.vertices = new Vector3[] {
                new Vector3(0, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0),
                new Vector3(1, 0, 0)};

                    quad.triangles = new int[] {
                0, 1, 2,
                0, 2, 3};
                }
                return quad;
            }
        }
    }
}
