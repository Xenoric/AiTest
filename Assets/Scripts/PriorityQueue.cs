using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System;

public struct PriorityQueueItem : IComparable<PriorityQueueItem>
{
    public int ItemId;  // Используем int вместо generic
    public float Priority;

    public int CompareTo(PriorityQueueItem other)
    {
        return Priority.CompareTo(other.Priority);
    }
}

public class PriorityQueue : IDisposable
{
    private NativeList<PriorityQueueItem> heap;
    private Allocator allocatorType;
    private NativeHashMap<int, Vector2> itemMap;  // Для маппинга ID к Vector2

    public PriorityQueue(int initialCapacity, Allocator allocator = Allocator.Temp)
    {
        allocatorType = allocator;
        heap = new NativeList<PriorityQueueItem>(initialCapacity, allocator);
        itemMap = new NativeHashMap<int, Vector2>(initialCapacity, allocator);
    }

    private int currentItemId = 0;

    public int Count => heap.Length;

    public void Enqueue(Vector2 item, float priority)
    {
        currentItemId++;
        var newItem = new PriorityQueueItem 
        { 
            ItemId = currentItemId, 
            Priority = priority 
        };
        
        heap.Add(newItem);
        itemMap.Add(currentItemId, item);
        SiftUp(heap.Length - 1);
    }

    public Vector2 Dequeue()
    {
        if (heap.Length == 0)
            throw new InvalidOperationException("Queue is empty");

        int itemId = heap[0].ItemId;
        Vector2 result = itemMap[itemId];
        
        // Удаляем из мэппинга
        itemMap.Remove(itemId);

        heap[0] = heap[heap.Length - 1];
        heap.RemoveAt(heap.Length - 1);

        if (heap.Length > 0)
        {
            SiftDown(0);
        }

        return result;
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
        int lastIndex = heap.Length - 1;

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
        (heap[indexA], heap[indexB]) = (heap[indexB], heap[indexA]);
    }

    public void Dispose()
    {
        if (heap.IsCreated)
            heap.Dispose();
        
        if (itemMap.IsCreated)
            itemMap.Dispose();
    }

    public void Clear()
    {
        heap.Clear();
        itemMap.Clear();
    }
}