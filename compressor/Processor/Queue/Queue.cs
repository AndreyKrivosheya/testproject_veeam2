using System;
using System.Threading;

using compressor.Common.Collections;

namespace compressor.Processor.Queue
{
    class Queue<TBlock> : LimitableCollection<TBlock>, IDisposable
        where TBlock: Block
    {
        public Queue(int maxCapacity)
        {
            if(maxCapacity < 1)
            {
                throw new ArgumentException("Can't limit collection to less than 1 item", "maxCapacity");
            }

            this.Implementation = new LimitableQueue<TBlock>(maxCapacity);
        }

        readonly LimitableCollection<TBlock> Implementation;

        public void Dispose()
        {
            Implementation.Dispose();
        }

        public int MaxCapacity
        {
            get
            {
                return Implementation.MaxCapacity;
            }
        }

        public int Count
        {
            get
            {
                return Implementation.Count;
            }
        }

        public bool IsCompleted
        {
            get
            {
                return Implementation.IsCompleted;
            }
        }
        
        public bool IsAddingCompleted
        {
            get
            {
                return Implementation.IsAddingCompleted;
            }
        }

        public virtual bool TryAdd(TBlock block, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            try
            {
                return Implementation.TryAdd(block, millisecondsTimeout, cancellationToken);
            }
            catch(OperationCanceledException)
            {
                if(!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                else
                {
                    return false;
                }
            }
        }

        public void CompleteAdding()
        {
            Implementation.CompleteAdding();
        }
       
        public bool TryTake(out TBlock block, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            try
            {
                return Implementation.TryTake(out block, millisecondsTimeout, cancellationToken);
            }
            catch(OperationCanceledException)
            {
                if(!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                else
                {
                    block = default(TBlock);
                    return false;
                }
            }
        }
    }
}