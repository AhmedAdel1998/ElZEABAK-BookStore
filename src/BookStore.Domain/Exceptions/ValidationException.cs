namespace BookStore.Domain.Exceptions;

/// <summary>
/// Represents invalid domain input.
/// </summary>
public class ValidationException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public ValidationException(string message)
        : base(message)
    {
    }
}
