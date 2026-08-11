namespace BookStore.Application.Features.Suppliers.Responses;

/// <summary>
/// Supplier command response.
/// </summary>
public sealed class SupplierResponse
{
    /// <summary>Gets or sets supplier identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets company name.</summary>
    public string CompanyName { get; set; } = string.Empty;
    /// <summary>Gets or sets active state.</summary>
    public bool IsActive { get; set; }
}
