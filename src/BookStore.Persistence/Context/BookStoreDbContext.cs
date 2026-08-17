using System.Linq.Expressions;
using BookStore.Domain.Common;
using BookStore.Domain.Entities;
using BookStore.Persistence.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookStore.Persistence.Context;

/// <summary>
/// Entity Framework database context for the BookStore persistence model.
/// </summary>
public class BookStoreDbContext : DbContext
{
    private readonly ILogger<BookStoreDbContext>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BookStoreDbContext"/> class.
    /// </summary>
    /// <param name="options">The context options.</param>
    /// <param name="logger">The optional logger.</param>
    public BookStoreDbContext(DbContextOptions<BookStoreDbContext> options, ILogger<BookStoreDbContext>? logger = null)
        : base(options)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the categories set.
    /// </summary>
    public DbSet<Category> Categories => Set<Category>();

    /// <summary>
    /// Gets the products set.
    /// </summary>
    public DbSet<Product> Products => Set<Product>();

    /// <summary>
    /// Gets the customers set.
    /// </summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>
    /// Gets the suppliers set.
    /// </summary>
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    /// <summary>
    /// Gets the product-supplier associations set.
    /// </summary>
    public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();

    /// <summary>
    /// Gets the users set.
    /// </summary>
    public DbSet<User> Users => Set<User>();

    /// <summary>
    /// Gets the roles set.
    /// </summary>
    public DbSet<Role> Roles => Set<Role>();

    /// <summary>
    /// Gets the permissions set.
    /// </summary>
    public DbSet<Permission> Permissions => Set<Permission>();

    /// <summary>
    /// Gets the sales set.
    /// </summary>
    public DbSet<Sale> Sales => Set<Sale>();

    /// <summary>
    /// Gets the sale items set.
    /// </summary>
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    /// <summary>
    /// Gets the inventory transactions set.
    /// </summary>
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();

    /// <summary>
    /// Gets the persistent application settings set.
    /// </summary>
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    /// <summary>
    /// Gets the audit log entries set.
    /// </summary>
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditValues();
        ValidateChanges();

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(ex, "Database update failed");
            throw;
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookStoreDbContext).Assembly);
        ApplySoftDeleteQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private void ApplyAuditValues()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(entity => entity.CreatedAt).CurrentValue = now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Property(entity => entity.UpdatedAt).CurrentValue = now;
            }
        }
    }

    private void ValidateChanges()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            if (entry.Entity.Id == Guid.Empty)
            {
                throw new PersistenceValidationException($"{entry.Entity.GetType().Name} has an invalid identifier.");
            }
        }
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "entity");
                var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var comparison = Expression.Equal(property, Expression.Constant(false));
                var lambda = Expression.Lambda(comparison, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}
