namespace BookStore.Persistence.Exceptions;

/// <summary>
/// Represents invalid data detected before persistence.
/// </summary>
public class PersistenceValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceValidationException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public PersistenceValidationException(string message)
        : base(message)
    {
    }
}
