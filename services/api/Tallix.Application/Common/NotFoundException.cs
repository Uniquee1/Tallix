namespace Tallix.Application.Common;

/// <summary>
/// The requested thing doesn't exist, or the caller isn't allowed to know that it does.
/// The API translates this into HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}