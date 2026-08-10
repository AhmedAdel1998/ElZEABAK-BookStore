namespace BookStore.Domain.Common;

/// <summary>
/// Represents a domain object with a stable identity.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the unique entity identifier.
    /// </summary>
    Guid Id { get; }
}
