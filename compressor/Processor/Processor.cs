using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Runtime.ExceptionServices;

using compressor.Processor.Queue;
using compressor.Processor.Settings;

namespace compressor.Processor
{
    abstract class Processor
    {
        public Processor(SettingsProvider settings)
        {
            this.Settings = settings;
        }
         
        protected readonly SettingsProvider Settings;

        protected abstract Reader CreateReader(Stream input, QueueToProcess queueToProcess);
        protected abstract Converter CreateConverter(QueueToProcess queueToProcess, QueueToWrite queueToWrite);
        protected abstract Writer CreateWriter(QueueToWrite queueToWrite, Stream output);

        public virtual void Process(Stream input, Stream output)
        {
            using(var queueToWrite = new QueueToWrite(Settings.MaxQueueSize))
            {
                using(var queueToProcess = new QueueToProcess(Settings.MaxQueueSize))
                {
                    using(var cancellationTokenSoureReader = new CancellationTokenSource())
                    {
                        using(var cancellationTokenSourceWriter = new CancellationTokenSource())
                        {
                            using(var cancellationTokenSourceConverters = new CancellationTokenSource())
                            {
                                var concurrency = Math.Max(Settings.MaxConcurrency, 3);
                                var threads = new Thread[concurrency];
                                var threadsFinishedEvents = new ManualResetEvent[threads.Length];
                                var threadsErrors = new ExceptionDispatchInfo[threads.Length];
                                for(int i = 0; i < threads.Length; ++i)
                                {
                                    threadsFinishedEvents[i] = new ManualResetEvent(false);
                                    threads[i] = new Thread((object param) => {
                                        var index = (int)param;
                                        try
                                        {
                                            switch(index)
                                            {
                                                case 0:
                                                    var
                                                    reader = CreateReader(input, queueToProcess);
                                                    reader.ReadBlocksAndQueueForProcessing(cancellationTokenSoureReader.Token);
                                                    break;
                                                case 1:
                                                    var
                                                    writer = CreateWriter(queueToWrite, output);
                                                    writer.GetBlocksForWritingAndWrite(cancellationTokenSourceWriter.Token);
                                                    break;
                                                default:
                                                    var
                                                    converter = CreateConverter(queueToProcess, queueToWrite);
                                                    converter.GetBlocksForProcessingConvertAndQueueForWriting(cancellationTokenSourceConverters.Token);
                                                    break;
                                            }
                                        }
                                        catch(Exception e)
                                        {
                                            threadsErrors[index] = ExceptionDispatchInfo.Capture(e);
                                        }
                                        finally
                                        {
                                            threadsFinishedEvents[index].Set();
                                        }
                                    });
                                    threads[i].Start(i);
                                }
                                // wait all finished
                                while(true)
                                {
                                    var awaitables = threadsFinishedEvents.Select((e, i) => Tuple.Create(e, i)).Where(x => x.Item1 != null).ToArray();
                                    if(awaitables.Length == 0)
                                    {
                                        // all finished
                                        break;
                                    }
                                    else
                                    {
                                        // wait for any to finish
                                        var justFinished = WaitHandle.WaitAny(awaitables.Select(x => x.Item1).ToArray(), Timeout.Infinite);
                                        var justFinishedAwaitableIndex = awaitables[justFinished].Item2;
                                        // clean up sync event
                                        threadsFinishedEvents[justFinishedAwaitableIndex].Close();
                                        threadsFinishedEvents[justFinishedAwaitableIndex] = null;
                                        // handle finish with error
                                        if(threadsErrors[justFinishedAwaitableIndex] != null)
                                        {
                                            switch(justFinishedAwaitableIndex)
                                            {
                                                // reader failed
                                                case 0:
                                                // writer failed
                                                case 1:
                                                    {
                                                        // cancel converters
                                                        cancellationTokenSourceConverters.Cancel();
                                                        // cancel writer/readee
                                                        switch(justFinishedAwaitableIndex)
                                                        {
                                                            case 0:
                                                                cancellationTokenSourceWriter.Cancel();
                                                                break;
                                                            default:
                                                                cancellationTokenSoureReader.Cancel();
                                                                break;
                                                        }
                                                        // wait canceled to finish
                                                        WaitHandle.WaitAll(awaitables.Where(x => x.Item2 != justFinishedAwaitableIndex).Select(x => x.Item1).ToArray(), Timeout.Infinite);
                                                        // throw
                                                        threadsErrors[justFinishedAwaitableIndex].Throw();
                                                    }
                                                    break;
                                                // converter failed
                                                default:
                                                    {
                                                        // if no converters left running
                                                        if(!awaitables.Any(x => x.Item2 > 2 && x.Item2 != justFinishedAwaitableIndex))
                                                        {
                                                            // cancle reader
                                                            cancellationTokenSoureReader.Cancel();
                                                            // cancel writer
                                                            cancellationTokenSourceWriter.Cancel();
                                                        }
                                                    }
                                                    break;
                                            }
                                        }
                                    }
                                }
                                // process errors if any
                                var errors = threadsErrors.Where(x => x != null);
                                if(errors.Any())
                                {
                                    throw new AggregateException(errors.Select(x => x.SourceException));
                                }
                            }
                        }
                    }
                }
            }
        }
    };
}