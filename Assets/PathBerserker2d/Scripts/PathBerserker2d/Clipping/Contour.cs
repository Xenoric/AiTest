using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal class Contour : IEnumerable<Vector2>
    {
        public int VertexCount { get { return verts.Count; } }
        public bool IsEmpty { get { return verts.Count == 0; } }
        public bool IsClosed { get; private set; }
        public List<Vector2> Verts => verts;

        private List<Vector2> verts;

        public Contour(List<Vector2> verticies, bool isClosed = true)
        {
            this.verts = verticies;
            IsClosed = isClosed;
        }

        public Contour(IEnumerable<Vector2> verticies, bool isClosed = true)
        {
            this.verts = new List<Vector2>(verticies);
            IsClosed = isClosed;
        }

        public Vector2 this[int key]
        {
            get { return verts[key]; }
        }

        public IEnumerator<Vector2> GetEnumerator()
        {
            return ((IEnumerable<Vector2>)verts).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Vector2>)verts).GetEnumerator();
        }

        public double SignedArea()
        {
            double area = 0;
            for (int i = 0, j = verts.Count - 1; i < verts.Count; j = i, i++)
                area += (verts[i].x - verts[j].x) *
                        (verts[i].y + verts[j].y);
            return area / 2.0;
        }

        public double Area()
        {
            return Math.Abs(SignedArea());
        }

        public void MakeCW()
        {
            if (!IsCW())
                verts.Reverse();
        }

        public void Invert()
        {
            verts.Reverse();
        }

        public bool IsCW()
        {
            Vector2 lowestPoint = verts[0];
            int lowestPointIndex = 0;
            for (int i = 1; i < VertexCount; i++)
            {
                if (lowestPoint.y > verts[i].y ||
                    lowestPoint.y == verts[i].y && lowestPoint.x < verts[i].x)
                {
                    lowestPoint = verts[i];
                    lowestPointIndex = i;
                }
            }

            Vector2 prevPoint = lowestPointIndex == 0 ? verts[VertexCount - 1] : verts[lowestPointIndex - 1];
            Vector2 nextPoint = lowestPointIndex == VertexCount - 1 ? verts[0] : verts[lowestPointIndex + 1];

            return ((lowestPoint.x - prevPoint.x) * (nextPoint.y - prevPoint.y) - (nextPoint.x - prevPoint.x) * (lowestPoint.y - prevPoint.y)) < 0;
        }

        public bool PointInContour(Vector2 point)
        {
            bool c = false;
            for (int i = 0, j = VertexCount - 1; i < VertexCount; j = i++)
            {
                if (((verts[i].y > point.y) != (verts[j].y > point.y)) &&
                 (point.x < (verts[j].x - verts[i].x) * (point.y - verts[i].y) / (verts[j].y - verts[i].y) + verts[i].x))
                    c = !c;
            }
            return c;
        }

        public bool Contains(Contour other)
        {
            foreach (var v in other)
                if (!PointInContour(v))
                    return false;
            return true;
        }

        public void Draw()
        {
            for (int i = IsClosed ? 0 : 1, j = IsClosed ? VertexCount - 1 : 0; i < VertexCount; j = i, i++)
            {
                DebugDrawingExtensions.DrawArrow(verts[i], verts[j]);
            }
        }

        public void Simplify(float tolerance)
        {
            ExtendedGeometry.MergeCloseVerts(verts, tolerance);
            this.verts = ExtendedGeometry.SimplifyContour(this.verts, tolerance);
        }
    }
}