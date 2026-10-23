namespace Tallix.Domain.Ledger;

public enum TransactionType
{
    Lent,
    Borrowed,
    PaymentReceived,
    PaymentMade,
    Adjustment
}