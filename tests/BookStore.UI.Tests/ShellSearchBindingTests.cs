using System.IO;
using System.Reflection;
using BookStore.UI.Navigation;
using BookStore.UI.ViewModels;

namespace BookStore.UI.Tests;

/// <summary>
/// Guards the wiring between the shell markup and its view model. A binding to a member that does
/// not exist fails silently in WPF -- the control simply does nothing, which is how a search box
/// can look finished and never search. Each case here asserts both halves of the contract: the
/// markup asks for the member, and the type exposes it.
/// </summary>
public class ShellSearchBindingTests
{
    private static readonly string ShellMarkup = ReadShellMarkup();

    [Theory]
    [InlineData("SearchText")]
    [InlineData("IsSearchResultsOpen")]
    [InlineData("SearchStatus")]
    [InlineData("SearchResults")]
    [InlineData("OpenTopSearchResultCommand")]
    [InlineData("CloseSearchResultsCommand")]
    [InlineData("OpenSearchResultCommand")]
    public void ShellMarkup_BindsToAViewModelMemberThatExists(string member)
    {
        Assert.Contains(member, ShellMarkup, StringComparison.Ordinal);
        AssertPublicMember(typeof(AuthenticatedHomeViewModel), member);
    }

    [Theory]
    [InlineData("PrimaryText")]
    [InlineData("Caption")]
    public void ResultRows_BindToAResultMemberThatExists(string member)
    {
        Assert.Contains(member, ShellMarkup, StringComparison.Ordinal);
        AssertPublicMember(typeof(GlobalSearchResult), member);
    }

    [Fact]
    public void SearchBox_CarriesTheKeyBindingsThatDriveIt()
    {
        // Without these the box can only be driven by mouse, and Enter -- the way a cashier
        // actually uses a search box -- does nothing.
        Assert.Contains("Key=\"Return\" Command=\"{Binding OpenTopSearchResultCommand}\"", ShellMarkup, StringComparison.Ordinal);
        Assert.Contains("Key=\"Escape\" Command=\"{Binding CloseSearchResultsCommand}\"", ShellMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchBox_UpdatesItsSourceOnEveryKeystroke()
    {
        // The view model debounces before querying. If the binding waited for focus to leave
        // instead, nothing would search until the cashier clicked elsewhere.
        Assert.Contains("Text=\"{Binding SearchText, UpdateSourceTrigger=PropertyChanged}\"", ShellMarkup, StringComparison.Ordinal);
    }

    private static void AssertPublicMember(Type type, string member)
    {
        var found = type.GetMember(member, BindingFlags.Public | BindingFlags.Instance);
        Assert.True(found.Length > 0, $"{type.Name} does not expose a public '{member}', so the binding to it is dead.");
    }

    private static string ReadShellMarkup()
    {
        // Copied next to the test assembly by the project file. Fully qualified because a WPF
        // project also has a System.Windows.Shapes.Path in scope.
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Markup", "AuthenticatedHomeView.xaml");
        Assert.True(System.IO.File.Exists(path), $"The shell markup was not copied to the test output: {path}");
        return System.IO.File.ReadAllText(path);
    }
}
