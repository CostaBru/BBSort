using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Flexols.Data.Collections
{
    public static class ReadOnlyExt
    {
        public static IReadOnlyCollection<T> TryCombine<T>(this IReadOnlyCollection<T> collection1, IReadOnlyCollection<T> collection2)
        {
            if (collection1 == null)
            {
                return collection2;
            }

            if (collection2 == null)
            {
                return collection1;
            }

            return new CollectionWrap<T>(collection1, collection2);
        }

        private class CollectionWrap<T> : ICollection<T>, IReadOnlyCollection<T>
        {
            private readonly IReadOnlyCollection<T> m_left;
            private readonly IReadOnlyCollection<T> m_right;

            public CollectionWrap(IReadOnlyCollection<T> t1, IReadOnlyCollection<T> t2)
            {
                m_left = t1;
                m_right = t2;
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var v in m_left)
                {
                    yield return v;
                }

                foreach (var v in m_right)
                {
                    yield return v;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void Add(T item)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(T item)
            {
                if (m_left.Contains(item))
                {
                    return true;
                }

                if (m_right.Contains(item))
                {
                    return true;
                }

                return false;
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public bool Remove(T item)
            {
                throw new NotImplementedException();
            }

            int ICollection<T>.Count => m_left.Count + m_right.Count;

            public bool IsReadOnly => true;

            int IReadOnlyCollection<T>.Count => m_left.Count + m_right.Count;
        }
    }
}