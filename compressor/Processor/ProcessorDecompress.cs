using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.ExceptionServices;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ProcessorDecompress: Processor
    {
        public ProcessorDecompress(SettingsProvider settings)
            : base(settings)
        {
        }
         
        protected sealed override Reader CreateReader(Stream input, QueueToProcess queueToProcess)
        {
            return new ReaderFromArchive(Settings, input, queueToProcess);
        }
        protected sealed override Converter CreateConverter(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            return new ConverterDecompress(Settings, queueToProcess, queueToWrite);
        }
        protected sealed override Writer CreateWriter(QueueToWrite queueToWrite, Stream output)
        {
            return new WriterToFile(Settings, queueToWrite, output);
        }

        public sealed override void Process(Stream input, Stream output)
        {
            try
            {
                base.Process(input, output);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to decompress", e);
            }
        }
    };
}