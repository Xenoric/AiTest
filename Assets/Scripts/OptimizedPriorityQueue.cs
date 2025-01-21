using System;
using Unity.Collections;

public class OptimizedPriorityQueue<T> : IDisposable where T : unmanaged
{
    private struct Element
    {
        public T Item;
        public float Priority;
    }

    private NativeArray<Element> heap;
    private int size;
    private const int DefaultCapacity = 512;

    public OptimizedPriorityQueue(int capacity = DefaultCapacity, Allocator allocator = Allocator.Persistent)
    {
        heap = new NativeArray<Element>(capacity, allocator);
        size = 0;
    }

    public int Count => size;

    public void Enqueue(T item, float priority)
    {
        if (size == heap.Length)
        {
            Resize(heap.Length * 2);
        }

        heap[size] = new Element { Item = item, Priority = priority };
        SiftUp(size);
        size++;
    }

    public T Dequeue()
    {
        if (size == 0)
            throw new InvalidOperationException("Queue is empty");

        T result = heap[0].Item;
        heap[0] = heap[size - 1];
        size--;
        SiftDown(0);
        return result;
    }

    private void Resize(int newSize)
    {
        NativeArray<Element> newHeap = new NativeArray<Element>(newSize, Allocator.Persistent);
        for (int i = 0; i < size; i++)
        {
            newHeap[i] = heap[i];
        }
        heap.Dispose();
        heap = newHeap;
    }

    private void SiftUp(int index)
    {
        var element = heap[index];
        while (index > 0)
        {
            int parentIndex = (index - 1) >> 1;
            if (heap[parentIndex].Priority <= element.Priority)
                break;

            heap[index] = heap[parentIndex];
            index = parentIndex;
        }
        heap[index] = element;
    }

    private void SiftDown(int index)
    {
        var element = heap[index];
        int halfSize = size >> 1;

        while (index < halfSize)
        {
            int leftChild = (index << 1) + 1;
            int rightChild = leftChild + 1;
            int smallestChild = leftChild;

            if (rightChild < size && heap[rightChild].Priority < heap[leftChild].Priority)
                smallestChild = rightChild;

            if (heap[smallestChild].Priority >= element.Priority)
                break;

            heap[index] = heap[smallestChild];
            index = smallestChild;
        }
        heap[index] = element;
    }

    public void Clear()
    {
        size = 0;
    }

    public void Dispose()
    {
        if (heap.IsCreated)
            heap.Dispose();
    }
}
