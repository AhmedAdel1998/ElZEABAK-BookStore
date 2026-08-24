using BookStore.Application.Features.Categories.Handlers;
using BookStore.Application.Features.Products.Handlers;
using BookStore.Application.Interfaces;
using BookStore.Shared.Constants;
using BookStore.UI.Services;
using BookStore.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.UI.Tests;

/// <summary>
/// Covers the handover from the global search box to the product list: the term the search could
/// not resolve to a single record has to arrive as the list page filter.
/// </summary>
public class ProductListHandoverTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ProductList_OpensFilteredByTheTermTheSearchHandedOver()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        await fixture.AddProductAsync("BK-002", "Refactoring");
        fixture.GlobalSearch.SetPendingFilter("clean");

        var list = CreateProductList(fixture);
        await WaitUntilAsync(() => list.Products.Count > 0, "the product list to load");

        Assert.Equal("clean", list.SearchTerm);
        Assert.Single(list.Products);
        Assert.Equal("Clean Code", list.Products[0].Title);
    }

    [Fact]
    public async Task ProductList_OpensUnfiltered_WhenNoTermWasHandedOver()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        await fixture.AddProductAsync("BK-002", "Refactoring");

        var list = CreateProductList(fixture);
        await WaitUntilAsync(() => list.Products.Count > 0, "the product list to load");

        Assert.Equal(string.Empty, list.SearchTerm);
        Assert.Equal(2, list.Products.Count);
    }

    [Fact]
    public async Task ProductList_OpensUnfilteredTheSecondTime_BecauseTheTermIsConsumedOnce()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        await fixture.AddProductAsync("BK-002", "Refactoring");
        fixture.GlobalSearch.SetPendingFilter("clean");

        var first = CreateProductList(fixture);
        await WaitUntilAsync(() => first.Products.Count > 0, "the first product list to load");

        var second = CreateProductList(fixture);
        await WaitUntilAsync(() => second.Products.Count > 0, "the second product list to load");

        Assert.Equal("clean", first.SearchTerm);
        Assert.Equal(string.Empty, second.SearchTerm);
        Assert.Equal(2, second.Products.Count);
    }

    /// <summary>
    /// Builds the product list page the way navigation does: everything from the container, one
    /// scope per page.
    /// </summary>
    private static ProductListViewModel CreateProductList(ShellSearchFixture fixture)
    {
        var scope = fixture.Services.CreateScope();
        var services = scope.ServiceProvider;
        return new ProductListViewModel(
            services.GetRequiredService<SearchProductsHandler>(),
            services.GetRequiredService<DeleteProductHandler>(),
            services.GetRequiredService<ActivateProductHandler>(),
            services.GetRequiredService<DeactivateProductHandler>(),
            services.GetRequiredService<DuplicateProductHandler>(),
            services.GetRequiredService<SearchCategoriesHandler>(),
            new FakeAuthorizationService(PermissionConstants.ProductView),
            fixture.ShellNavigation,
            fixture.Notifications,
            new FakeConfirmationDialogService(),
            new ProductNavigationState(),
            fixture.GlobalSearch);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string because)
    {
        var deadline = DateTime.UtcNow + Timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Timed out after {Timeout.TotalSeconds:N0}s waiting for {because}.");
    }
}

internal sealed class FakeConfirmationDialogService : IConfirmationDialogService
{
    public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(true);
}
