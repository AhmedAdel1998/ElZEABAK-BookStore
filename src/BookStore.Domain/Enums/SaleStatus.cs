namespace BookStore.Domain.Enums;

/// <summary>
/// Defines the lifecycle state of a sale.
/// </summary>
public enum SaleStatus
{
    /// <summary>
    /// Sale is pending.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Sale is completed.
    /// </summary>
    Completed = 1,

    /// <summary>
    /// Sale is cancelled.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// Sale is refunded.
    /// </summary>
    Refunded = 3,

    /// <summary>
    /// Sale is suspended and can be resumed.
    /// </summary>
    Suspended = 4
}
