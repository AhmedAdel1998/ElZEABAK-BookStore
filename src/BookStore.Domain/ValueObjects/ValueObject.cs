namespace BookStore.Domain.ValueObjects;

/// <summary>
/// Base class for immutable value objects.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Gets the atomic values used for equality comparison.
    /// </summary>
    /// <returns>The value object's equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is ValueObject other
            && GetType() == other.GetType()
            && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(1, (current, component) => HashCode.Combine(current, component));
    }
}
