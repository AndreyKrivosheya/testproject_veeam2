using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class WriterToArchive: Writer
    {
        public WriterToArchive(SettingsProvider settings, QueueToWrite queueToWrite, Stream streamToWrite)
            : base(settings, queueToWrite, streamToWrite)
        {
        }

        protected sealed override void WriteBlock(BlockToWrite block, CancellationToken cancellationToken)
        {
            // write block data length
            if(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var blockLengthBuffer = BitConverter.GetBytes((Int64)block.Data.Length);
                    StreamToWrite.Write(blockLengthBuffer, 0, blockLengthBuffer.Length);                
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to write block length to archive", e);
                }
            }
            // write block data
            if(!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    StreamToWrite.Write(block.Data, 0, block.Data.Length);
                }
                catch(Exception e)
                {
                    throw new ApplicationException("Failed to write block to archive", e);
                }
            }
        }
        
        protected sealed override void WriteBlocks(IEnumerable<BlockToWrite> blocks, CancellationToken cancellationToken)
        {
            try
            {
                base.WriteBlocks(blocks, cancellationToken);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to write blocks set to archive", e);
            }
        }

        public sealed override void GetBlocksForWritingAndWrite(CancellationToken cancellationToken)
        {
            try
            {
                base.GetBlocksForWritingAndWrite(cancellationToken);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to write blocks to archive", e);
            }
        }
    }
}