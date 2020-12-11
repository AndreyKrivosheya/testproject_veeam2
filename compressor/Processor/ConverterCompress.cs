using System;
using System.IO;
using System.IO.Compression;
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
                            var compressedOriginal = outStreamRaw.ToArray();
                            var compressedHeaderStripped = new byte[compressedOriginal.Length - GZipStreamHelper.Header.Length];
                            Array.Copy(compressedOriginal, GZipStreamHelper.Header.Length, compressedHeaderStripped, 0, compressedHeaderStripped.Length);
                            return compressedHeaderStripped;
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