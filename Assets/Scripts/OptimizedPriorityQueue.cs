using System;
using System.Collections.Generic;

public class OptimizedPriorityQueue<T>
{
    private List<(T item, float priority)> heap = new List<(T, float)>();
    private Dictionary<T, int> itemToIndex = new Dictionary<T, int>();

    public int Count => heap.Count;

    // Добавление элемента в очередь с приоритетом
    public void Enqueue(T item, float priority)
    {
        if (itemToIndex.ContainsKey(item))
        {
            // Если элемент уже существует, обновляем его приоритет
            UpdatePriority(item, priority);
            return;
        }

        // Добавляем элемент в конец кучи
        heap.Add((item, priority));
        int index = heap.Count - 1;
        itemToIndex[item] = index;

        // Просеиваем вверх для восстановления свойств кучи
        SiftUp(index);
    }

    // Извлечение элемента с наименьшим приоритетом
    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Queue is empty");

        var minItem = heap[0].item;
        itemToIndex.Remove(minItem);

        // Перемещаем последний элемент на место корня
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        if (heap.Count > 0)
        {
            itemToIndex[heap[0].item] = 0;
            // Просеиваем вниз для восстановления свойств кучи
            SiftDown(0);
        }

        return minItem;
    }

    // Проверка наличия элемента в очереди
    public bool Contains(T item)
    {
        return itemToIndex.ContainsKey(item);
    }

    // Обновление приоритета элемента
    public void UpdatePriority(T item, float newPriority)
    {
        if (!itemToIndex.ContainsKey(item))
            throw new InvalidOperationException("Item not found in queue");

        int index = itemToIndex[item];
        float oldPriority = heap[index].priority;
        heap[index] = (item, newPriority);

        // Просеиваем вверх или вниз в зависимости от изменения приоритета
        if (newPriority < oldPriority)
            SiftUp(index);
        else
            SiftDown(index);
    }

    // Просеивание вверх (восстановление свойств кучи)
    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;

            if (heap[index].priority >= heap[parentIndex].priority)
                break;

            Swap(index, parentIndex);
            index = parentIndex;
        }
    }

    // Просеивание вниз (восстановление свойств кучи)
    private void SiftDown(int index)
    {
        int lastIndex = heap.Count - 1;

        while (true)
        {
            int leftChildIndex = 2 * index + 1;
            int rightChildIndex = 2 * index + 2;
            int smallestIndex = index;

            if (leftChildIndex <= lastIndex && heap[leftChildIndex].priority < heap[smallestIndex].priority)
                smallestIndex = leftChildIndex;

            if (rightChildIndex <= lastIndex && heap[rightChildIndex].priority < heap[smallestIndex].priority)
                smallestIndex = rightChildIndex;

            if (smallestIndex == index)
                break;

            Swap(index, smallestIndex);
            index = smallestIndex;
        }
    }

    // Обмен элементов в куче
    private void Swap(int indexA, int indexB)
    {
        var temp = heap[indexA];
        heap[indexA] = heap[indexB];
        heap[indexB] = temp;

        // Обновляем индексы в словаре
        itemToIndex[heap[indexA].item] = indexA;
        itemToIndex[heap[indexB].item] = indexB;
    }
}