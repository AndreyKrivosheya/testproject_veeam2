namespace compressor.Processor
{
    static class GZipStreamHelper
    {
        public static readonly byte[] Header = new byte[] {
#if NET_CORE_3_1
            /*magic*/
            0x1f, 0x8b,
            /*deflate*/
            0x08,
            /*file type*/
            0x00,
            /*file modification time in Unix format*/
            0x00, 0x00, 0x00, 0x00
#else
            /*magic*/
            0x1f, 0x8b
#endif
        };
    }
}