using System;
using System.Threading;

namespace compressor.Processor.Queue
{
    class BlockToWrite: Block
    {
        public BlockToWrite(BlockToProcess originalBlock, byte[] data)
            : base(originalBlock, originalBlock.OriginalLength, data)
        {
        }

        public bool Last
        {
            get
            {
                return Awaiter.Last;
            }
        }

        public void NotifyProcessedAndAddedToQueue()
        {
            Awaiter.NotifyProcessedAndAddedToQueueToWrite();
        }

        public void WaitPreviousBlockProcessedAndAddedToQueue(CancellationToken cancellationToken)
        {
            Awaiter.WaitPreviousBlockProcessedAndAddedToQueueToWrite(cancellationToken);
        }
    }
}