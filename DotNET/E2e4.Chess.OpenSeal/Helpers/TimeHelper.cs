using System;

namespace E2e4.Chess.OpenSeal.Helpers
{
    public class TimeHelper
    {
        public static void GetTimeOfDay(out long seconds, out long microSeconds, long systemTime = 0)
        {
            DateTime fileTime = new DateTime(1601, 1, 1);
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            TimeSpan deltaEpoch = unixEpoch - fileTime;

            long tmpSystemTime = systemTime;
            if (tmpSystemTime == 0)
            {
                TimeSpan deltaUtcNow = DateTime.UtcNow - unixEpoch;
                tmpSystemTime = deltaUtcNow.Ticks + deltaEpoch.Ticks;
            }

            tmpSystemTime -= deltaEpoch.Ticks;
            tmpSystemTime /= 10;

            seconds = tmpSystemTime / 1000000;
            microSeconds = tmpSystemTime % 1000000;
        }

        public static long GetTimestamp(long systemTime = 0)
        {
            long tv_sec;
            long tv_usec;
            TimeHelper.GetTimeOfDay(out tv_sec, out tv_usec, systemTime);

            //Calculate the timestamp which is in the format 'ssssuuu' where s=seconds and u=micro second
            return (tv_sec % 10000) * 1000 + tv_usec / 1000;
        }
    }
}
