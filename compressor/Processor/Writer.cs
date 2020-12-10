using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class Writer
    {
        public Writer(SettingsProvider settings, QueueToWrite queueToWrite, Stream streamToWrite)
        {
            this.Settings = settings;
            this.QueueToWrite = queueToWrite;
            this.StreamToWrite = streamToWrite;
        }

        readonly protected SettingsProvider Settings;
        readonly protected QueueToWrite QueueToWrite;
        readonly protected Stream StreamToWrite;

        protected abstract void WriteBlock(BlockToWrite block, CancellationToken cancellationToken);
        
        protected virtual void WriteBlocks(IEnumerable<BlockToWrite> blocks, CancellationToken cancellationToken)
        {
            try
            {
                foreach(var block in blocks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WriteBlock(block, cancellationToken);
                }
                StreamToWrite.Flush();
            }
            catch(OperationCanceledException)
            {
                if(!cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        public virtual void GetBlocksForWritingAndWrite(CancellationToken cancellationToken)
        {
            try
            {
                while(true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var blocks = new LinkedList<BlockToWrite>();
                    while(blocks.Count < Settings.MaxBlocksToWriteAtOnce)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        BlockToWrite block;
                        if(!QueueToWrite.TryTake(out block, Timeout.Infinite, cancellationToken))
                        {
                            if(QueueToWrite.IsCompleted)
                            {
                                // queue to write is completed
                                break;
                            }
                            else
                            {
                                if(!cancellationToken.IsCancellationRequested)
                                {
                                    throw new InvalidOperationException("Failed to get next block for writting while queue to write is not empty");
                                }
                            }
                        }
                        else
                        {
                            blocks.AddLast(block);
                        }
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if(blocks.Count < 1)
                    {
                        // all blocks are written
                        break;
                    }
                    else
                    {
                        WriteBlocks(blocks, cancellationToken);
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