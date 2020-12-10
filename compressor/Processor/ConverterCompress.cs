using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ConverterCompress: Converter
    {
        public ConverterCompress(SettingsProvider settings, QueueToProcess queueToProcess, QueueToWrite queueToWrite)
            : base(settings, queueToProcess, queueToWrite)
        {
        }

        protected byte[] ConvertBlockData(byte[] data)
        {
            try
            {
                using(var outStreamRaw = new MemoryStream())
                {
                    using(var outStreamRawGz = new GZipStream(outStreamRaw, CompressionLevel.Optimal, true))
                    {
                        using(var inStream = new MemoryStream(data))
                        {
                            inStream.CopyTo(outStreamRawGz);
                        }
                        outStreamRawGz.Flush();
                    }

                    if(outStreamRaw.Length >= GZipStreamHelper.Header.Length)
                    {
                        if(outStreamRaw.Length == GZipStreamHelper.Header.Length)
                        {
                            return new byte[] { };
                        }
                        else
                        {
                            // all bytes but the header
                            return outStreamRaw.ToArray().Skip(GZipStreamHelper.Header.Length).ToArray();
                        }
                    }
                    else
                    {
                        throw new ApplicationException("Failed to compress block: compressed block size is less than header size");
                    }
                }
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to compress block", e);
            }
        }

        protected sealed override BlockToWrite ConvertBlockToProcessToBlockToWrite(BlockToProcess block)
        {
            return new BlockToWrite(block, ConvertBlockData(block.Data));
        }

        public sealed override void GetBlocksForProcessingConvertAndQueueForWriting(CancellationToken cancellationToken)
        {
            try
            {
                base.GetBlocksForProcessingConvertAndQueueForWriting(cancellationToken);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to compress input file block", e);
            }
        }
    }
}