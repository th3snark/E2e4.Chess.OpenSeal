using System;

namespace E2e4.Chess.OpenSeal.Helpers;

/// <summary>
/// Produces the timestamp openseal embeds in every message, from a Windows file time (100 ns
/// ticks since 1601-01-01 UTC), mirroring the C reference's gettimeofday emulation.
/// </summary>
internal static class TimeHelper
{
    private const long TicksPerMicrosecond = 10;
    private const long MicrosecondsPerSecond = 1000000;

    private static readonly long FileTimeEpochTicks = new DateTime(1601, 1, 1).Ticks;
    private static readonly long UnixEpochTicks = new DateTime(1970, 1, 1).Ticks;

    /// <summary>
    /// Splits a file time into whole seconds and the remaining microseconds since the Unix epoch.
    /// </summary>
    /// <param name="seconds">Whole seconds since 1970-01-01 UTC.</param>
    /// <param name="microSeconds">The microseconds past <paramref name="seconds"/>.</param>
    /// <param name="systemTime">A Windows file time, or 0 for now.</param>
    public static void GetTimeOfDay(out long seconds, out long microSeconds, long systemTime = 0)
    {
        long fileTime = systemTime != 0 ? systemTime : DateTime.UtcNow.Ticks - FileTimeEpochTicks;
        long unixTicks = fileTime - (UnixEpochTicks - FileTimeEpochTicks);
        long unixMicroseconds = unixTicks / TicksPerMicrosecond;

        seconds = unixMicroseconds / MicrosecondsPerSecond;
        microSeconds = unixMicroseconds % MicrosecondsPerSecond;
    }

    /// <summary>
    /// The timestamp in the format 'ssssuuu': the last four digits of the seconds followed by
    /// three digits of milliseconds.
    /// </summary>
    /// <param name="systemTime">A Windows file time, or 0 for now.</param>
    public static long GetTimestamp(long systemTime = 0)
    {
        GetTimeOfDay(out long seconds, out long microSeconds, systemTime);
        return (seconds % 10000) * 1000 + microSeconds / 1000;
    }
}
