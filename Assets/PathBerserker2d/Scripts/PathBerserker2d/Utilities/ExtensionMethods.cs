using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal static class ExtensionMethods
    {

        #region LinkedList

        public static void AppendRange<T>(this LinkedList<T> source, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                source.AddLast(item);
            }
        }

        public static void PrependRange<T>(this LinkedList<T> source, IEnumerable<T> items)
        {
            LinkedListNode<T> first = source.First;
            foreach (T item in items)
            {
                source.AddBefore(first, item);
            }
        }

        public static T[] ToArray<T>(this LinkedList<T> source)
        {
            T[] result = new T[source.Count];
            LinkedListNode<T> cNode = source.First;
            int index = 0;
            do
            {
                result[index] = cNode.Value;
                index++;
            } while ((cNode = cNode.Next) != null);
            return result;
        }

        public static void Reverse<T>(this LinkedList<T> source)
        {
            var head = source.First;
            while (head.Next != null)
            {
                var next = head.Next;
                source.Remove(next);
                source.AddFirst(next.Value);
            }
        }
        #endregion

        public static bool IsLayerWithinMask(this LayerMask source, int other)
        {
            return source == (source | (1 << other));
        }

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
            float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static Vector2 RotateRad(this Vector2 v, float rads)
        {
            float sin = Mathf.Sin(rads);
            float cos = Mathf.Cos(rads);

            float tx = v.x;
            float ty = v.y;
            v.x = (cos * tx) - (sin * ty);
            v.y = (sin * tx) + (cos * ty);
            return v;
        }

        public static bool ContainsBounds(this Rect source, Rect other)
        {
            return source.Contains(other.min) && source.Contains(other.max);
        }

        public static Rect CombineWith(this Rect source, Rect other)
        {
            return Rect.MinMaxRect(
                Mathf.Min(source.xMin, other.xMin),
                Mathf.Min(source.yMin, other.yMin),
                Mathf.Max(source.xMax, other.xMax),
                Mathf.Max(source.yMax, other.yMax));
        }

        public static float Area(this Rect source)
        {
            return source.x * source.y;
        }
    }
}
