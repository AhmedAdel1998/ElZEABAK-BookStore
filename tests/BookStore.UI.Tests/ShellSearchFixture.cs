using BookStore.Application;
using BookStore.Domain.Entities;
using BookStore.Domain.Interfaces;
using BookStore.Domain.ValueObjects;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using BookStore.UI.Navigation;
using BookStore.Shared.Constants;
using BookStore.UI.Services;
using BookStore.UI.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BookStore.UI.Tests;

/// <summary>
/// Builds the shell view model over the real query stack: real handlers, real validators, real
/// repositories and a real SQLite database. Only the presentation services the shell talks to are
/// faked, so a test failure means the search itself is broken rather than a stub being wrong.
/// </summary>
internal sealed class ShellSearchFixture : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private ShellSearchFixture(SqliteConnection connection, ServiceProvider provider)
    {
        _connection = connection;
        _provider = provider;
    }

    public FakeShellNavigationService ShellNavigation { get; } = new();

    public FakeApplicationNavigationService ApplicationNavigation { get; } = new();

    public FakeLoadingService Loading { get; } = new();

    public FakeNotificationService Notifications { get; } = new();

    public IProductNavigationState ProductNavigation { get; } = new ProductNavigationState();

    public ICustomerNavigationState CustomerNavigation { get; } = new CustomerNavigationState();

    public IGlobalSearchState GlobalSearch { get; } = new GlobalSearchState();

    public IServiceProvider Services => _provider;

    /// <summary>
    /// Creates a fixture whose signed-in user holds the supplied permissions. Passing none models
    /// a cashier who may not browse the catalogue.
    /// </summary>
    public static async Task<ShellSearchFixture> CreateAsync(params string[] permissions)
    {
        // A named shared-cache in-memory database, with one connection held open for the fixture's
        // lifetime so the schema survives: every DbContext then opens its own connection to it,
        // which is what lets two overlapping searches run without sharing one SQLite connection.
        var databaseName = $"shell-search-{Guid.NewGuid():N}";
        var connectionString = $"Data Source=file:{databaseName}?mode=memory&cache=shared";
        var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        services.AddDbContext<BookStoreDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUnitOfWork, BookStore.Persistence.UnitOfWork.UnitOfWork>();
        services.AddApplication();

        var provider = services.BuildServiceProvider();
        using (var scope = provider.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<BookStoreDbContext>().Database.EnsureCreatedAsync();
        }

        var fixture = new ShellSearchFixture(connection, provider);
        fixture.Permissions = permissions;
        return fixture;
    }

    public string[] Permissions { get; private set; } = [];

    /// <summary>Builds the shell view model under test.</summary>
    public AuthenticatedHomeViewModel CreateShell() => new(
        new FakeAuthenticationService(),
        ApplicationNavigation,
        ShellNavigation,
        new FakeAuthorizationService(Permissions),
        new FakeCurrentUserService(),
        new FakeSessionTimeoutService(),
        Notifications,
        Loading,
        new FakeThemeService(),
        new FakeLocalizationService(),
        _provider.GetRequiredService<IServiceScopeFactory>(),
        ProductNavigation,
        CustomerNavigation,
        GlobalSearch);

    /// <summary>Adds a product, returning its identifier.</summary>
    public async Task<Guid> AddProductAsync(string barcode, string title, string? author = null, bool isActive = true, bool isDeleted = false)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var category = await context.Categories.FirstOrDefaultAsync();
        if (category is null)
        {
            category = new Category("Books");
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }

        var product = new Product(new Barcode(barcode), title, 50m, 100m, category.Id);
        if (author is not null)
        {
            product.UpdateDetails(null, null, author, null, null, null);
        }

        if (!isActive)
        {
            product.Deactivate();
        }

        if (isDeleted)
        {
            product.MarkDeleted();
        }

        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product.Id;
    }

    /// <summary>Adds a customer, returning its identifier.</summary>
    public async Task<Guid> AddCustomerAsync(string fullName, string phone)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BookStoreDbContext>();
        var customer = new Customer(fullName);
        customer.UpdateContact(new PhoneNumber(phone), null, null);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer.Id;
    }

    /// <summary>
    /// Tears down the query stack while the shell is still alive, so a test can see what the
    /// search does when the database becomes unreachable.
    /// </summary>
    public async Task DisposeProviderAsync()
    {
        if (_providerDisposed)
        {
            return;
        }

        _providerDisposed = true;
        await _provider.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeProviderAsync();
        await _connection.DisposeAsync();
    }

    private bool _providerDisposed;

    /// <summary>Permission bundle for a user who may browse both catalogue and customers.</summary>
    public static string[] FullAccess => [PermissionConstants.ProductView, PermissionConstants.CustomerView];
}
