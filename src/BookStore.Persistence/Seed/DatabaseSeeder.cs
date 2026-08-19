using BookStore.Domain.Entities;
using BookStore.Domain.ValueObjects;
using BookStore.Shared.Constants;
using BookStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BookStore.Persistence.Seed;

/// <summary>
/// Seeds required default data.
/// </summary>
public class DatabaseSeeder
{
    private readonly BookStoreDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSeeder"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseSeeder(BookStoreDbContext dbContext, IConfiguration configuration, ILogger<DatabaseSeeder> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
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
        var permissions = PermissionCatalog.All
            .Select(definition => new Permission(definition.Name, definition.Description))
            .ToArray();

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

        foreach (var permission in storedPermissions.Where(permission => PermissionCatalog.ManagerPermissions.Contains(permission.Name, StringComparer.OrdinalIgnoreCase)))
        {
            manager.AddPermission(permission);
        }

        foreach (var permission in storedPermissions.Where(permission => PermissionCatalog.CashierPermissions.Contains(permission.Name, StringComparer.OrdinalIgnoreCase)))
        {
            cashier.AddPermission(permission);
        }

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
            foreach (var permission in permissions.Where(permission => PermissionCatalog.ManagerPermissions.Contains(permission.Name, StringComparer.OrdinalIgnoreCase) && manager.Permissions.All(existing => existing.Name != permission.Name)))
            {
                manager.AddPermission(permission);
            }
        }

        EnsureCashierPermissions(roles, permissions);
    }

    private static void EnsureCashierPermissions(IReadOnlyCollection<Role> roles, IReadOnlyCollection<Permission> permissions)
    {
        var cashier = roles.FirstOrDefault(role => role.Name == "Cashier");
        if (cashier is null)
        {
            return;
        }

        foreach (var permission in permissions.Where(permission =>
            PermissionCatalog.CashierPermissions.Contains(permission.Name, StringComparer.OrdinalIgnoreCase)
            && cashier.Permissions.All(existing => existing.Name != permission.Name)))
        {
            cashier.AddPermission(permission);
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

        var initialAdmin = new InitialAdminOptions
        {
            Username = _configuration["Application:InitialAdmin:Username"] ?? string.Empty,
            PasswordHash = _configuration["Application:InitialAdmin:PasswordHash"] ?? string.Empty,
            FullName = _configuration["Application:InitialAdmin:FullName"] ?? string.Empty,
            Email = _configuration["Application:InitialAdmin:Email"] ?? string.Empty
        };
        if (string.IsNullOrWhiteSpace(initialAdmin.Username) || string.IsNullOrWhiteSpace(initialAdmin.PasswordHash))
        {
            _logger.LogInformation("No initial administrator was seeded from configuration. First-run setup will create the administrator account.");
            return;
        }

        var administratorRole = await _dbContext.Roles.FirstAsync(role => role.Name == "Administrator", cancellationToken);
        var adminUser = new User(initialAdmin.Username.Trim(), initialAdmin.PasswordHash.Trim(), string.IsNullOrWhiteSpace(initialAdmin.FullName) ? "System Administrator" : initialAdmin.FullName.Trim(), administratorRole.Id);
        if (!string.IsNullOrWhiteSpace(initialAdmin.Email))
        {
            adminUser.UpdateContact(new Email(initialAdmin.Email.Trim()));
        }

        await _dbContext.Users.AddAsync(adminUser, cancellationToken);
    }

    private sealed class InitialAdminOptions
    {
        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
