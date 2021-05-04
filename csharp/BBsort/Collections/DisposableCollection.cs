using System;
using System.Collections.Generic;
using Flexols.Data.Interfaces;

namespace Flexols.Data.Collections
{
    public class DisposableCollection : IDisposableCollection
    {
        private HybridList<IDisposable> m_list = new HybridList<IDisposable>();

        public void AddDisposable(IDisposable disposable)
        {
            m_list.Add(disposable);
        }

        public bool RemoveDisposable(IDisposable disposable)
        {
            return m_list.Remove(disposable);
        }

        public IEnumerable<IDisposable> Items
        {
            get { return m_list; }
        }

        public void Dispose()
        {
            foreach (var disposable in m_list)
            {
                disposable.Dispose();
            }

            m_list.Clear();
        }
    }
}
