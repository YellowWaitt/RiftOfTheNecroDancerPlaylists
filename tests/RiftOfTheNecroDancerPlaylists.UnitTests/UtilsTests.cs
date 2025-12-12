namespace RiftOfTheNecroDancerPlaylists.UnitTests;

public class UtilsTests
{
    [Theory]
    [InlineData("", 0, 0, 0)]
    [InlineData("1", 0, 0, 1)]
    [InlineData("01", 0, 0, 1)]
    [InlineData("12", 0, 0, 12)]
    [InlineData("123", 0, 2, 3)]
    [InlineData("1:23", 0, 1, 23)]
    [InlineData("01:23", 0, 1, 23)]
    [InlineData("01:03", 0, 1, 3)]
    [InlineData("12:34", 0, 12, 34)]
    [InlineData("12:04", 0, 12, 4)]
    [InlineData("123:45", 2, 3, 45)]
    [InlineData("1:23:45", 1, 23, 45)]
    [InlineData("01:23:45", 1, 23, 45)]
    [InlineData("01:03:45", 1, 3, 45)]
    [InlineData("01:23:05", 1, 23, 5)]
    [InlineData("01:03:05", 1, 3, 5)]
    [InlineData("12:34:56", 12, 34, 56)]
    [InlineData("712:34:56", 712, 34, 56)]
    [InlineData("123:456:789", 130, 49, 9)]  // After all, why not ?
    public void ParseDuration_OnDuration_ReturnTimeSpan(string duration, Int32 hours, Int32 minutes, Int32 seconds)
    {
        var timeSpan = Utils.ParseDuration(duration);
        var excepted = new TimeSpan(hours, minutes, seconds);

        Assert.Equal(excepted, timeSpan);
    }
}
