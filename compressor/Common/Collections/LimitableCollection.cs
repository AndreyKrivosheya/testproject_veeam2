using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Common.Collections
{
    interface LimitableCollection<T> : IDisposable
    {
        int MaxCapacity { get; }
        int Count { get; }

        bool IsCompleted { get; }
        
        bool IsAddingCompleted { get; }

        bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken);
        bool TryAdd(T item, int millisecondsTimeout);

        void CompleteAdding();
       
        bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken);
        bool TryTake(out T item, int millisecondsTimeout);
    }
}