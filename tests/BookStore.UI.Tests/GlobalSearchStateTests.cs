using BookStore.UI.Navigation;
using BookStore.UI.Services;

namespace BookStore.UI.Tests;

public class GlobalSearchStateTests
{
    [Fact]
    public void ConsumePendingFilter_ReturnsNothing_WhenNoSearchWasHandedOver()
    {
        IGlobalSearchState state = new GlobalSearchState();

        Assert.Null(state.ConsumePendingFilter());
    }

    [Fact]
    public void ConsumePendingFilter_ReturnsTheStoredTerm()
    {
        IGlobalSearchState state = new GlobalSearchState();
        state.SetPendingFilter("clean code");

        Assert.Equal("clean code", state.ConsumePendingFilter());
    }

    [Fact]
    public void ConsumePendingFilter_ClearsTheTerm_SoTheNextPageOpensUnfiltered()
    {
        IGlobalSearchState state = new GlobalSearchState();
        state.SetPendingFilter("clean code");

        Assert.Equal("clean code", state.ConsumePendingFilter());
        Assert.Null(state.ConsumePendingFilter());
    }

    [Fact]
    public void SetPendingFilter_TrimsTheTerm()
    {
        IGlobalSearchState state = new GlobalSearchState();
        state.SetPendingFilter("  clean code  ");

        Assert.Equal("clean code", state.ConsumePendingFilter());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPendingFilter_StoresNothing_ForABlankTerm(string term)
    {
        IGlobalSearchState state = new GlobalSearchState();
        state.SetPendingFilter(term);

        Assert.Null(state.ConsumePendingFilter());
    }

    [Fact]
    public void SetPendingFilter_ReplacesAnUnconsumedTerm()
    {
        IGlobalSearchState state = new GlobalSearchState();
        state.SetPendingFilter("first");
        state.SetPendingFilter("second");

        Assert.Equal("second", state.ConsumePendingFilter());
    }
}

public class GlobalSearchResultTests
{
    [Fact]
    public void Caption_IsTheGroupAlone_WhenThereIsNoSupportingDetail()
    {
        var result = new GlobalSearchResult { Group = "Page", PrimaryText = "Reports", Open = () => Task.CompletedTask };

        Assert.Equal("Page", result.Caption);
    }

    [Fact]
    public void Caption_JoinsGroupAndSupportingDetail()
    {
        var result = new GlobalSearchResult
        {
            Group = "Product",
            PrimaryText = "Clean Code",
            SecondaryText = "BK-001",
            Open = () => Task.CompletedTask
        };

        Assert.Equal("Product  •  BK-001", result.Caption);
    }

    [Fact]
    public void Caption_IgnoresBlankSupportingDetail_SoRowsNeverEndInASeparator()
    {
        var result = new GlobalSearchResult
        {
            Group = "Customer",
            PrimaryText = "Walk-in",
            SecondaryText = "   ",
            Open = () => Task.CompletedTask
        };

        Assert.Equal("Customer", result.Caption);
    }
}
