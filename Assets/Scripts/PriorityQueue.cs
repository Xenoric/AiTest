using System;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue
{
    private List<(Vector2 Item, float Priority)> heap;

    public PriorityQueue()
    {
        heap = new List<(Vector2, float)>();
    }

    public int Count => heap.Count;

    public void Enqueue(Vector2 item, float priority)
    {
        heap.Add((item, priority));
        SiftUp(heap.Count - 1);
    }

    public Vector2 Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Queue is empty");

        var result = heap[0].Item;

        // Перемещаем последний элемент на верхушку
        heap[0] = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);

        if (heap.Count > 0)
        {
            SiftDown(0);
        }

        return result;
    }

    public Vector2 Peek()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Queue is empty");

        return heap[0].Item;
    }

    public void Clear()
    {
        heap.Clear();
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            if (heap[parentIndex].Priority <= heap[index].Priority)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    private void SiftDown(int index)
    {
        int lastIndex = heap.Count - 1;

        while (true)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            int smallestIndex = index;

            if (leftChildIndex <= lastIndex &&
                heap[leftChildIndex].Priority < heap[smallestIndex].Priority)
                smallestIndex = leftChildIndex;

            if (rightChildIndex <= lastIndex &&
                heap[rightChildIndex].Priority < heap[smallestIndex].Priority)
                smallestIndex = rightChildIndex;

            if (smallestIndex == index)
                break;

            Swap(index, smallestIndex);
            index = smallestIndex;
        }
    }

    private void Swap(int indexA, int indexB)
    {
        var temp = heap[indexA];
        heap[indexA] = heap[indexB];
        heap[indexB] = temp;
    }
}