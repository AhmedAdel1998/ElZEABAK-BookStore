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
            new Permission(PermissionConstants.SupplierCreate, "Create suppliers"),
            new Permission(PermissionConstants.SupplierEdit, "Edit suppliers"),
            new Permission(PermissionConstants.SupplierDelete, "Delete suppliers"),
            new Permission(PermissionConstants.SupplierViewProducts, "View supplier products"),
            new Permission(PermissionConstants.ReportsView, "View reports"),
            new Permission(PermissionConstants.ReportView, "Open reports dashboard"),
            new Permission(PermissionConstants.ReportSales, "View sales reports"),
            new Permission(PermissionConstants.ReportProfit, "View profit reports"),
            new Permission(PermissionConstants.ReportInventory, "View inventory reports"),
            new Permission(PermissionConstants.ReportCustomers, "View customer reports"),
            new Permission(PermissionConstants.ReportCashiers, "View cashier performance reports"),
            new Permission(PermissionConstants.ReportExport, "Export reports"),
            new Permission(PermissionConstants.ReceiptPrint, "Print receipts"),
            new Permission(PermissionConstants.ReceiptReprint, "Reprint receipts"),
            new Permission(PermissionConstants.ReceiptTestPrint, "Run printer test print"),
            new Permission(PermissionConstants.ReceiptSettings, "Manage receipt printer settings"),
            new Permission(PermissionConstants.SettingsView, "View settings"),
            new Permission(PermissionConstants.UsersManage, "Manage users"),
            new Permission(PermissionConstants.RolesManage, "Manage roles"),
            new Permission(PermissionConstants.BackupView, "View database backups"),
            new Permission(PermissionConstants.BackupCreate, "Create database backups"),
            new Permission(PermissionConstants.BackupRestore, "Restore database backups"),
            new Permission(PermissionConstants.BackupDelete, "Delete database backups"),
            new Permission(PermissionConstants.BackupValidate, "Validate database backups"),
            new Permission(PermissionConstants.BackupSettings, "Manage backup settings")
        };

        var existingPermissionNames = await _dbContext.Permissions.Select(permission => permission.Name).ToListAsync(cancellationToken);
        var missingPermissions = permissions
            .Where(permission => !existingPermissionNames.Contains(permission.Name, StringComparer.OrdinalIgnoreCase))
            .ToArray();
        if (missingPermissions.Length > 0)
        {
            await _dbContext.Permissions.AddRangeAsync(missingPermissions, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var storedPermissions = await _dbContext.Permissions.ToListAsync(cancellationToken);

        if (await _dbContext.Roles.AnyAsync(cancellationToken))
        {
            await EnsureRolePermissionsAsync(storedPermissions, cancellationToken);
            return;
        }

        var administrator = new Role("Administrator", "Full system access");
        var manager = new Role("Manager", "Store management access");
        var cashier = new Role("Cashier", "Point-of-sale access");

        foreach (var permission in storedPermissions)
        {
            administrator.AddPermission(permission);
        }

        foreach (var permission in storedPermissions.Where(permission => permission.Name is not PermissionConstants.UsersManage and not PermissionConstants.RolesManage and not PermissionConstants.BackupRestore))
        {
            manager.AddPermission(permission);
        }

        cashier.AddPermission(storedPermissions.Single(permission => permission.Name == PermissionConstants.SalesCreate));
        cashier.AddPermission(storedPermissions.Single(permission => permission.Name == PermissionConstants.ProductView));
        cashier.AddPermission(storedPermissions.Single(permission => permission.Name == PermissionConstants.CustomerView));
        cashier.AddPermission(storedPermissions.Single(permission => permission.Name == PermissionConstants.ReceiptPrint));

        await _dbContext.Roles.AddRangeAsync([administrator, manager, cashier], cancellationToken);
    }

    private async Task EnsureRolePermissionsAsync(IReadOnlyCollection<Permission> permissions, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles.Include(role => role.Permissions).ToListAsync(cancellationToken);
        var administrator = roles.FirstOrDefault(role => role.Name == "Administrator");
        if (administrator is not null)
        {
            foreach (var permission in permissions.Where(permission => administrator.Permissions.All(existing => existing.Name != permission.Name)))
            {
                administrator.AddPermission(permission);
            }
        }

        var manager = roles.FirstOrDefault(role => role.Name == "Manager");
        if (manager is not null)
        {
            foreach (var permission in permissions.Where(permission => permission.Name is not PermissionConstants.UsersManage and not PermissionConstants.RolesManage and not PermissionConstants.BackupRestore && manager.Permissions.All(existing => existing.Name != permission.Name)))
            {
                manager.AddPermission(permission);
            }
        }
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
