using System.Collections.Generic;

public class OptimizedPriorityQueue<T>
{
    private List<(T item, float priority)> elements = new ();
    private HashSet<T> elementsSet = new ();

    public int Count => elements.Count;

    public void Enqueue(T item, float priority)
    {
        if (elementsSet.Contains(item)) return;

        elements.Add((item, priority));
        elements.Sort((x, y) => x.priority.CompareTo(y.priority));
        elementsSet.Add(item);
    }

    public T Dequeue()
    {
        var bestItem = elements[0].item;
        elements.RemoveAt(0);
        elementsSet.Remove(bestItem);
        return bestItem;
    }

    public bool Contains(T item)
    {
        return elementsSet.Contains(item);
    }
}