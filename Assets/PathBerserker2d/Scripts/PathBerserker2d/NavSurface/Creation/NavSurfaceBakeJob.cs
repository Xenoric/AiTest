using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using UnityEngine;

namespace PathBerserker2d
{
    internal class NavSurfaceBakeJob
    {
        public bool IsFinished { get { return bakeThread != null && !bakeThread.IsAlive; } }
        public bool IsRunning { get { return bakeThread != null && bakeThread.IsAlive; } }
        public float Progress { get { return progress; } }
        public float TotalBakeTime { get; private set; }

        public List<NavSegment> navSegments;
        public Rect bounds;

        private Polygon[][] polygons;
        private IntersectionTester it;
        private NavSurface owner;
        private Tuple<Rect, Vector2>[] subtractors;
        Matrix4x4 surfaceWorldToLocal;
        Matrix4x4 surfaceLocalToWorld;

        private Thread bakeThread;
        private volatile float progress = 0;
        private volatile bool aborted;

        public NavSurfaceBakeJob(NavSurface owner)
        {
            this.owner = owner;
        }

        public void Start(Polygon[][] polygons, IntersectionTester it, Tuple<Rect, Vector2>[] subtractors, Matrix4x4 surfaceWorldToLocal)
        {
            this.polygons = polygons;
            this.it = it;
            this.subtractors = subtractors;
            this.surfaceWorldToLocal = surfaceWorldToLocal;
            this.surfaceLocalToWorld = surfaceWorldToLocal.inverse;

            aborted = false;
            progress = 0;

            bakeThread = new Thread(Bake);
            bakeThread.IsBackground = true;
            bakeThread.Start();
        }

        public void AbortJoin()
        {
            if (bakeThread != null && bakeThread.IsAlive)
            {
                this.aborted = true;
                bakeThread.Join();
            }
        }

        private void Bake()
        {
            // filter out empty polygons
            polygons = polygons.Where(p => p.Length > 0).ToArray();

            if (polygons.Length == 0)
            {
                navSegments = new List<NavSegment>(0);
                bounds = new Rect();
                return;
            }

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var polySet = new PolygonSet();
            foreach (var polys in polygons)
            {
                foreach (var poly in polys)
                {
                    polySet.AddPolygon(it.Clipper, poly);
                }
            }

            //pre process, simplify countours
            bounds = polySet.First().BoundingRect;
            foreach (var poly in polySet)
            {
                poly.Simplify(owner.SmallestDistanceYouCareAbout);
                bounds = bounds.CombineWith(poly.BoundingRect);
            }

            float totalContourCount = 0;
            foreach (var poly in polySet)
            {
                totalContourCount += poly.Holes.Count + 1;
            }

            navSegments = new List<NavSegment>((int)totalContourCount * 5);
            float processedContourCount = 0;
            foreach (var poly in polySet)
            {
                foreach (var contour in poly)
                {
                    it.MarkContour(contour, owner, polySet, navSegments);

                    processedContourCount++;
                    progress = processedContourCount / totalContourCount;

                    if (aborted)
                        goto End;
                }
            }

            // post process 
            // 1. filter slopes
            FilterSlopes();

            // 2. substract substractors
            FilterSubtractors();

            // 3. filter by length
            FilterSmallSegments();

        End:
            sw.Stop();
            TotalBakeTime = sw.ElapsedMilliseconds;
        }

        private void RemoveSegmentAt(int index)
        {
            if (navSegments[index].HasNext)
            {
                navSegments[navSegments[index].NextSegmentIndex].PrevSegmentIndex = -1;
            }
            if (navSegments[index].HasPrev)
            {
                navSegments[navSegments[index].PrevSegmentIndex].NextSegmentIndex = -1;
            }

            int lastIndex = navSegments.Count - 1;
            if (lastIndex != index)
            {
                if (navSegments[lastIndex].HasNext)
                {
                    navSegments[navSegments[lastIndex].NextSegmentIndex].PrevSegmentIndex = index;
                }
                if (navSegments[lastIndex].HasPrev)
                {
                    navSegments[navSegments[lastIndex].PrevSegmentIndex].NextSegmentIndex = index;
                }
                navSegments[index] = navSegments[lastIndex];
            }
            navSegments.RemoveAt(lastIndex);
        }

        private void FilterSlopes()
        {
            for (int i = 0; i < navSegments.Count; i++)
            {
                var seg = navSegments[i];
                if (Vector2.Angle(Vector2.up, surfaceLocalToWorld.MultiplyVector(seg.Normal)) > owner.MaxSlopeAngle)
                {
                    RemoveSegmentAt(i);
                    i--;
                }
            }
        }

