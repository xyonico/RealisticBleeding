using System;

namespace RealisticBleeding
{
    public class FastList<T>
    {
        private readonly T[] _array;

        public int Count { get; private set; }

        public FastList(int capacity)
        {
            _array = new T[capacity];
        }

        public ref T this[int index] => ref _array[index];

        public bool TryAdd(in T value)
        {
            if (Count >= _array.Length)
            {
                return false;
            }

            _array[Count++] = value;

            return true;
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
    }
}