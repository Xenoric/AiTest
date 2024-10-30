using System.Collections.Concurrent;

namespace Scripts.Bot.Grid
{
    public class Heap<T> where T : IHeapNode<T>
    {
        private ConcurrentQueue<T> _queue;
        private T[] _items;
        private int _currentItemCount;

        public Heap(int maxHeapSize) => _items = new T[maxHeapSize];

        public void Add(T item)
        {
            item.HeapIndex = _currentItemCount;
            _items[_currentItemCount] = item;
            SortUp(item);
            _currentItemCount++;
        }

        public T RemoveFirst()
        {
            var firstItem = _items[0];
            _currentItemCount--;
            _items[0] = _items[_currentItemCount];
            _items[0].HeapIndex = 0;
            SortDown(_items[0]);
            return firstItem;
        }

        public void UpdateItem(T item)
        {
            SortUp(item);
            SortDown(item);
        }
        
        public int Count => _currentItemCount;
        
        public bool Contains(T item)
        {
            return Equals(_items[item.HeapIndex], item);
        }

        private void SortDown(T item)
        {
            while (true)
            {
                int childIndexLeft = item.HeapIndex * 2 + 1;
                int childIndexRight = item.HeapIndex * 2 + 2;

                if (childIndexLeft < _currentItemCount)
                {
                    int swapIndex = childIndexLeft;

                    if (childIndexRight < _currentItemCount)
                    {
                        if (_items[childIndexLeft].CompareTo(_items[childIndexRight]) < 0)
                            swapIndex = childIndexRight;
                    }

                    if (item.CompareTo(_items[swapIndex]) < 0)
                        Swap(item, _items[swapIndex]);
                    else
                        return;
                } else
                    return;
            }
        }

        private void SortUp(T item)
        {
            int parentIndex = (item.HeapIndex - 1) / 2;

            while (true)
            {
                T parentItem = _items[parentIndex];

                if (item.CompareTo(parentItem) > 0)
                    Swap(item, parentItem);
                else
                    break;

                parentIndex = (item.HeapIndex - 1) / 2;
            }
        }

        private void Swap(T item1, T item2)
        {
            (_items[item1.HeapIndex], _items[item2.HeapIndex]) = (item2, item1);
            (item1.HeapIndex, item2.HeapIndex) = (item2.HeapIndex, item1.HeapIndex);
        }
    }
}