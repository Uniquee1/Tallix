namespace Tallix.Application.Common;

/// <summary>
/// The request is valid, but conflicts with existing data
/// (for example, an email already owned by another account).
/// The API will translate this into HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}