using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class WriterToFile: Writer
    {
        public WriterToFile(SettingsProvider settings, QueueToWrite queueToWrite, Stream streamToWrite)
            : base(settings, queueToWrite, streamToWrite)
        {
        }

        protected sealed override void WriteBlock(BlockToWrite block, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                StreamToWrite.Write(block.Data, 0, block.Data.Length);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to write block to output file", e);
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
                throw new ApplicationException("Failed to write blocks set to output file", e);
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
                throw new ApplicationException("Failed to write blocks to output file", e);
            }
        }
    }
}