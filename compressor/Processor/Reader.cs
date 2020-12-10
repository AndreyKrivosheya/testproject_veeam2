using System.IO;
using System.Threading;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class Reader
    {
        public Reader(SettingsProvider settings, Stream streamToRead, QueueToProcess queueToProcess)
        {
            this.Settings = settings;
            this.StreamToRead = streamToRead;
            this.QueueToProcess = queueToProcess;
        }

        readonly protected SettingsProvider Settings;
        readonly protected Stream StreamToRead;
        readonly protected QueueToProcess QueueToProcess;

        public abstract void ReadBlocksAndQueueForProcessing(CancellationToken cancellationToken);
    }
}