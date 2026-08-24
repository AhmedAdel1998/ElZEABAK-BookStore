using BookStore.Shared.Constants;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Tests;

/// <summary>
/// Covers the shell global search box end to end: typing a term, what the dropdown offers, and
/// where each row lands. These run against a real database through the real query handlers.
/// </summary>
public class GlobalSearchTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Typing_FindsAProductByTitle()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);

        var row = Assert.Single(shell.SearchResults, result => result.PrimaryText == "Clean Code");
        Assert.Equal("Search.Products", row.Group);
        Assert.Equal("BK-001", row.SecondaryText);
    }

    [Fact]
    public async Task Typing_FindsAProductByBarcode()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-4711", "Domain Driven Design");
        var shell = fixture.CreateShell();

        shell.SearchText = "4711";
        await WaitForSearchAsync(shell);

        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Domain Driven Design");
    }

    [Fact]
    public async Task Typing_FindsAProductByAuthor()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-002", "Refactoring", author: "Martin Fowler");
        var shell = fixture.CreateShell();

        shell.SearchText = "fowler";
        await WaitForSearchAsync(shell);

        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Refactoring");
    }

    [Fact]
    public async Task Typing_MatchesRegardlessOfCase()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-003", "Clean Architecture");
        var shell = fixture.CreateShell();

        shell.SearchText = "CLEAN ARCH";
        await WaitForSearchAsync(shell);

        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Clean Architecture");
    }

    [Fact]
    public async Task Typing_FindsAnArabicTitle()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-004", "الأسود يليق بك");
        var shell = fixture.CreateShell();

        shell.SearchText = "يليق";
        await WaitForSearchAsync(shell);

        Assert.Single(shell.SearchResults, result => result.PrimaryText == "الأسود يليق بك");
    }

    [Fact]
    public async Task Typing_FindsACustomerByNameAndShowsThePhone()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddCustomerAsync("Ahmed Helmy", "+201001001000");
        var shell = fixture.CreateShell();

        shell.SearchText = "helmy";
        await WaitForSearchAsync(shell);

        var row = Assert.Single(shell.SearchResults, result => result.PrimaryText == "Ahmed Helmy");
        Assert.Equal("Search.Customers", row.Group);
        Assert.Equal("+201001001000", row.SecondaryText);
    }

    [Fact]
    public async Task Typing_FindsACustomerByPhone()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddCustomerAsync("Ahmed Helmy", "+201001001000");
        var shell = fixture.CreateShell();

        shell.SearchText = "1001001000";
        await WaitForSearchAsync(shell);

        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Ahmed Helmy");
    }

    [Fact]
    public async Task Typing_OffersAPageThatMatchesTheTerm()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var shell = fixture.CreateShell();

        shell.SearchText = "produc";
        await WaitForSearchAsync(shell);

        var row = Assert.Single(shell.SearchResults, result => result.Group == "Search.Pages");
        Assert.Equal("Products", row.PrimaryText);
    }

    [Fact]
    public async Task Typing_NeverOffersLogoutAsAPage()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var shell = fixture.CreateShell();

        shell.SearchText = "logout";
        await WaitForSearchAsync(shell);

        Assert.DoesNotContain(shell.SearchResults, result => result.PrimaryText == "Logout");
    }

    [Fact]
    public async Task Typing_SkipsProducts_WhenTheUserMayNotViewThem()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(PermissionConstants.CustomerView);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        await fixture.AddCustomerAsync("Clean Customer", "+201001001000");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);

        Assert.DoesNotContain(shell.SearchResults, result => result.Group == "Search.Products");
        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Clean Customer");
    }

    [Fact]
    public async Task Typing_SkipsCustomers_WhenTheUserMayNotViewThem()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(PermissionConstants.ProductView);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        await fixture.AddCustomerAsync("Clean Customer", "+201001001000");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);

        Assert.DoesNotContain(shell.SearchResults, result => result.Group == "Search.Customers");
        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Clean Code");
    }

    [Fact]
    public async Task Typing_CapsTheProductRowsItShows()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        for (var index = 0; index < 9; index++)
        {
            await fixture.AddProductAsync($"BK-1{index}", $"Clean Volume {index}");
        }

        var shell = fixture.CreateShell();

        shell.SearchText = "clean volume";
        await WaitForSearchAsync(shell);

        Assert.Equal(5, shell.SearchResults.Count(result => result.Group == "Search.Products"));
    }

    [Fact]
    public async Task Typing_FindsADeactivatedProduct_BecauseItsDetailsPageIsStillWorthReaching()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-005", "Retired Title", isActive: false);
        var shell = fixture.CreateShell();

        shell.SearchText = "retired";
        await WaitForSearchAsync(shell);

        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Retired Title");
    }

    [Fact]
    public async Task Typing_NeverFindsADeletedProduct()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-006", "Deleted Title", isDeleted: true);
        var shell = fixture.CreateShell();

        shell.SearchText = "deleted";
        await WaitForSearchAsync(shell);

        Assert.DoesNotContain(shell.SearchResults, result => result.PrimaryText == "Deleted Title");
        Assert.Equal("Search.NoMatches", shell.SearchStatus);
    }

    [Fact]
    public async Task Typing_SaysSoWhenNothingMatches()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = "nothing here";
        await WaitForSearchAsync(shell);

        Assert.Empty(shell.SearchResults);
        Assert.Equal("Search.NoMatches", shell.SearchStatus);
        Assert.True(shell.IsSearchResultsOpen);
    }

    [Theory]
    [InlineData("")]
    [InlineData("c")]
    [InlineData(" ")]
    public async Task Typing_StaysClosed_UntilTheTermIsLongEnoughToBeUseful(string term)
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = term;
        await Task.Delay(600);

        Assert.Empty(shell.SearchResults);
        Assert.False(shell.IsSearchResultsOpen);
        Assert.Equal(string.Empty, shell.SearchStatus);
    }

    [Fact]
    public async Task Retyping_ShowsOnlyTheLastTermsResults()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        await fixture.AddProductAsync("BK-002", "Refactoring");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        shell.SearchText = "refactor";
        await WaitForSearchAsync(shell);

        // Long enough that a surviving first search would have landed by now.
        await Task.Delay(400);

        Assert.Single(shell.SearchResults, result => result.PrimaryText == "Refactoring");
        Assert.DoesNotContain(shell.SearchResults, result => result.PrimaryText == "Clean Code");
    }

    [Fact]
    public async Task ClearingTheBox_ClosesTheDropdown()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);
        shell.SearchText = string.Empty;

        Assert.Empty(shell.SearchResults);
        Assert.False(shell.IsSearchResultsOpen);
    }

    [Fact]
    public async Task Escape_ClosesTheDropdownButKeepsTheTermForEditing()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);
        shell.CloseSearchResultsCommand.Execute(null);

        Assert.Empty(shell.SearchResults);
        Assert.False(shell.IsSearchResultsOpen);
        Assert.Equal("clean", shell.SearchText);
    }

    [Fact]
    public async Task OpeningAProduct_LandsOnThatProductsDetailsPage()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var productId = await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);
        var row = shell.SearchResults.Single(result => result.PrimaryText == "Clean Code");
        await shell.OpenSearchResultCommand.ExecuteAsync(row);

        Assert.Equal(productId, fixture.ProductNavigation.SelectedProductId);
        Assert.Contains((typeof(ProductDetailsViewModel), "Products > Details"), fixture.ShellNavigation.Navigations);
    }

    [Fact]
    public async Task OpeningACustomer_LandsOnThatCustomersDetailsPage()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var customerId = await fixture.AddCustomerAsync("Ahmed Helmy", "+201001001000");
        var shell = fixture.CreateShell();

        shell.SearchText = "helmy";
        await WaitForSearchAsync(shell);
        var row = shell.SearchResults.Single(result => result.PrimaryText == "Ahmed Helmy");
        await shell.OpenSearchResultCommand.ExecuteAsync(row);

        Assert.Equal(customerId, fixture.CustomerNavigation.SelectedCustomerId);
        Assert.Contains((typeof(CustomerDetailsViewModel), "Customers > Details"), fixture.ShellNavigation.Navigations);
    }

    [Fact]
    public async Task OpeningAPage_NavigatesToIt()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var shell = fixture.CreateShell();

        shell.SearchText = "produc";
        await WaitForSearchAsync(shell);
        var row = shell.SearchResults.Single(result => result.Group == "Search.Pages");
        await shell.OpenSearchResultCommand.ExecuteAsync(row);

        Assert.Contains(fixture.ShellNavigation.Navigations, entry => entry.ViewModel == typeof(ProductListViewModel));
    }

    [Fact]
    public async Task OpeningAResult_EmptiesTheSearchBox()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);
        await shell.OpenSearchResultCommand.ExecuteAsync(shell.SearchResults.Single(result => result.Group == "Search.Products"));

        Assert.Equal(string.Empty, shell.SearchText);
        Assert.Empty(shell.SearchResults);
        Assert.False(shell.IsSearchResultsOpen);
    }

    [Fact]
    public async Task Enter_OpensTheFirstResult()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var productId = await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        // "clean" matches no page name, so the product is the first row.
        shell.SearchText = "clean";
        await WaitForSearchAsync(shell);
        await shell.OpenTopSearchResultCommand.ExecuteAsync(null);

        Assert.Equal(productId, fixture.ProductNavigation.SelectedProductId);
    }

    [Fact]
    public async Task Enter_FallsBackToTheFilteredProductList_WhenNothingMatched()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var shell = fixture.CreateShell();

        shell.SearchText = "no such book";
        await WaitForSearchAsync(shell);
        await shell.OpenTopSearchResultCommand.ExecuteAsync(null);

        Assert.Contains(fixture.ShellNavigation.Navigations, entry => entry.ViewModel == typeof(ProductListViewModel));
        Assert.Equal("no such book", fixture.GlobalSearch.ConsumePendingFilter());
        Assert.Equal(string.Empty, shell.SearchText);
    }

    [Fact]
    public async Task Enter_DoesNothing_WhenTheUserMayNotViewProducts()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync();
        var shell = fixture.CreateShell();
        fixture.ShellNavigation.Navigations.Clear();

        shell.SearchText = "no such book";
        await WaitForSearchAsync(shell);
        await shell.OpenTopSearchResultCommand.ExecuteAsync(null);

        Assert.Empty(fixture.ShellNavigation.Navigations);
        Assert.Null(fixture.GlobalSearch.ConsumePendingFilter());
    }

    [Fact]
    public async Task Enter_DoesNothing_WhileTheTermIsTooShort()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var shell = fixture.CreateShell();
        fixture.ShellNavigation.Navigations.Clear();

        shell.SearchText = "c";
        await shell.OpenTopSearchResultCommand.ExecuteAsync(null);

        Assert.Empty(fixture.ShellNavigation.Navigations);
        Assert.Null(fixture.GlobalSearch.ConsumePendingFilter());
    }

    [Fact]
    public async Task Searching_ReportsFailureInsteadOfThrowing_WhenTheQueryStackIsUnavailable()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        var shell = fixture.CreateShell();

        // Disposing the provider makes every later scope request fail, standing in for a database
        // that has gone away mid-session.
        await fixture.DisposeProviderAsync();

        shell.SearchText = "clean";
        await WaitUntilAsync(() => shell.SearchStatus.Length > 0, "the failure message to appear");

        Assert.Equal("Search.Failed", shell.SearchStatus);
        Assert.Empty(shell.SearchResults);
    }

    [Fact]
    public async Task Disposing_CancelsAnInFlightSearch()
    {
        await using var fixture = await ShellSearchFixture.CreateAsync(ShellSearchFixture.FullAccess);
        await fixture.AddProductAsync("BK-001", "Clean Code");
        var shell = fixture.CreateShell();

        shell.SearchText = "clean";
        shell.Dispose();
        await Task.Delay(600);

        Assert.Empty(shell.SearchResults);
        Assert.False(shell.IsSearchResultsOpen);
    }

    /// <summary>
    /// Waits for the debounce and the query behind it. The view model opens the dropdown once a
    /// search finishes, including when it found nothing, so that is the completion signal.
    /// </summary>
    private static Task WaitForSearchAsync(AuthenticatedHomeViewModel shell) =>
        WaitUntilAsync(() => shell.IsSearchResultsOpen, "the search to finish");

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
