using BookStore.Domain.Entities;
using BookStore.Infrastructure.Authentication;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using BookStore.Persistence.UnitOfWork;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookStore.Infrastructure.Tests;

public class FirstRunSetupServiceTests
{
    [Fact]
    public async Task CreateAdministratorAsync_WithValidPassword_CreatesAdministratorUser()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedAdministratorRoleAsync();
        var service = fixture.CreateService();

        var result = await service.CreateAdministratorAsync("admin", "Mansour Maged", "admin@bookstore.local", "Admin123!", "Admin123!");

        Assert.True(result.Succeeded);
        var user = await fixture.Context.Users.Include(user => user.Role).SingleAsync();
        Assert.Equal("admin", user.Username);
        Assert.Equal("Mansour Maged", user.FullName);
        Assert.Equal("admin@bookstore.local", user.Email?.Value);
        Assert.NotNull(user.Role);
        Assert.Equal("Administrator", user.Role.Name);
        Assert.True(new PasswordHasher().VerifyPassword("Admin123!", user.PasswordHash));
    }

    [Fact]
    public async Task CreateAdministratorAsync_WithWeakPassword_ReturnsValidationErrorAndDoesNotCreateUser()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        await fixture.SeedAdministratorRoleAsync();
        var service = fixture.CreateService();

        var result = await service.CreateAdministratorAsync("admin", "Mansour Maged", "admin@bookstore.local", "admin123", "admin123");

        Assert.False(result.Succeeded);
        Assert.Contains(result.ValidationErrors, error => error.PropertyName == "password");
        Assert.Empty(await fixture.Context.Users.ToListAsync());
    }

    private sealed class PersistenceFixture : IAsyncDisposable
    {
        private PersistenceFixture(SqliteConnection connection, BookStoreDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        public SqliteConnection Connection { get; }
        public BookStoreDbContext Context { get; }

        public static async Task<PersistenceFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BookStoreDbContext>().UseSqlite(connection).Options;
            var context = new BookStoreDbContext(options);
            await context.Database.EnsureCreatedAsync();
            return new PersistenceFixture(connection, context);
        }

        public async Task SeedAdministratorRoleAsync()
        {
            Context.Roles.Add(new Role("Administrator", "Full system access"));
            await Context.SaveChangesAsync();
        }

        public FirstRunSetupService CreateService()
        {
            var unitOfWork = new UnitOfWork(
                Context,
                new ProductRepository(Context),
                new CategoryRepository(Context),
                new CustomerRepository(Context),
                new SupplierRepository(Context),
                new UserRepository(Context),
                new RoleRepository(Context),
                new SaleRepository(Context),
                new InventoryRepository(Context),
                NullLogger<UnitOfWork>.Instance);

            return new FirstRunSetupService(unitOfWork, new PasswordHasher(), NullLogger<FirstRunSetupService>.Instance);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
