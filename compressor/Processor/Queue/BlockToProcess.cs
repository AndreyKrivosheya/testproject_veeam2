using System;
using System.Threading;

namespace compressor.Processor.Queue
{
    class BlockToProcess: Block
    {
        public BlockToProcess(BlockToProcess previousBlock, long originalLength, byte[] data)
            : base(previousBlock != null ? new AwaiterForAddedToQueue(previousBlock.Awaiter) : new AwaiterForAddedToQueue(), originalLength, data)
        {
        }
    }
}