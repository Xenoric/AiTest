using System.Collections.Generic;
using System;
using UnityEngine;

namespace PathBerserker2d
{
    internal class PointChain
    {
        public LinkedList<Vector2> points = new LinkedList<Vector2>();

        bool closed; // is the first point linked with the last one

        public PointChain(Vector2 a, Vector2 b)
        {
            closed = false;

            Add(a, b);
        }

        public void Add(Vector2 a, Vector2 b)
        {
            points.AddLast(a);
            points.AddLast(b);
        }

        public bool LinkSegment(Vector2 a, Vector2 b)
        {
            if (b == points.First.Value)
            {
                if (a == points.Last.Value)
                    closed = true;
                else
                    points.AddFirst(a);
                return true;
            }
            if (a == points.Last.Value)
            {
                if (b.Equals(points.First.Value))
                    closed = true;
                else
                    points.AddLast(b);
                return true;
            }
            return false;
        }

        public bool LinkPointChain(PointChain chain)
        {
            if (chain.points.First.Value == (points.Last.Value))
            {
                chain.points.RemoveFirst();
                AppendRange(chain.points);

                return true;
            }
            if (chain.points.Last.Value == (points.First.Value))
            {
                points.RemoveFirst();
                PrependRange(chain.points);

                return true;
            }
            return false;
        }

        private void PrependRange(LinkedList<Vector2> list)
        {
            var node = list.Last;
            do
            {
                this.points.AddFirst(node.Value);
            } while ((node = node.Previous) != null);
        }

        private void PrependRangeReverse(LinkedList<Vector2> list)
        {
            var node = list.First;
            do
            {
                this.points.AddFirst(node.Value);
            } while ((node = node.Next) != null);
        }

        private void AppendRange(LinkedList<Vector2> list)
        {
            var node = list.First;
            do
            {
                this.points.AddLast(node.Value);
            } while ((node = node.Next) != null);
        }

        private void AppendRangeReverse(LinkedList<Vector2> list)
        {
            var node = list.Last;
            do
            {
                this.points.AddLast(node.Value);
            } while ((node = node.Previous) != null);
        }

        public LinkedListNode<Vector2> First { get { return points.First; } }
        public LinkedListNode<Vector2> Last { get { return points.Last; } }

        public bool IsClosed() { return closed; }
        public void Clear() { points.Clear(); }
        public int GetNumPoints() { return points.Count; }
    }
}