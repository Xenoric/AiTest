using UnityEngine;
using UnityEditor;
using System;
using System.Text;

namespace PathBerserker2d
{
    internal static class GLThickLine
    {
       
        private static Matrix4x4 matrix;

        public static void UseSolidMat()
        {
            SharedMaterials.UnlitVertexColorSolid.SetPass(0);
        }

        public static void UseTransparentMat()
        {
            SharedMaterials.UnlitVertexColorTransparent.SetPass(0);
        }

        public static void Begin(Matrix4x4 matrix)
        {
            GLThickLine.matrix = matrix;
            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            GL.LoadProjectionMatrix(Camera.current.projectionMatrix);
            UseSolidMat();
        }

        public static void End()
        {
            GL.End();
            GL.PopMatrix();
        }

        public static void DrawLine(Vector2 a, Vector2 b, Color color, float width)
        {
            a = matrix.MultiplyPoint3x4(a);
            b = matrix.MultiplyPoint3x4(b);
            Vector2 normal = new Vector2(-(b.y - a.y), b.x - a.x).normalized * width * 0.5f;

            GL.Color(color);
            GL.Vertex3(a.x - normal.x, a.y - normal.y, 0);
            GL.Vertex3(a.x + normal.x, a.y + normal.y, 0);
            GL.Vertex3(b.x + normal.x, b.y + normal.y, 0);
            GL.Vertex3(b.x - normal.x, b.y - normal.y, 0);
        }

        public static void DrawRect(Vector3[] corners, Color color)
        {
            GL.Color(color);
            GL.Vertex(matrix.MultiplyPoint3x4(corners[0]));
            GL.Vertex(matrix.MultiplyPoint3x4(corners[1]));
            GL.Vertex(matrix.MultiplyPoint3x4(corners[2]));
            GL.Vertex(matrix.MultiplyPoint3x4(corners[3]));
        }

        public static string ToUpper(string str)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(str));
        }
    }
}
