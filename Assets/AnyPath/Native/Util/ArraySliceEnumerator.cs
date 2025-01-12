#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Internal;

namespace AnyPath.Native.Util
{
    /// <summary>
    /// Utility enumerator that allows for allocation free enumeration of managed array slices.
    /// </summary>
    [ExcludeFromDocs]
    public struct ArraySliceEnumerator<T> : IEnumerator<T>
    {
        private readonly T[] _array;
        private readonly int _start;
        private readonly int _end; // cache Offset + Count, since it's a little slow
        private int _current;

        public ArraySliceEnumerator(T[] array, int start, int length)
        {
            _array = array;
            _start = start;
            _end = start + length;
            _current = start - 1;
        }

        public bool MoveNext()
        {
            if (_current < _end)
            {
                _current++;
                return _current < _end;
            }
            return false;
        }

        public T Current
        {
            get
            {
                if (_current < _start)
                    throw new InvalidOperationException("Enumeration not started");
                if (_current >= _end)
                        throw new InvalidOperationException("Enumeration ended");
                return _array![_current];
            }
        }

        object? IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            _current = _start - 1;
        }

        public void Dispose()
        {
        }
    }
}