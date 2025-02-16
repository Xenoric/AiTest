using System.Collections.Generic;

public class ObjectPool<T> where T : class
{
    private readonly Stack<T> _pool = new();
    private readonly System.Func<T> _createFunc;

    public ObjectPool(System.Func<T> createFunc)
    {
        _createFunc = createFunc;
    }

    public T Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }
        return _createFunc();
    }

    public void Release(T obj)
    {
        if (obj is System.Collections.IDictionary dictionary)
        {
            dictionary.Clear();
        }
        _pool.Push(obj);
    }
}