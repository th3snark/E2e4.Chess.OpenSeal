using E2e4.Chess.OpenSeal.Helpers;
using Xunit;

namespace E2e4.Chess.OpenSeal.Test;

public class TimeHelperUnitTest
{
    [Fact]
    public void GetTimestampForFixedSystemTimeReturnsExpectedSevenDigitValue()
    {
        Assert.Equal(OpenSealTestData.Timestamp1, TimeHelper.GetTimestamp(OpenSealTestData.SystemTime1));
    }

    [Fact]
    public void GetTimeOfDayForFixedSystemTimeIsConsistentWithGetTimestamp()
    {
        TimeHelper.GetTimeOfDay(out long seconds, out long microSeconds, OpenSealTestData.SystemTime1);

        Assert.Equal(OpenSealTestData.Timestamp1, (seconds % 10000) * 1000 + microSeconds / 1000);
    }

    [Fact]
    public void GetTimestampWithDefaultSystemTimeReturnsValueInSevenDigitRange()
    {
        long timestamp = TimeHelper.GetTimestamp();

        Assert.InRange(timestamp, 0, 9_999_999);
    }
}
