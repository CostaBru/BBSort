using System;
using System.Diagnostics;

namespace Flexols.Data.Collections
{
    [DebuggerDisplay("PoolList. Size: {m_size}")]
    public class PoolList<T> : PoolListBase<T>, IDisposable
    {
        private bool m_isDisposed;

        public PoolList(int maxCapacity, int arraySize) : base(maxCapacity, arraySize)
        {
        }

        public PoolList(int maxCapacity, int arraySize, T item) : base(maxCapacity, arraySize, item)
        {
        }

        public PoolList(PoolListBase<T> poolList) : base(poolList)
        {
        }

        ~PoolList()
        {
            Dispose(false);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!(m_isDisposed))
            {
                Clear();

                m_isDisposed = true;
            }
        }
    }
}