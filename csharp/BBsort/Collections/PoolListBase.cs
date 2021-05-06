using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Flexols.Data.Collections
{
    [DebuggerDisplay("PoolList. Size: {m_size}")]
    public class PoolListBase<T> : IEnumerable
    {
        public static readonly T Default = default(T);

        internal readonly int m_maxCapacity;

        internal T[] m_items;

        internal int m_size;

        public int Count => m_size;

        public ref T this[int index]
        {
            get
            {
                if (index >= m_size)
                {
                    throw new IndexOutOfRangeException();
                }

                return ref m_items[index];
            }
        }

        public PoolListBase(int maxCapacity, int arraySize)
        {
            m_maxCapacity = maxCapacity;
            m_items = ArrayPool<T>.Shared.Rent(Math.Min(arraySize, maxCapacity));
            Array.Clear(m_items, 0, m_items.Length);
        }

        public PoolListBase(int maxCapacity, int arraySize, int size)
          : this(maxCapacity, arraySize)
        {
            m_size = size;
        }

        public PoolListBase(PoolListBase<T> poolList)
        {
            var newArr = ArrayPool<T>.Shared.Rent(poolList.m_items.Length);

            Array.Clear(newArr, 0, newArr.Length);

            Array.Copy(poolList.m_items, 0, newArr, 0, poolList.m_size);

            m_items = newArr;
            m_size = poolList.m_size;
            m_maxCapacity = poolList.m_maxCapacity;
        }

        public bool Add(T item)
        {
            AddItem(item);
         
            return true;
        }

        public void AddItem(T item)
        {
            if (m_items == null)
            {
                T[] vals = ArrayPool<T>.Shared.Rent(Math.Max(4, m_size * 2));

                Array.Clear(vals, 0, vals.Length);

                m_items = vals;
            }
            else if (m_size == m_items.Length)
            {
                int newCapacity = Math.Min(m_items.Length == 0 ? 4 : m_items.Length * 2, m_maxCapacity);

                T[] vals = ArrayPool<T>.Shared.Rent(newCapacity);

                Array.Clear(vals, 0, vals.Length);

                if (m_size > 0)
                {
                    Array.Copy(m_items, 0, vals, 0, m_size);
                }

                ArrayPool<T>.Shared.Return(m_items, clearArray: false);

                m_items = vals;
            }

            m_items[m_size] = item;

            m_size++;
        }

        public void Clear()
        {
            if (m_items != null)
            {
                ArrayPool<T>.Shared.Return(m_items, clearArray: false);
            }

            if (m_size > 0)
            {
                m_items = null;

                m_size = 0;
            }
        }

        public bool RemoveLast()
        {
            if (m_size > 0)
            {
                m_size--;

                m_items[m_size] = Default;
            }

            return m_size == 0;
        }

        public void RemoveAt(int index)
        {
            m_size = m_size - 1;

            if (index < m_size)
            {
                Array.Copy(m_items, index + 1, m_items, index, m_size - index);
            }

            if (m_size >= 0)
            {
                m_items[m_size] = Default;
            }
        }

        public void Insert(int index, T item)
        {
            if (m_items == null)
            {
                T[] vals = ArrayPool<T>.Shared.Rent(Math.Max(4, m_size * 2));

                Array.Clear(vals, 0, vals.Length);

                m_items = vals;
            }
            else if (m_size == m_items.Length)
            {
                int newCapacity = Math.Min(m_items.Length == 0 ? 4 : m_items.Length * 2, m_maxCapacity);

                T[] vals = ArrayPool<T>.Shared.Rent(newCapacity);

                Array.Clear(vals, 0, vals.Length);

                if (m_size > 0)
                {
                    Array.Copy(m_items, 0, vals, 0, m_size);
                }

                ArrayPool<T>.Shared.Return(m_items, clearArray: false);

                m_items = vals;
            }

            if (index < m_size)
            {
                Array.Copy(m_items, index, m_items, index + 1, m_size - index);
            }

            m_items[index] = item;
            m_size = m_size + 1;
        }

        public int RemoveAll(Func<T, bool> match)
        {
            int index1 = 0;
            while (index1 < m_size && !match(m_items[index1]))
            {
                ++index1;
            }

            if (index1 >= m_size)
            {
                return 0;
            }

            int index2 = index1 + 1;
            while (index2 < m_size)
            {
                while (index2 < m_size && match(m_items[index2]))
                {
                    ++index2;
                }
                if (index2 < m_size)
                {
                    m_items[index1++] = m_items[index2++];
                }
            }

            Array.Clear(m_items, index1, m_size - index1);

            int num = m_size - index1;
            m_size = index1;

            return num;
        }

        public int FindIndex(Predicate<T> match, int start)
        {
            for (int index = start; index < m_items.Length && index < m_size; ++index)
            {
                if (match(m_items[index]))
                {
                    return index;
                }
            }
            return -1;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int index = 0; index < m_size && index < m_items.Length; index++)
            {
                yield return m_items[index];
            }
        }

        public ref T ValueByRef(int index)
        {
            return ref m_items[index];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal int RemoveAll(T item, IComparer<T> comparer)
        {
            int index1 = 0;
            while (index1 < m_size && comparer.Compare(m_items[index1], item) != 0)
            {
                ++index1;
            }

            if (index1 >= m_size)
            {
                return 0;
            }

            int index2 = index1 + 1;
            while (index2 < m_size)
            {
                while (index2 < m_size && comparer.Compare(m_items[index2], item) == 0)
                {
                    ++index2;
                }
                if (index2 < m_size)
                {
                    m_items[index1++] = m_items[index2++];
                }
            }

            Array.Clear(m_items, index1, m_size - index1);

            int num = m_size - index1;
            m_size = index1;

            return num;
        }
    }
}
