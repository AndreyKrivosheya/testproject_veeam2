using System;
using System.IO;
using System.Linq;

using compressor.Processor;
using compressor.Processor.Settings;

namespace compressor
{
    class Program
    {
        static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("compressor.exe compress|decompress in out");
        }

        static int RunProcessor(string pathIn, string pathOut, Func<SettingsProvider, Processor.Processor> processorFactory)
        {
            var
            stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            try
            {
                using(var inStream = new FileStream(pathIn, FileMode.Open))
                {
                    Stream outStream = null;
                    try
                    {
                        try
                        {
                            outStream = new FileStream(pathOut, FileMode.Open);
                            outStream.SetLength(0);
                        }
                        catch(FileNotFoundException)
                        {
                            outStream = new FileStream(pathOut, FileMode.CreateNew);
                        }
                    
                        var
                        processor = processorFactory(SettingsProviderFromEnvironment.Instance);
                        processor.Process(inStream, outStream);
                    }
                    finally
                    {
                        if(outStream != null)
                        {
                            outStream.Dispose();
                            outStream = null;
                        }
                    }
                }
            }
            finally
            {
                stopWatch.Stop();
                System.Diagnostics.Debug.WriteLine("Time took: '{0}'", stopWatch.Elapsed);
            }

            return 0;
        }

        static int Main(string[] args)
        {
            try
            {
                if(args.Length < 3 || args.Length > 3)
                {
                    Usage();
                    return 1;
                }
                else
                {
                    if(string.Equals("compress", args[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            return RunProcessor(args[1], args[2], (settings) => new ProcessorCompress(settings));
                        }
                        catch(Exception e)
                        {
                            throw new ApplicationException(string.Format("Failed to compress '{0}' to '{1}'", args[1], args[2]), e);
                        }
                    }
                    else if(string.Equals("decompress", args[0], StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            return RunProcessor(args[1], args[2], (settings) => new ProcessorDecompress(settings));
                        }
                        catch(Exception e)
                        {
                            throw new ApplicationException(string.Format("Failed to decompress '{0}' to '{1}'", args[1], args[2]), e);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Command '{0}' is unknown.", args[0]);
                        Usage();
                        return 1;
                    }
                }
            }
            catch(Exception e)
            {
                string GetMessagesChain(Exception e)
                {
                    var eAsAggregate = e as AggregateException;
                    if(eAsAggregate != null)
                    {
                        if(eAsAggregate.InnerExceptions.Count > 0)
                        {
                            return string.Format("[{0}]", string.Join(", ", eAsAggregate.InnerExceptions.Select(x => string.Format("{{{0}}}", GetMessagesChain(x)))));
                        }
                        else
                        {
                            return eAsAggregate.Message;
                        }
                    }
                    else
                    {
                        if(e.InnerException != null)
                        {
                            if(!string.IsNullOrEmpty(e.Message))
                            {
                                return string.Format("{0} => {1}", e.Message, GetMessagesChain(e.InnerException));
                            }
                            else
                            {
                                return GetMessagesChain(e.InnerException);
                            }
                        }
                        else
                        {
                            return e.Message;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(e);
                Console.WriteLine(GetMessagesChain(e));
                return 0;
            }
        }
    }
}
