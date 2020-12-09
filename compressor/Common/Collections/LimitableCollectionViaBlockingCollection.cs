using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Common.Collections
{
    class LimitableCollectionViaBlockingCollection<T> : LimitableCollection<T>
    {
        public LimitableCollectionViaBlockingCollection(BlockingCollection<T> blockingCollection)
        {
            this.BlockingCollection = blockingCollection;
        }
        public LimitableCollectionViaBlockingCollection(int maxCapacity)
            : this(new BlockingCollection<T>(new ConcurrentQueue<T>(), maxCapacity))
        {
        }

        readonly BlockingCollection<T> BlockingCollection;

        public void Dispose()
        {
            BlockingCollection.Dispose();
        }

        public int MaxCapacity
        {
            get
            {
                return BlockingCollection.BoundedCapacity;
            }
        }
        
        public int Count
        {
            get
            {
                return BlockingCollection.Count;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return BlockingCollection.IsCompleted;
            }
        }
        
        public bool IsAddingCompleted
        {
            get
            {
                return BlockingCollection.IsAddingCompleted;
            }
        }

        public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return BlockingCollection.TryAdd(item, millisecondsTimeout, cancellationToken);
        }
        public bool TryAdd(T item, int millisecondsTimeout)
        {
            return BlockingCollection.TryAdd(item, millisecondsTimeout);
        }

        public void CompleteAdding()
        {
            BlockingCollection.CompleteAdding();
        }
       
        public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return BlockingCollection.TryTake(out item, millisecondsTimeout, cancellationToken);
        }
        public bool TryTake(out T item, int millisecondsTimeout)
        {
            return BlockingCollection.TryTake(out item, millisecondsTimeout);
        }
    }
}