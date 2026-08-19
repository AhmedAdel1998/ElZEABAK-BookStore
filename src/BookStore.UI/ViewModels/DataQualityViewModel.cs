using System.Collections.ObjectModel;
using BookStore.Application.Features.DataQuality.DTOs;
using BookStore.Application.Features.DataQuality.Handlers;
using BookStore.Application.Features.DataQuality.Queries;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Data quality center view model.
/// </summary>
public partial class DataQualityViewModel : BaseViewModel
{
    private readonly GetDataQualitySummaryHandler _handler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private DataQualitySummaryDto summary = new(0, 0, 0, 0, 0, 0, 0, []);
    [ObservableProperty] private decimal minimumMarginPercent = 10;

    /// <summary>Initializes a new instance of the <see cref="DataQualityViewModel"/> class.</summary>
    public DataQualityViewModel(GetDataQualitySummaryHandler handler, INotificationService notificationService)
    {
        _handler = handler;
        _notificationService = notificationService;
        Title = "Data Quality";
        _ = RefreshAsync();
    }

    /// <summary>Gets issue rows.</summary>
    public ObservableCollection<DataQualityIssueDto> Issues { get; } = [];

    /// <summary>Refreshes data quality checks.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsBusy = true;
        var result = await _handler.HandleAsync(new GetDataQualitySummaryQuery(MinimumMarginPercent));
        IsBusy = false;

        Issues.Clear();
        if (!result.IsSuccess || result.Value is null)
        {
            Summary = new DataQualitySummaryDto(0, 0, 0, 0, 0, 0, 0, []);
            _notificationService.Show("Data Quality", result.Error ?? "Unable to run data quality checks.", NotificationSeverity.Warning);
            return;
        }

        Summary = result.Value;
        foreach (var issue in Summary.Issues)
        {
            Issues.Add(issue);
        }
    }
}
