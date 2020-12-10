using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue
{
    class QueueToWrite: Queue<BlockToWrite>
    {
        public QueueToWrite(int maxCapacity)
            : base(maxCapacity)
        {
        }

        public override bool TryAdd(BlockToWrite block, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // wait previous block already processed and added to queue
            try
            {
                block.WaitPreviousBlockProcessedAndAddedToQueue(cancellationToken);
            }
            catch(OperationCanceledException)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                else
                {
                    throw;
                }
            }
            // add block to queue and notify added
            if(base.TryAdd(block, millisecondsTimeout, cancellationToken))
            {
                block.NotifyProcessedAndAddedToQueue();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}