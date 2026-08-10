namespace BookStore.UI.Validation;

/// <summary>
/// Represents a single validation summary item.
/// </summary>
/// <param name="PropertyName">The invalid property name.</param>
/// <param name="Message">The validation message.</param>
public sealed record ValidationSummaryItem(string PropertyName, string Message);
