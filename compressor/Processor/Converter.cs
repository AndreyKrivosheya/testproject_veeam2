using System;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class Converter
    {
        public Converter(SettingsProvider settings, QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            this.Settings = settings;
            this.QueueToProcess = queueToProcess;
            this.QueueToWrite = queueToWrite;
        }

        readonly protected SettingsProvider Settings;
        readonly protected QueueToProcess QueueToProcess;
        readonly protected QueueToWrite QueueToWrite;

        protected abstract BlockToWrite ConvertBlockToProcessToBlockToWrite(BlockToProcess block);
        
        public virtual void GetBlocksForProcessingConvertAndQueueForWriting(CancellationToken cancellationToken)
        {
            try
            {
                BlockToWrite lastBlock = null;
                while(true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    BlockToProcess block = null;
                    if(!QueueToProcess.TryTake(out block, Timeout.Infinite, cancellationToken))
                    {
                        if(QueueToProcess.IsCompleted)
                        {
                            // queue to process is completed
                            break;
                        }
                        else
                        {
                            if(!cancellationToken.IsCancellationRequested)
                            {
                                throw new InvalidOperationException("Failed to get next block for processing while queue to process is not empty");
                            }
                        }
                    }

                    QueueToWrite.TryAdd(lastBlock = ConvertBlockToProcessToBlockToWrite(block), Timeout.Infinite, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                if(lastBlock != null)
                {
                    if(lastBlock.Last)
                    {
                        QueueToWrite.CompleteAdding();
                    }
                }
            }
            catch(OperationCanceledException)
            {
                if(!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }
    }
}