        private void FilterSubtractors()
        {
            float i1, i2;
            for (int i = 0; i < navSegments.Count; i++)
            {
                float angle = Vector2.SignedAngle(navSegments[i].Tangent, Vector2.up);

                foreach (var s in subtractors)
                {
                    if (!ExtendedGeometry.IsAngleBetweenAngles(s.Item2.x, s.Item2.y, angle))
                        continue;

                    if (ExtendedGeometry.RectLineIntersection(s.Item1, surfaceLocalToWorld.MultiplyPoint3x4(navSegments[i].GetPositionAlongSegment(0)), surfaceLocalToWorld.MultiplyPoint3x4(navSegments[i].GetPositionAlongSegment(navSegments[i].Length)), out i1, out i2))
                    {
                        // entirely inside
                        if (i1 < 0 && i2 > 1)
                        {
                            RemoveSegmentAt(i);
                            i--;
                            break;
                        }
                        else if (i1 > 0 || i2 < 1)
                        {
                            // inside, 2 intersections, subdiv
                            float cellSize = (float)navSegments[i].Length / (float)navSegments[i].CellCount;
                            float[] clearances = navSegments[i].CloneCellClearances();

                            NavSegment segA = null;
                            NavSegment segB = null;

                            if (i1 > 0 && i1 < 1)
                            {
                                int cellCountA = Mathf.CeilToInt(navSegments[i].CellCount * i1);
                                float cellSizeA = (float)(i1 * navSegments[i].Length) / (float)cellCountA;
                                float cellConvA = cellSizeA / cellSize;
                                bool cellSizeEqual = Mathf.Abs(cellSizeA - cellSize) < 0.001f;

                                float[] clearancesA = new float[cellCountA];

                                for (int iCell = 0; iCell < clearancesA.Length; iCell++)
                                {
                                    int a = Mathf.FloorToInt(iCell * cellConvA);

                                    if (cellSizeEqual)
                                        clearancesA[iCell] = clearances[a];
                                    else
                                    {
                                        int b = Mathf.FloorToInt((iCell + 1) * cellConvA);

                                        // prevents rounding issue
                                        if (b >= clearances.Length)
                                            b--;

                                        clearancesA[iCell] = Mathf.Min(clearances[a], clearances[b]);
                                    }
                                }

                                Vector2 newStart = surfaceLocalToWorld.MultiplyPoint3x4(navSegments[i].Start);
                                segA = new NavSegment(owner, surfaceWorldToLocal.MultiplyPoint3x4(newStart), navSegments[i].Tangent, i1 * navSegments[i].Length, clearancesA);
                            }

                            if (i2 > 0 && i2 < 1)
                            {
                                int cellCountB = Mathf.CeilToInt(navSegments[i].CellCount * (1f - i2));

                                float cellSizeB = (float)((1f - i2) * navSegments[i].Length) / (float)cellCountB;
                                float cellConvB = cellSizeB / cellSize;
                                float cellOffset = ((navSegments[i].Length / cellSizeB) - cellCountB) * cellConvB;
                                bool cellSizeEqual = Mathf.Abs(cellSizeB - cellSize) < 0.001f;

                                float[] clearancesB = new float[cellCountB];

                                for (int iCell = 0; iCell < clearancesB.Length; iCell++)
                                {
                                    int a = Mathf.FloorToInt(cellOffset + iCell * cellConvB);


                                    //happens when cellsizes match exactly
                                    if (cellSizeEqual)
                                        clearancesB[iCell] = clearances[a];
                                    else
                                    {
                                        int b = Mathf.FloorToInt(cellOffset + (iCell + 1) * cellConvB);

                                        // prevents rounding issue
                                        if (b >= clearances.Length)
                                            b--;

                                        clearancesB[iCell] = Mathf.Min(clearances[a], clearances[b]);
                                    }
                                }

                                Vector2 newStart = navSegments[i].Start + navSegments[i].Tangent * i2 * navSegments[i].Length;
                                segB = new NavSegment(owner, newStart, navSegments[i].Tangent, (1f - i2) * navSegments[i].Length, clearancesB);
                            }

                            if (segA != null && segB != null)
                            {
                                if (navSegments[i].HasNext)
                                    navSegments[navSegments[i].NextSegmentIndex].PrevSegmentIndex = navSegments.Count;

                                segA.PrevSegmentIndex = navSegments[i].PrevSegmentIndex;
                                segB.NextSegmentIndex = navSegments[i].NextSegmentIndex;

                                navSegments[i] = segA;
                                navSegments.Add(segB);
                            }
                            else if (segA != null)
                            {
                                if (navSegments[i].HasNext)
                                    navSegments[navSegments[i].NextSegmentIndex].PrevSegmentIndex = -1;

                                segA.PrevSegmentIndex = navSegments[i].PrevSegmentIndex;
                                navSegments[i] = segA;
                            }
                            else if (segB != null)
                            {
                                if (navSegments[i].HasPrev)
                                    navSegments[navSegments[i].PrevSegmentIndex].NextSegmentIndex = -1;

                                segB.NextSegmentIndex = navSegments[i].NextSegmentIndex;
                                navSegments[i] = segB;
                            }
                        }
                    }
                }
            }
        }

        private void FilterSmallSegments()
        {
            for (int i = 0; i < navSegments.Count; i++)
            {
                var seg = navSegments[i];
                if (seg.Length < owner.MinSegmentLength)
                {
                    RemoveSegmentAt(i);
                    i--;
                }
            }
        }
    }
}
