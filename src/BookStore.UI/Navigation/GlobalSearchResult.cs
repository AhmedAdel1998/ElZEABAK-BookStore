namespace BookStore.UI.Navigation;

/// <summary>
/// One row in the shell's global search dropdown.
/// </summary>
public sealed class GlobalSearchResult
{
    /// <summary>Gets the localized group this row belongs to (page, product, customer).</summary>
    public required string Group { get; init; }

    /// <summary>Gets the row's main label.</summary>
    public required string PrimaryText { get; init; }

    /// <summary>Gets supporting detail such as a barcode or a phone number.</summary>
    public string? SecondaryText { get; init; }

    /// <summary>
    /// Gets the navigation this row performs when it is chosen. Each result carries its own
    /// action because the three result kinds land on unrelated pages.
    /// </summary>
    public required Func<Task> Open { get; init; }

    /// <summary>Gets the single line of supporting text shown under <see cref="PrimaryText"/>.</summary>
    public string Caption => string.IsNullOrWhiteSpace(SecondaryText) ? Group : $"{Group}  •  {SecondaryText}";
}
