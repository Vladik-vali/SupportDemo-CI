using SupportDemo;
using Xunit;

namespace SupportDemo.Tests;

public class TicketStatusesTests
{
    [Theory]
    [InlineData("New")]
    [InlineData("InProgress")]
    [InlineData("Closed")]
    public void IsValid_KnownStatus_ReturnsTrue(string status)
    {
        Assert.True(TicketStatuses.IsValid(status));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Pending")]
    [InlineData(null)]
    public void IsValid_UnknownStatus_ReturnsFalse(string? status)
    {
        Assert.False(TicketStatuses.IsValid(status));
    }
}