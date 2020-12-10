namespace compressor.Processor.Settings
{
    interface SettingsProvider
    {
        int MaxConcurrency { get; }

        int MaxQueueSize { get; }

        long BlockSize { get; }

        int MaxBlocksToWriteAtOnce { get; }
    }
}