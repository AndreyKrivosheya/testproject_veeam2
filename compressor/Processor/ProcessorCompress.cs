using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.ExceptionServices;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    class ProcessorCompress: Processor
    {
        public ProcessorCompress(SettingsProvider settings)
            : base(settings)
        {
        }
         
        protected sealed override Reader CreateReader(Stream input, QueueToProcess queueToProcess)
        {
            return new ReaderFromFile(Settings, input, queueToProcess);
        }
        protected sealed override Converter CreateConverter(QueueToProcess queueToProcess, QueueToWrite queueToWrite)
        {
            return new ConverterCompress(Settings, queueToProcess, queueToWrite);
        }
        protected sealed override Writer CreateWriter(QueueToWrite queueToWrite, Stream output)
        {
            return new WriterToArchive(Settings, queueToWrite, output);
        }

        public sealed override void Process(Stream input, Stream output)
        {
            try
            {
                base.Process(input, output);
            }
            catch(Exception e)
            {
                throw new ApplicationException("Failed to compress", e);
            }
        }
    };
}