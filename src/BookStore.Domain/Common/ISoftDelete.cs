namespace BookStore.Domain.Common;

/// <summary>
/// Represents a domain object that can be soft deleted.
/// </summary>
public interface ISoftDelete
{
    /// <summary>
    /// Gets a value indicating whether the object is deleted.
    /// </summary>
    bool IsDeleted { get; }
}
