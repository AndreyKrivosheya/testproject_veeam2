using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ConverterDecompress: Converter
    {
        public ConverterDecompress(SettingsProvider settings, QueueToProcess queueToProcess, QueueToWrite queueToWrite)
            : base(settings, queueToProcess, queueToWrite)
        {
        }

        protected byte[] ConvertBlockData(byte[] data)
        {
            try
            {
                using(var inStream = new GZipStream(new MemoryStream(Enumerable.Concat(GZipStreamHelper.Header, data).ToArray()), CompressionMode.Decompress))
                {
                    using(var outStream = new MemoryStream(BitConverter.ToInt32(data, data.Length - sizeof(Int32))))
                    {
                        inStream.CopyTo(outStream);
                        return outStream.ToArray();
                    }
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to decompress block", e);
            }
        }

        protected sealed override BlockToWrite ConvertBlockToProcessToBlockToWrite(BlockToProcess block)
        {
            var dataConverted = ConvertBlockData(block.Data);
            if(dataConverted.Length != block.OriginalLength)
            {
                throw new ApplicationException("Failed to decompress block: decompressed size does not match with original one");
            }
            else
            {
                return new BlockToWrite(block, dataConverted);
            }
        }

        public sealed override void GetBlocksForProcessingConvertAndQueueForWriting(CancellationToken cancellationToken)
        {
            try
            {
                base.GetBlocksForProcessingConvertAndQueueForWriting(cancellationToken);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to decompress archive block", e);
            }
        }
    }
}