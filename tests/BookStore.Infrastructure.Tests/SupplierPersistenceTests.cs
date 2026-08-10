using BookStore.Domain.Entities;
using BookStore.Domain.ValueObjects;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Tests;

public class SupplierPersistenceTests
{
    [Fact]
    public async Task SupplierCreationAndSearch_WorkAgainstSqlite()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new SupplierRepository(fixture.Context);
        var supplier = new Supplier("ABC Distribution");
        supplier.UpdateDetails("Ahmed", new PhoneNumber("+201001001000"), new Email("supplier@example.com"), null, null);

        await repository.AddAsync(supplier);
        await fixture.Context.SaveChangesAsync();

        var results = await repository.SearchAsync("1001001000", true, 1, 10);

        Assert.Single(results);
        Assert.Equal("ABC Distribution", results.Single().CompanyName);
    }

    [Fact]
    public async Task SupplierUpdate_PersistsProfileChanges()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new SupplierRepository(fixture.Context);
        var supplier = new Supplier("ABC Distribution");
        supplier.UpdateDetails(null, new PhoneNumber("+201001001000"), null, null, null);
        await repository.AddAsync(supplier);
        await fixture.Context.SaveChangesAsync();

        supplier.UpdateProfile("Updated Supplier", "Ahmed", new PhoneNumber("+201001001001"), null, null, "Notes", false);
        await fixture.Context.SaveChangesAsync();

        var updated = await repository.GetSummaryByIdAsync(supplier.Id);
        Assert.Equal("Updated Supplier", updated!.CompanyName);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task SupplierSoftDeletion_RemovesFromDefaultSearch()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new SupplierRepository(fixture.Context);
        var supplier = new Supplier("Deleted Supplier");
        supplier.UpdateDetails(null, new PhoneNumber("+201001001000"), null, null, null);
        await repository.AddAsync(supplier);
        await fixture.Context.SaveChangesAsync();

        supplier.MarkDeleted();
        await fixture.Context.SaveChangesAsync();

        Assert.Equal(0, await repository.CountAsync());
    }

    [Fact]
    public async Task SupplierProductRelationship_ReturnsProducts()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new SupplierRepository(fixture.Context);
        var category = new Category("Books");
        var product = new Product(new BookStore.Domain.ValueObjects.Barcode("BK100"), "Supplier Book", 50, 100, category.Id);
        var supplier = new Supplier("ABC Distribution");
        supplier.UpdateDetails(null, new PhoneNumber("+201001001000"), null, null, null);
        fixture.Context.Categories.Add(category);
        fixture.Context.Products.Add(product);
        fixture.Context.Suppliers.Add(supplier);
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ProductSuppliers.Add(new ProductSupplier(product.Id, supplier.Id));
        await fixture.Context.SaveChangesAsync();

        var products = await repository.GetProductsAsync(supplier.Id, 1, 10);

        Assert.Single(products);
        Assert.Equal("Supplier Book", products.Single().Title);
        Assert.Equal(1, await repository.CountProductsAsync(supplier.Id));
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

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
