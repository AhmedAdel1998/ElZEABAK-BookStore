using CommunityToolkit.Mvvm.ComponentModel;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Base class for all presentation view models.
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string title = string.Empty;
}
