namespace BookStore.Shared.Constants;

/// <summary>
/// Contains point-of-sale limits.
/// </summary>
public static class PosConstants
{
    /// <summary>
    /// The largest number of invoices a workstation may keep open at the same time.
    /// </summary>
    /// <remarks>
    /// Bounded on purpose. Every open invoice reserves stock away from the others, so an unbounded
    /// strip of forgotten carts would make products look sold out while nothing was actually sold.
    /// </remarks>
    public const int MaxOpenInvoices = 10;
}
