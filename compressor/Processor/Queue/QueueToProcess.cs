using System;
using System.Collections.Concurrent;
using System.Threading;

namespace compressor.Processor.Queue
{
    class QueueToProcess: Queue<BlockToProcess>
    {
        public QueueToProcess(int maxCapacity)
            : base(maxCapacity)
        {
        }
    }
}