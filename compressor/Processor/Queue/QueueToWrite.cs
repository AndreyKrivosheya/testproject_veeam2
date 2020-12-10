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

        public override bool TryAdd(BlockToWrite block, int millisecondsTimeout)
        {
            if(base.TryAdd(block, millisecondsTimeout))
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