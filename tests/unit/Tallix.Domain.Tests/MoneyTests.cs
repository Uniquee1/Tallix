using Tallix.Domain.Common;
using Tallix.Domain.Ledger;

namespace Tallix.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Add_ReturnsSum()
    {
        var result = Money.Php(5000m).Add(Money.Php(2000m));

        Assert.Equal(Money.Php(7000m), result);
    }

    [Fact]
    public void Subtract_CanGoNegative()
    {
        var result = Money.Php(1000m).Subtract(Money.Php(1500m));

        Assert.Equal(-500m, result.Amount);
    }

    [Fact]
    public void Constructor_RejectsMoreThanTwoDecimalPlaces()
    {
        Assert.Throws<DomainException>(() => Money.Php(10.123m));
    }

    [Theory]
    [InlineData(10.5)]
    [InlineData(10.55)]
    [InlineData(0)]
    public void Constructor_AcceptsTwoOrFewerDecimalPlaces(double value)
    {
        var money = Money.Php((decimal)value);

        Assert.Equal((decimal)value, money.Amount);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        Assert.Equal(Money.Php(100m), Money.Php(100m));
        Assert.NotEqual(Money.Php(100m), Money.Php(100.50m));
    }

    [Fact]
    public void Negate_FlipsSign()
    {
        Assert.Equal(-250m, Money.Php(250m).Negate().Amount);
    }
}