using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal class Polygon : System.IEquatable<Polygon>, IEnumerable<Contour>
    {
        public Contour Hull => hull;
        public List<Contour> Holes { get; set; }
        public bool IsEmpty => hull.IsEmpty;
        public float XMax => boundingRect.xMax;
        public Rect BoundingRect => boundingRect;

        private Contour hull;
        private Rect boundingRect;

        public Polygon(Contour hull, List<Contour> holes)
        {
            this.hull = hull;
            this.Holes = holes;

            UpdateBounds();
        }

        public Polygon(Contour hull)
        {
            this.hull = hull;
            this.Holes = new List<Contour>(0);

            UpdateBounds();
        }

        public Polygon()
        {
            this.hull = new Contour(new Vector2[0]);
            this.Holes = new List<Contour>(0);
        }

        public bool Equals(Polygon other)
        {
            return other == this;
        }

        public void AddAsChild(Polygon other)
        {
            this.Holes.Add(other.hull);
            this.Holes.AddRange(other.Holes);
        }

        public int TotalVertCount()
        {
            int result = hull.VertexCount;
            foreach (var path in Holes)
            {
                result += path.VertexCount;
            }
            return result;
        }

        public bool BoundsOverlap(Polygon other)
        {
            return boundingRect.Overlaps(other.boundingRect);
        }

        public bool PointInPolyon(Vector2 point)
        {
            return boundingRect.Contains(point) && hull.PointInContour(point);
        }

        public bool Contains(Polygon other)
        {
            foreach (var v in other.hull)
            {
                if (!PointInPolyon(v))
                    return false;
            }
            return true;
        }

        public bool Contains(Contour other)
        {
            foreach (var v in other)
            {
                if (!PointInPolyon(v))
                    return false;
            }
            return true;
        }

        public void Draw()
        {
            Gizmos.color = Color.green;
            hull.Draw();

            Gizmos.color = Color.yellow;
            foreach (var contour in this.Holes)
            {
                contour.Draw();
            }
        }

        public IEnumerator<Contour> GetEnumerator()
        {
            yield return hull;
            foreach (var hole in Holes)
                yield return hole;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return hull;
            foreach (var hole in Holes)
                yield return hole;
        }

        public void UpdateBounds()
        {
            if (hull.VertexCount == 0)
                return;

            Vector2 min = hull[0];
            Vector2 max = hull[0];
            for (int iVert = 1; iVert < hull.VertexCount; iVert++)
            {
                min = Vector2.Min(hull[iVert], min);
                max = Vector2.Max(hull[iVert], max);
            }
            // add some fudge
            Vector2 fudge = new Vector2(0.0001f, 0.0001f);
            boundingRect = new Rect(min - fudge, max - min + fudge * 2);
        }

        public void Simplify(float tolerance)
        {
            hull.Simplify(tolerance);
            foreach (var hole in Holes)
            {
                hole.Simplify(tolerance);
            }
            UpdateBounds();
        }

        public double SignedArea()
        {
            double area = Hull.SignedArea();
            foreach (var hole in Holes)
                area += hole.SignedArea();
            return area;
        }

        public void EnsureCWOrdering()
        {
            if (Hull.IsCW())
                return;

            Hull.Invert();
            foreach (var child in Holes)
                child.Invert();
        }
    }
}