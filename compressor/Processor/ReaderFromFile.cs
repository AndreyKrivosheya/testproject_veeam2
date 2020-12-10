using System;
using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ReaderFromFile: Reader
    {
        public ReaderFromFile(SettingsProvider settings, Stream streamToRead, QueueToProcess queueToProcess)
            : base(settings, streamToRead, queueToProcess)
        {
        }

        public sealed override void ReadBlocksAndQueueForProcessing(CancellationToken cancellationToken)
        {
            try
            {
                BlockToProcess lastBlock = null;
                while(!cancellationToken.IsCancellationRequested)
                {
                    var blockBuffer = new byte[Settings.BlockSize];
                    var blockActuallyRead = StreamToRead.Read(blockBuffer, 0, blockBuffer.Length);
                    if(blockActuallyRead != 0)
                    {
                        var blockBufferActuallyRead = blockBuffer;
                        if(blockActuallyRead < blockBuffer.Length)
                        {
                            blockBufferActuallyRead = new byte[blockActuallyRead];
                            Array.Copy(blockBuffer, 0, blockBufferActuallyRead, 0, blockBufferActuallyRead.Length);
                        }
                        
                        lastBlock = new BlockToProcess(lastBlock, blockBufferActuallyRead.Length, blockBufferActuallyRead);
                        QueueToProcess.TryAdd(lastBlock, Timeout.Infinite, cancellationToken);
                    }
                    else
                    {
                        // finished reading input
                        QueueToProcess.CompleteAdding();
                        break;
                    }
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to read blocks from input file", e);
            }
        }
    }
}