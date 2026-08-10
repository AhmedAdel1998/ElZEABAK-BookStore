namespace BookStore.Shared.Results;

/// <summary>
/// Represents a validation failure for a specific property.
/// </summary>
/// <param name="PropertyName">The property that failed validation.</param>
/// <param name="Message">The validation message.</param>
public sealed record ValidationError(string PropertyName, string Message);
