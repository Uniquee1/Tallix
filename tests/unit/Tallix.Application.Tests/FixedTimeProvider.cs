namespace Tallix.Application.Tests;

internal sealed class FixedTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; }

    public FixedTimeProvider(DateTimeOffset now)
    {
        Now = now;
    }

    public override DateTimeOffset GetUtcNow() => Now;
}