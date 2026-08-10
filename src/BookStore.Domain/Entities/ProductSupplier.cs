using BookStore.Domain.Common;
using BookStore.Domain.Exceptions;

namespace BookStore.Domain.Entities;

/// <summary>
/// Connects a product to a supplier without assuming only one supplier per product.
/// </summary>
public class ProductSupplier : BaseEntity
{
    private ProductSupplier()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductSupplier"/> class.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="supplierId">The supplier identifier.</param>
    public ProductSupplier(Guid productId, Guid supplierId)
    {
        if (productId == Guid.Empty)
        {
            throw new ValidationException("Product is required.");
        }

        if (supplierId == Guid.Empty)
        {
            throw new ValidationException("Supplier is required.");
        }

        ProductId = productId;
        SupplierId = supplierId;
        IsActive = true;
    }

    /// <summary>Gets the product identifier.</summary>
    public Guid ProductId { get; private set; }

    /// <summary>Gets the product.</summary>
    public Product? Product { get; private set; }

    /// <summary>Gets the supplier identifier.</summary>
    public Guid SupplierId { get; private set; }

    /// <summary>Gets the supplier.</summary>
    public Supplier? Supplier { get; private set; }

    /// <summary>Gets supplier-specific product code reserved for purchasing.</summary>
    public string? SupplierSku { get; private set; }

    /// <summary>Gets a value indicating whether this association is preferred.</summary>
    public bool IsPreferred { get; private set; }

    /// <summary>Gets a value indicating whether the association is active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Updates optional supplier product metadata.</summary>
    public void UpdateMetadata(string? supplierSku, bool isPreferred, bool isActive)
    {
        SupplierSku = string.IsNullOrWhiteSpace(supplierSku) ? null : supplierSku.Trim();
        IsPreferred = isPreferred;
        IsActive = isActive;
        MarkUpdated();
    }
}
