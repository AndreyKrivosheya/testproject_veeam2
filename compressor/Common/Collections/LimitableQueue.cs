using System.Collections.Concurrent;

namespace compressor.Common.Collections
{
    class LimitableQueue<T> : LimitableCollection<T, ConcurrentQueue<T>>
    {
        public LimitableQueue(int maxCapacity)
            : base(maxCapacity)
        {
        }
    }
}