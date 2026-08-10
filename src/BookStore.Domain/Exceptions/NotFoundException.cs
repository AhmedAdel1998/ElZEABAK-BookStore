namespace BookStore.Domain.Exceptions;

/// <summary>
/// Represents a missing domain object.
/// </summary>
public class NotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public NotFoundException(string message)
        : base(message)
    {
    }
}
