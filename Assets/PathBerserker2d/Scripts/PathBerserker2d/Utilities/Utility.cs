using System.Collections.Generic;
using UnityEngine;

namespace PathBerserker2d
{
    internal static class Utility
    {
        public static T WeightedRandomChoice<T>(IEnumerable<T> values, IEnumerable<float> weights)
        {
            float total = 0;
            foreach (var w in weights)
            {
                total += w;
            }
            return WeightedRandomChoice(values, weights, total);
        }

        public static T WeightedRandomChoice<T>(IEnumerable<T> values, IEnumerable<float> weights, float totalWeight)
        {
            float total = 0;
            foreach (var w in weights)
            {
                total += w;
            }

            float r = Random.value * total;
            IEnumerator<T> enumerator = values.GetEnumerator();
            foreach (var w in weights)
            {
                enumerator.MoveNext();
                if (w >= r)
                    return enumerator.Current;

                r -= w;
            }
            throw new System.Exception();
        }

        public static void ResizeWithDefault<T>(ref T[] array, int newLength, T defaultElem)
        {
            int oldLength = array.Length;
            System.Array.Resize(ref array, newLength);
            for (int i = oldLength; i < newLength; i++)
            {
                array[i] = defaultElem;
            }
        }
    }
}
