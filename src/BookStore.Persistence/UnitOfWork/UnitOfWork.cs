using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BookStore.Persistence.UnitOfWork;

/// <summary>
/// Entity Framework implementation of the unit of work pattern.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly BookStoreDbContext _dbContext;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="products">The product repository.</param>
    /// <param name="categories">The category repository.</param>
    /// <param name="customers">The customer repository.</param>
    /// <param name="suppliers">The supplier repository.</param>
    /// <param name="users">The user repository.</param>
    /// <param name="roles">The role repository.</param>
    /// <param name="sales">The sale repository.</param>
    /// <param name="inventory">The inventory repository.</param>
    /// <param name="logger">The logger.</param>
    public UnitOfWork(
        BookStoreDbContext dbContext,
        IProductRepository products,
        ICategoryRepository categories,
        ICustomerRepository customers,
        ISupplierRepository suppliers,
        IUserRepository users,
        IRoleRepository roles,
        ISaleRepository sales,
        IInventoryRepository inventory,
        ILogger<UnitOfWork> logger)
    {
        _dbContext = dbContext;
        Products = products;
        Categories = categories;
        Customers = customers;
        Suppliers = suppliers;
        Users = users;
        Roles = roles;
        Sales = sales;
        Inventory = inventory;
        _logger = logger;
    }

    /// <inheritdoc />
    public IProductRepository Products { get; }

    /// <inheritdoc />
    public ICategoryRepository Categories { get; }

    /// <inheritdoc />
    public ICustomerRepository Customers { get; }

    /// <inheritdoc />
    public ISupplierRepository Suppliers { get; }

    /// <inheritdoc />
    public IUserRepository Users { get; }

    /// <inheritdoc />
    public IRoleRepository Roles { get; }

    /// <inheritdoc />
    public ISaleRepository Sales { get; }

    /// <inheritdoc />
    public IInventoryRepository Inventory { get; }

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction ??= await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database transaction commit failed");
            await RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            return;
        }

        await _currentTransaction.RollbackAsync(cancellationToken);
        await DisposeTransactionAsync();
    }

    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }
}
