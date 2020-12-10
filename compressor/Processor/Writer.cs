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
            foreach(var block in blocks)
            {
                if(cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                WriteBlock(block, cancellationToken);
            }
            StreamToWrite.Flush();
        }

        public virtual void GetBlocksForWritingAndWrite(CancellationToken cancellationToken)
        {
            while(!cancellationToken.IsCancellationRequested)
            {
                var blocks = new LinkedList<BlockToWrite>();
                while(blocks.Count < Settings.MaxBlocksToWriteAtOnce && !cancellationToken.IsCancellationRequested)
                {
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

                if(!cancellationToken.IsCancellationRequested)
                {
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
        }
    }
}