using BookStore.Domain.Entities;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BookStore.Persistence.Seed;

/// <summary>
/// Seeds required default data.
/// </summary>
public class DatabaseSeeder
{
    private const string AdminPasswordHash = "$2a$11$4cIiq8n2V7b7WqtddnMP7uWuBphrvLePAUQV1NQFBnnbaYbWYpJ26";
    private readonly BookStoreDbContext _dbContext;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSeeder"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseSeeder(BookStoreDbContext dbContext, ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database when required.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedRolesAndPermissionsAsync(cancellationToken);
        await SeedCategoriesAsync(cancellationToken);
        await SeedAdminUserAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Database seed completed");
    }

    private async Task SeedRolesAndPermissionsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        var permissions = new[]
        {
            new Permission(PermissionConstants.ProductView, "View products"),
            new Permission(PermissionConstants.ProductCreate, "Create products"),
            new Permission(PermissionConstants.ProductEdit, "Edit products"),
            new Permission(PermissionConstants.ProductDelete, "Delete products"),
            new Permission(PermissionConstants.CategoryView, "View categories"),
            new Permission(PermissionConstants.CategoryCreate, "Create categories"),
            new Permission(PermissionConstants.CategoryEdit, "Edit categories"),
            new Permission(PermissionConstants.CategoryDelete, "Delete categories"),
            new Permission(PermissionConstants.SalesCreate, "Create sales"),
            new Permission(PermissionConstants.SalesCancel, "Cancel sales"),
            new Permission(PermissionConstants.InventoryView, "View inventory"),
            new Permission(PermissionConstants.InventoryEdit, "Edit inventory"),
            new Permission(PermissionConstants.CustomerView, "View customers"),
            new Permission(PermissionConstants.SupplierView, "View suppliers"),
            new Permission(PermissionConstants.ReportsView, "View reports"),
            new Permission(PermissionConstants.SettingsView, "View settings"),
            new Permission(PermissionConstants.UsersManage, "Manage users"),
            new Permission(PermissionConstants.RolesManage, "Manage roles"),
            new Permission(PermissionConstants.BackupDatabase, "Back up database"),
            new Permission(PermissionConstants.RestoreDatabase, "Restore database")
        };

        var administrator = new Role("Administrator", "Full system access");
        var manager = new Role("Manager", "Store management access");
        var cashier = new Role("Cashier", "Point-of-sale access");

        foreach (var permission in permissions)
        {
            administrator.AddPermission(permission);
        }

        foreach (var permission in permissions.Where(permission => permission.Name is not PermissionConstants.UsersManage and not PermissionConstants.RolesManage and not PermissionConstants.RestoreDatabase))
        {
            manager.AddPermission(permission);
        }

        cashier.AddPermission(permissions.Single(permission => permission.Name == PermissionConstants.SalesCreate));
        cashier.AddPermission(permissions.Single(permission => permission.Name == PermissionConstants.ProductView));
        cashier.AddPermission(permissions.Single(permission => permission.Name == PermissionConstants.CustomerView));

        await _dbContext.Roles.AddRangeAsync([administrator, manager, cashier], cancellationToken);
    }

    private async Task SeedCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = new[]
        {
            new Category("Programming"),
            new Category("Novels"),
            new Category("Education"),
            new Category("Children"),
            new Category("Religion"),
            new Category("Science")
        };

        await _dbContext.Categories.AddRangeAsync(categories, cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var administratorRole = await _dbContext.Roles.FirstAsync(role => role.Name == "Administrator", cancellationToken);
        var adminUser = new User("admin", AdminPasswordHash, "System Administrator", administratorRole.Id);
        adminUser.UpdateContact(new Email("admin@bookstore.local"));
        await _dbContext.Users.AddAsync(adminUser, cancellationToken);
    }
}
