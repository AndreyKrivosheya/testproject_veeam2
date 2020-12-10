using System;
using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ReaderFromArchive: Reader
    {
        public ReaderFromArchive(SettingsProvider settings, Stream streamToRead, QueueToProcess queueToProcess)
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
                    var blockLengthBuffer = new byte[sizeof(Int64)];
                    var blockLengthActuallyRead = StreamToRead.Read(blockLengthBuffer, 0, blockLengthBuffer.Length);
                    if(blockLengthActuallyRead != 0)
                    {
                        if(blockLengthActuallyRead != sizeof(Int64))
                        {
                            throw new InvalidDataException("Failed to read next block size, read less bytes then block size length occupies");
                        }
                        else
                        {
                            var blockBuffer = new byte[BitConverter.ToInt64(blockLengthBuffer, 0)];
                            var blockActuallyRead = StreamToRead.Read(blockBuffer, 0, blockBuffer.Length);
                            if(blockActuallyRead < blockBuffer.Length)
                            {
                                throw new InvalidDataException("Failed to read next block, read less bytes then block occupies");
                            }
                            else
                            {
                                lastBlock = new BlockToProcess(lastBlock, BitConverter.ToInt32(blockBuffer, blockBuffer.Length - sizeof(Int32)), blockBuffer);
                                QueueToProcess.TryAdd(lastBlock, Timeout.Infinite, cancellationToken);
                            }
                        }
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
                throw new ApplicationException("Failed to read blocks from archive", e);
            }
        }
    }
}