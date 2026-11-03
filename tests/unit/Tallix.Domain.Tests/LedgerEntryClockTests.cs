using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class LedgerEntryClockTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(-3650)] // ten years ago: backdating is a feature
    [InlineData(0)]
    [InlineData(3600)]  // an hour ahead
    [InlineData(86400)] // exactly 24 hours ahead: still allowed
    public void TimesUpToADayAhead_AreAccepted(int secondsFromNow)
    {
        var when = secondsFromNow == -3650 ? Now.AddDays(-3650) : Now.AddSeconds(secondsFromNow);

        LedgerEntry.EnsureNotTooFarInFuture(when, Now);
    }

    [Theory]
    [InlineData(86401)]
    [InlineData(172800)]
    public void TimesMoreThanADayAhead_AreRejected(int secondsFromNow)
    {
        var error = Assert.Throws<DomainException>(() =>
            LedgerEntry.EnsureNotTooFarInFuture(Now.AddSeconds(secondsFromNow), Now));

        Assert.Contains("clock", error.Message);
    }
}