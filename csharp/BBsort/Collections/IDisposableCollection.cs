using System;
using System.Collections.Generic;

namespace Flexols.Data.Interfaces
{
    public interface IDisposableCollection
    {
        void AddDisposable(IDisposable disposable);

        bool RemoveDisposable(IDisposable disposable);

        IEnumerable<IDisposable> Items { get; }
    }
}