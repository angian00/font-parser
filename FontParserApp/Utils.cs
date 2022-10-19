namespace FontParserApp
{
    public class Utils
    {

        public static string BytesToStr(byte[] buf, UInt32 start, UInt32 len)
        {
            return System.Text.Encoding.Latin1.GetString(buf, (int)start, (int)len);
        }


        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private static long MacEpochTicks = (new DateTime(1904, 12, 1)).Ticks;


        // classic Mac (Apple) epoch: seconds from 1 January 1904
        public static DateTime DateTimeFromMacTimestamp(long macSeconds)
        {
            return new DateTime(MacEpochTicks + macSeconds * TicksPerSecond);
        }
    }
}