using BookStore.Domain.Entities;
using BookStore.Domain.Enums;
using BookStore.Domain.ValueObjects;
using BookStore.Persistence.Context;
using BookStore.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Infrastructure.Tests;

public class CustomerPersistenceTests
{
    [Fact]
    public async Task CustomerCreationAndSearch_WorkAgainstSqlite()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new CustomerRepository(fixture.Context);
        var customer = new Customer("Ahmed Adel");
        customer.UpdateContact(new PhoneNumber("+201001001000"), new Email("ahmed@example.com"), null);

        await repository.AddAsync(customer);
        await fixture.Context.SaveChangesAsync();

        var results = await repository.SearchAsync("1001001000", true, 1, 10);

        Assert.Single(results);
        Assert.Equal("Ahmed Adel", results.Single().FullName);
    }

    [Fact]
    public async Task CustomerUpdate_PersistsProfileChanges()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new CustomerRepository(fixture.Context);
        var customer = new Customer("Ahmed Adel");
        customer.UpdateContact(new PhoneNumber("+201001001000"), null, null);
        await repository.AddAsync(customer);
        await fixture.Context.SaveChangesAsync();

        customer.UpdateProfile("Updated Customer", new PhoneNumber("+201001001001"), null, null, true);
        await fixture.Context.SaveChangesAsync();

        var updated = await repository.GetSummaryByIdAsync(customer.Id);
        Assert.Equal("Updated Customer", updated!.FullName);
        Assert.Equal("+201001001001", updated.Phone);
    }

    [Fact]
    public async Task CustomerSoftDeletion_RemovesFromDefaultSearch()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new CustomerRepository(fixture.Context);
        var customer = new Customer("Deleted Customer");
        customer.UpdateContact(new PhoneNumber("+201001001000"), null, null);
        await repository.AddAsync(customer);
        await fixture.Context.SaveChangesAsync();

        customer.MarkDeleted();
        await fixture.Context.SaveChangesAsync();

        Assert.Equal(0, await repository.CountAsync());
    }

    [Fact]
    public async Task CustomerSaleRelationship_ReturnsSalesHistory()
    {
        await using var fixture = await PersistenceFixture.CreateAsync();
        var repository = new CustomerRepository(fixture.Context);
        var role = new Role("Cashier");
        var user = new User("cashier", "$2a$11$abcdefghijklmnopqrstuuF8dO6k6iIq3ONC8CS5f8FrcHcJfW8pG", "Cashier", role.Id);
        var customer = new Customer("History Customer");
        customer.UpdateContact(new PhoneNumber("+201001001000"), null, null);
        var category = new Category("Books");
        var product = new Product(new BookStore.Domain.ValueObjects.Barcode("BK100"), "Book", 50, 100, category.Id);
        fixture.Context.Roles.Add(role);
        fixture.Context.Users.Add(user);
        fixture.Context.Customers.Add(customer);
        fixture.Context.Categories.Add(category);
        fixture.Context.Products.Add(product);
        await fixture.Context.SaveChangesAsync();

        var sale = new Sale("INV-100", user.Id, PaymentMethod.Cash);
        sale.AssignCustomer(customer.Id);
        sale.AddItem(new SaleItem(product.Id, 1, 100));
        sale.Complete(100);
        fixture.Context.Sales.Add(sale);
        await fixture.Context.SaveChangesAsync();

        var history = await repository.GetSalesHistoryAsync(customer.Id, null, null, 1, 10);

        Assert.Single(history);
        Assert.Equal("INV-100", history.Single().InvoiceNumber);
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
