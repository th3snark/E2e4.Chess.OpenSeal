using System.Threading;
using E2e4.Chess.OpenSeal.Helpers;
using Xunit;

namespace E2e4.Chess.OpenSeal.Test;

/// <summary>
/// The timeseal timestamp is (seconds % 10000) * 1000 + milliseconds, taken from a Windows
/// file time (100 ns ticks since 1601). These pin down its edges.
/// </summary>
public class TimeHelperBoundaryUnitTest
{
    /// <summary>The file time of 1970-01-01T00:00:00Z.</summary>
    private const long UnixEpoch = 116_444_736_000_000_000L;

    private const long TicksPerSecond = 10_000_000L;

    [Fact]
    public void TheUnixEpochIsTimestampZero()
    {
        Assert.Equal(0, TimeHelper.GetTimestamp(UnixEpoch));
    }

    [Fact]
    public void TimeOfDaySplitsIntoSecondsAndMicroseconds()
    {
        TimeHelper.GetTimeOfDay(out long seconds, out long microSeconds, UnixEpoch + (1234 * TicksPerSecond) + 5678 * 10);

        Assert.Equal(1234, seconds);
        Assert.Equal(5678, microSeconds);
    }

    [Fact]
    public void TheTimestampWrapsEveryTenThousandSeconds()
    {
        Assert.Equal(9_999_000, TimeHelper.GetTimestamp(UnixEpoch + 9999 * TicksPerSecond));
        Assert.Equal(0, TimeHelper.GetTimestamp(UnixEpoch + 10_000 * TicksPerSecond));
        Assert.Equal(1000, TimeHelper.GetTimestamp(UnixEpoch + 10_001 * TicksPerSecond));
    }

    [Fact]
    public void TheLastMillisecondBeforeTheWrapIsTheLargestTimestamp()
    {
        long justBefore = UnixEpoch + 9999 * TicksPerSecond + 999 * 10_000;

        Assert.Equal(9_999_999, TimeHelper.GetTimestamp(justBefore));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(9_999, 0)]          // 0.9999 ms of ticks: still millisecond zero
    [InlineData(10_000, 1)]         // exactly 1 ms
    [InlineData(19_999, 1)]
    [InlineData(5_000_000, 500)]
    public void SubMillisecondTicksTruncateTowardZero(long extraTicks, long expectedMilliseconds)
    {
        Assert.Equal(expectedMilliseconds, TimeHelper.GetTimestamp(UnixEpoch + extraTicks));
    }

    [Fact]
    public void TwoSystemTimesAMillisecondApartGiveConsecutiveTimestamps()
    {
        long first = TimeHelper.GetTimestamp(UnixEpoch + 42 * TicksPerSecond);
        long second = TimeHelper.GetTimestamp(UnixEpoch + 42 * TicksPerSecond + 10_000);

        Assert.Equal(first + 1, second);
    }

    [Fact]
    public void TheLiveClockMovesForwardBetweenCalls()
    {
        long first = TimeHelper.GetTimestamp();
        Thread.Sleep(20);
        long second = TimeHelper.GetTimestamp();

        // Allow for the 10000-second wrap happening between the two calls.
        long elapsed = (second - first + 10_000_000) % 10_000_000;
        Assert.InRange(elapsed, 1, 5_000);
    }
}
