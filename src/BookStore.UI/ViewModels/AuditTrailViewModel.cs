using System.Collections.ObjectModel;
using BookStore.Application.Features.Audit.DTOs;
using BookStore.Application.Features.Audit.Handlers;
using BookStore.Application.Features.Audit.Queries;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Production audit trail screen view model.
/// </summary>
public partial class AuditTrailViewModel : BaseViewModel
{
    private readonly SearchAuditLogHandler _handler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string area = string.Empty;
    [ObservableProperty] private DateTime from = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime to = DateTime.Today.AddDays(1);
    [ObservableProperty] private int totalCount;

    /// <summary>Initializes a new instance of the <see cref="AuditTrailViewModel"/> class.</summary>
    public AuditTrailViewModel(SearchAuditLogHandler handler, INotificationService notificationService)
    {
        _handler = handler;
        _notificationService = notificationService;
        Title = "Audit Trail";
        _ = RefreshAsync();
    }

    /// <summary>Gets audit rows.</summary>
    public ObservableCollection<AuditLogEntryDto> Entries { get; } = [];

    /// <summary>Refreshes audit rows.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        var result = await _handler.HandleAsync(new SearchAuditLogQuery(
            new DateTimeOffset(From),
            new DateTimeOffset(To),
            string.IsNullOrWhiteSpace(Area) ? null : Area.Trim(),
            string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim()));
        IsBusy = false;

        Entries.Clear();
        if (!result.IsSuccess || result.Value is null)
        {
            TotalCount = 0;
            _notificationService.Show("Audit Trail", result.Error ?? "Unable to load audit trail.", NotificationSeverity.Warning);
            return;
        }

        TotalCount = result.Value.TotalCount;
        foreach (var entry in result.Value.Items)
        {
            Entries.Add(entry);
        }
    }
}
