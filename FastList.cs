using System;

namespace RealisticBleeding
{
    public class FastList<T>
    {
        private T[] _array;

        public int Count { get; private set; }
        public int Capacity => _array.Length;

        public FastList(int capacity)
        {
            _array = new T[capacity];

            if (_array.Length == 0)
            {
                UnityEngine.Debug.LogError("Got back a zero length array!");
            }
        }

        public ref T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                return ref _array[index];
            }
        }

        public bool TryAddNoResize(in T value)
        {
            if (Count >= _array.Length)
            {
                return false;
            }

            _array[Count++] = value;

            return true;
        }

        public void Add(in T value)
        {
            if (Count >= _array.Length)
            {
                DoubleCapacity();
            }

            _array[Count++] = value;
        }

        public void Insert(int index, in T value)
        {
            if (index < 0 || index > Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (index == Count)
            {
                Add(in value);

                return;
            }

            if (Count >= _array.Length)
            {
                DoubleCapacity();
            }

            Array.Copy(_array, index, _array, index + 1, Count - index);

            _array[index] = value;
            Count++;
        }

        public void RemoveAtSwapBack(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            Count--;

            if (index == Count)
            {
                return;
            }

            _array[index] = _array[Count];
        }

        public void Clear()
        {
            Count = 0;
        }

        private void DoubleCapacity()
        {
            var newArray = new T[_array.Length * 2];

            if (newArray.Length == 0)
            {
                UnityEngine.Debug.LogError("Got back a zero length array!");
            }

            Array.Copy(_array, newArray, _array.Length);

            _array = newArray;
        }

        public string Debug()
        {
            return $"Count: {Count}, Array Length: {_array.Length}";
        }
    }
}