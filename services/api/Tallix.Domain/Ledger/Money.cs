using Tallix.Domain.Common;

namespace Tallix.Domain.Ledger;

public enum Currency
{
    PHP
}

public readonly record struct Money
{
    public decimal Amount { get; }
    public Currency Currency { get; }

    public Money(decimal amount, Currency currency)
    {
        if (decimal.Round(amount, 2) != amount)
        {
            throw new DomainException("Amount cannot have more than 2 decimal places.");
        }

        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(Currency currency) => new(0m, currency);

    public static Money Php(decimal amount) => new(amount, Currency.PHP);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Negate() => new(-Amount, Currency);

    public override string ToString() => $"{Currency} {Amount:N2}";

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException(
                $"Cannot combine {Currency} with {other.Currency}.");
        }
    }
}