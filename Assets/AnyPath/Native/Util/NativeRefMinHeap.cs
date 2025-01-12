using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;

namespace AnyPath.Native.Util
{
    public interface IRefComparer<T>
    {
        int Compare(ref T a, ref T b);
    }
    
    /// <summary>
    /// Copy from <see cref="NativeMinHeap{T,TComp}"/> but the comparer works with a ref argument,
    /// which is faster for the case where T is a large struct
    /// </summary>
    public struct NativeRefMinHeap<T, TComp> : INativeDisposable
        where T : unmanaged
        where TComp : struct, IRefComparer<T>
    {
        private NativeList<T> values;
        private TComp comparer;

        public NativeRefMinHeap(TComp comparer, Allocator allocator)
        {
            this.comparer = comparer;
            values = new NativeList<T>(allocator);
            values.Add(default(T));
        }

        public void Clear()
        {
            values.Clear();
            values.Add(default(T));
        }


        public int Count => values.Length - 1;
        public T Min => values[1];

        public bool TryPop(out T value)
        {
            if (Count > 0)
            {
                value = ExtractMin();
                return true;
            }

            value = default;
            return false;
        }

        public T ExtractMin()
        {
            int count = Count;

            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (count == 0)
            {
                throw new InvalidOperationException("Heap is empty.");
            }
            #endif

            var min = Min;
            values[1] = values[count];
            values.RemoveAt(count);

            if (values.Length > 1)
            {
                BubbleDown(1);
            }

            return min;
        }


        public void Push(T item)
        {
            values.Add(item);
            BubbleUp(Count);
        }

        private void BubbleUp(int index)
        {
            int parent = index / 2;
            while (index > 1 && CompareResult(parent, index) > 0)
            {
                Exchange(index, parent);
                index = parent;
                parent /= 2;
            }
        }

        private void BubbleDown(int index)
        {
            int min;

            while (true)
            {
                int left = index * 2;
                int right = left + 1;

                if (left < values.Length &&
                    CompareResult(left, index) < 0)
                {
                    min = left;
                }
                else
                {
                    min = index;
                }

                if (right < values.Length &&
                    CompareResult(right, min) < 0)
                {
                    min = right;
                }

                if (min != index)
                {
                    Exchange(index, min);
                    index = min;
                }
                else
                {
                    return;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int CompareResult(int index1, int index2)
        {
            return comparer.Compare(ref values.ElementAt(index1), ref values.ElementAt(index2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Exchange(int index, int max)
        {
            var tmp = values[index];
            values[index] = values[max];
            values[max] = tmp;
        }
        
        public void Dispose()
        {
            values.Dispose();
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            return values.Dispose(inputDeps);
        }
    }
}