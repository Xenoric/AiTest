using System;

namespace Scripts.Bot.Grid
{
    public interface IHeapNode<T> : IComparable<T>
    {
        int HeapIndex { get; set; }
    }
}