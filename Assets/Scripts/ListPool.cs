using System.Collections.Generic;

public static class ListPool<T>
{
    private static readonly Stack<List<T>> pool = new Stack<List<T>>();

    public static List<T> Get()
    {
        if (pool.Count > 0)
        {
            return pool.Pop();
        }
        return new List<T>();
    }

    public static void Release(List<T> list)
    {
        list.Clear();
        pool.Push(list);
    }
}