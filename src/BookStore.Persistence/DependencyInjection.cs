using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using BookStore.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Persistence;

/// <summary>
/// Registers persistence-layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds Entity Framework, repositories, unit of work, and database initialization services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BookStoreDb")
            ?? "Data Source=bookstore.db";

        services.AddDbContext<BookStoreDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
                sqliteOptions.MigrationsAssembly(typeof(BookStoreDbContext).Assembly.FullName));
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }
}
