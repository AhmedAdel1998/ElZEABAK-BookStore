using BookStore.Application.Features.Sales.DTOs;
using BookStore.Application.Interfaces;
using BookStore.Domain.Interfaces;

namespace BookStore.Application.Features.Sales.Services;

/// <summary>
/// Resolves POS cart stock against the product table and the other open invoices.
/// </summary>
public sealed class PosCartStockService : IPosCartStockService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPosSaleSessionStore _sessionStore;

    /// <summary>Initializes a new instance of the <see cref="PosCartStockService"/> class.</summary>
    public PosCartStockService(IUnitOfWork unitOfWork, IPosSaleSessionStore sessionStore)
    {
        _unitOfWork = unitOfWork;
        _sessionStore = sessionStore;
    }

    /// <inheritdoc />
    public async Task<int> GetAvailableAsync(Guid productId, Guid saleId, CancellationToken cancellationToken = default)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId, cancellationToken);
        if (product is null || !product.IsActive)
        {
            return 0;
        }

        var reserved = await _sessionStore.GetReservedQuantityAsync(productId, saleId, cancellationToken);
        return Math.Max(product.Quantity - reserved, 0);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<PosCartAdjustmentDto>> SynchronizeAsync(SaleSessionDto sale, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);

        var adjustments = new List<PosCartAdjustmentDto>();
        var dropped = new List<SaleCartItemDto>();

        foreach (var item in sale.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null || !product.IsActive)
            {
                dropped.Add(item);
                adjustments.Add(new PosCartAdjustmentDto
                {
                    ProductId = item.ProductId,
                    Title = item.Title,
                    PreviousQuantity = item.Quantity,
                    NewQuantity = 0,
                    Reason = PosCartAdjustmentReason.ProductUnavailable
                });
                continue;
            }

            // Everything the other open invoices hold is off limits, so two tabs cannot both sell the
            // last copy of a book.
            var reserved = await _sessionStore.GetReservedQuantityAsync(item.ProductId, sale.SaleId, cancellationToken);
            var available = Math.Max(product.Quantity - reserved, 0);

            item.Title = product.Title;
            item.Barcode = product.Barcode.Value;
            item.CategoryName = product.Category?.Name ?? item.CategoryName;
            item.UnitPrice = product.SellingPrice;

            if (item.Quantity <= available)
            {
                item.AvailableQuantity = available - item.Quantity;
                continue;
            }

            var previousQuantity = item.Quantity;
            if (available <= 0)
            {
                dropped.Add(item);
                adjustments.Add(new PosCartAdjustmentDto
                {
                    ProductId = item.ProductId,
                    Title = product.Title,
                    PreviousQuantity = previousQuantity,
                    NewQuantity = 0,
                    Reason = PosCartAdjustmentReason.LineRemoved
                });
                continue;
            }

            item.Quantity = available;
            item.AvailableQuantity = 0;
            item.Discount = Math.Min(item.Discount, item.UnitPrice * item.Quantity);
            adjustments.Add(new PosCartAdjustmentDto
            {
                ProductId = item.ProductId,
                Title = product.Title,
                PreviousQuantity = previousQuantity,
                NewQuantity = available,
                Reason = PosCartAdjustmentReason.QuantityReduced
            });
        }

        foreach (var item in dropped)
        {
            sale.Items.Remove(item);
        }

        return adjustments;
    }
}
