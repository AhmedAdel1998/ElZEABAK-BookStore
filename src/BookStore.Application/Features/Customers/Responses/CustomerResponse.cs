namespace BookStore.Application.Features.Customers.Responses;

/// <summary>
/// Response returned after customer mutations.
/// </summary>
public sealed class CustomerResponse
{
    /// <summary>Gets or sets customer identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets full name.</summary>
    public string FullName { get; set; } = string.Empty;
}
