namespace BookStore.Domain.Common;

/// <summary>
/// Represents an auditable domain object.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// Gets the date and time when the object was created.
    /// </summary>
    DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets the date and time when the object was last updated.
    /// </summary>
    DateTimeOffset? UpdatedAt { get; }
}
