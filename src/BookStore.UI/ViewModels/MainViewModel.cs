using BookStore.UI.Navigation;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Root shell view model for the desktop application.
/// </summary>
public partial class MainViewModel : BaseViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    /// <param name="navigationService">The navigation service.</param>
    public MainViewModel(INavigationService navigationService)
    {
        NavigationService = navigationService;
        Title = "BookStore POS";
    }

    /// <summary>
    /// Gets the navigation service used by the shell.
    /// </summary>
    public INavigationService NavigationService { get; }
}
