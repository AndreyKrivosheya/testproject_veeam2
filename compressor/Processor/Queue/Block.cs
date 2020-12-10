using System;
using System.Collections.Generic;
using System.Threading;

namespace compressor.Processor.Queue
{
    abstract class Block
    {
        protected class AwaiterForAddedToQueue
        {
            public AwaiterForAddedToQueue(AwaiterForAddedToQueue previous)
            {
                if(previous != null)
                {
                    this.Previous = previous;
                    this.Previous.Last = false;
                    //this.A = this.Previous.A + 1;
                }
                else
                {
                    //this.A = 0;
                }
            }
            public AwaiterForAddedToQueue()
                : this(null)
            {
            }

            //readonly int A;

            volatile AwaiterForAddedToQueue Previous = null;

            public bool Last { get; private set; } = true;

            readonly object BlockAddedToQueueToWriteLazyLock = new object();
            Lazy<ManualResetEvent> BlockAddedToQueueToWriteLazy = new Lazy<ManualResetEvent>(() => new ManualResetEvent(false));

            public void NotifyProcessedAndAddedToQueueToWrite()
            {
                lock(BlockAddedToQueueToWriteLazyLock)
                {
                    if(BlockAddedToQueueToWriteLazy.IsValueCreated)
                    {
                        if(BlockAddedToQueueToWriteLazy.Value != null)
                        {
                            BlockAddedToQueueToWriteLazy.Value.Set();
                        }
                    }
                    Previous = null;
                    BlockAddedToQueueToWriteLazy = new Lazy<ManualResetEvent>(() => null);
                }
            }

            public void WaitBlockProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken)
            {
                // wait this block to be added to queue
                WaitHandle waitableThisBlockAddedToQueueToWrite;
                lock(BlockAddedToQueueToWriteLazyLock)
                {
                    waitableThisBlockAddedToQueueToWrite = BlockAddedToQueueToWriteLazy.Value;
                }
                if(waitableThisBlockAddedToQueueToWrite != null)
                {
                    switch(WaitHandle.WaitAny(new [] { waitableThisBlockAddedToQueueToWrite, cancellationToken.WaitHandle }, Timeout.Infinite))
                    {
                        case 0:
                            lock(BlockAddedToQueueToWriteLazyLock)
                            {
                                BlockAddedToQueueToWriteLazy = new Lazy<ManualResetEvent>(() => null);
                            }

                            try
                            {
                                waitableThisBlockAddedToQueueToWrite.Close();
                            }
                            catch(Exception)
                            {
                                // probably already closed on another thread
                                // one of the threads should succeed
                            }

                            break;
                        case 1:
                            throw new OperationCanceledException(cancellationToken);
                    }
                }
            }
            public void WaitPreviousBlockProcessedAndAddedToQueueToWrite(CancellationToken cancellationToken)
            {
                var previous = Previous;
                if(previous != null)
                {
                    previous.WaitBlockProcessedAndAddedToQueueToWrite(cancellationToken);
                }
            }
        };

        protected Block(AwaiterForAddedToQueue awaiter, long originalLength, byte[] data)
        {
            this.Awaiter = awaiter;
            this.OriginalLength = originalLength;
            this.Data = data;
        }
        protected Block(Block awaiterBlock, long originalLength, byte[] data)
            : this(awaiterBlock.Awaiter, originalLength, data)
        {
        }

        protected readonly AwaiterForAddedToQueue Awaiter;
        public readonly byte[] Data;
        public long OriginalLength;
    }
}