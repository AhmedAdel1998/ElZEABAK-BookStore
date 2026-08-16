using BookStore.Domain.Interfaces;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using BookStore.Persistence.Seed;
using BookStore.Persistence.Settings;
using BookStore.Shared.Constants;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
        var connectionString = BuildSqliteConnectionString(configuration.GetConnectionString("BookStoreDb")
            ?? "Data Source=bookstore.db");

        services.AddDbContext<BookStoreDbContext>(options =>
        {
            options
                .UseSqlite(connectionString, sqliteOptions =>
                    sqliteOptions.MigrationsAssembly(typeof(BookStoreDbContext).Assembly.FullName))
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.CommandExecuted));
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<ISettingsStore, EfSettingsStore>();
        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<DatabaseSeeder>();
        services.AddHostedService<DatabaseInitializer>();

        return services;
    }

    private static string BuildSqliteConnectionString(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString)
        {
            ForeignKeys = true,
            DefaultTimeout = 30
        };
        if (!string.IsNullOrWhiteSpace(builder.DataSource) && builder.DataSource != ":memory:" && !Path.IsPathRooted(Environment.ExpandEnvironmentVariables(builder.DataSource)))
        {
            builder.DataSource = ApplicationPaths.ResolveDataPath(builder.DataSource);
        }

        return builder.ToString();
    }
}
