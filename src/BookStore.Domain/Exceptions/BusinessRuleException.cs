namespace BookStore.Domain.Exceptions;

/// <summary>
/// Represents a violated business rule.
/// </summary>
public class BusinessRuleException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BusinessRuleException"/> class.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public BusinessRuleException(string message)
        : base(message)
    {
    }
}
