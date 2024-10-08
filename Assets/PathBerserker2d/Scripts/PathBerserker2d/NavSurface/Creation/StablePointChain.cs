using System.Collections.Generic;
using System;

namespace PathBerserker2d
{
    internal class StablePointChain<T> where T : IEquatable<T>
    {
        public LinkedList<T> points = new LinkedList<T>();

        bool closed; // is the first point linked with the last one

        public StablePointChain(T a, T b)
        {
            closed = false;

            Add(a, b);
        }

        public void Add(T a, T b)
        {
            points.AddLast(a);
            points.AddLast(b);
        }

        public bool LinkSegment(T a, T b)
        {
            if (b.Equals(points.First.Value))
            {
                if (a.Equals(points.Last.Value))
                    closed = true;
                else
                    points.AddFirst(a);
                return true;
            }
            if (a.Equals(points.Last.Value))
            {
                if (b.Equals(points.First.Value))
                    closed = true;
                else
                    points.AddLast(b);
                return true;
            }
            return false;
        }

        public bool LinkPointChain(StablePointChain<T> chain)
        {
            if (chain.points.First.Value.Equals(points.Last.Value))
            {
                chain.points.RemoveFirst();
                AppendRange(chain.points);

                return true;
            }
            if (chain.points.Last.Value.Equals(points.First.Value))
            {
                points.RemoveFirst();
                PrependRange(chain.points);

                return true;
            }
            return false;
        }

        private void PrependRange(LinkedList<T> list)
        {
            var node = list.Last;
            do
            {
                this.points.AddFirst(node.Value);
            } while ((node = node.Previous) != null);
        }

        private void AppendRange(LinkedList<T> list)
        {
            var node = list.First;
            do
            {
                this.points.AddLast(node.Value);
            } while ((node = node.Next) != null);
        }

        public LinkedListNode<T> First { get { return points.First; } }
        public LinkedListNode<T> Last { get { return points.Last; } }

        public bool IsClosed() { return closed; }
        public void Clear() { points.Clear(); }
        public int GetNumPoints() { return points.Count; }
    }
}