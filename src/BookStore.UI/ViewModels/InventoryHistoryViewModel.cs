using System.Collections.ObjectModel;
using BookStore.Application.Features.Inventory.DTOs;
using BookStore.Application.Features.Inventory.Queries.GetInventoryHistory;
using BookStore.Domain.Enums;
using BookStore.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InventoryHandlers = BookStore.Application.Features.Inventory.Handlers;

namespace BookStore.UI.ViewModels;

/// <summary>
/// Inventory history view model.
/// </summary>
public partial class InventoryHistoryViewModel : BaseViewModel
{
    private readonly InventoryHandlers.GetInventoryHistoryHandler _historyHandler;
    private readonly INotificationService _notificationService;

    [ObservableProperty] private DateTime? dateFrom;
    [ObservableProperty] private DateTime? dateTo;
    [ObservableProperty] private InventoryTransactionType? transactionType;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalCount;

    /// <summary>Initializes a new instance of the <see cref="InventoryHistoryViewModel"/> class.</summary>
    public InventoryHistoryViewModel(InventoryHandlers.GetInventoryHistoryHandler historyHandler, INotificationService notificationService)
    {
        _historyHandler = historyHandler;
        _notificationService = notificationService;
        Title = "Stock Ledger";
        TransactionTypes = [InventoryTransactionType.InitialStock, InventoryTransactionType.ManualAdjustment, InventoryTransactionType.Correction, InventoryTransactionType.Damage, InventoryTransactionType.Return, InventoryTransactionType.Sale];
        _ = LoadAsync();
    }

    /// <summary>Gets ledger rows.</summary>
    public ObservableCollection<InventoryTransactionDto> Transactions { get; } = [];

    /// <summary>Gets transaction types.</summary>
    public ObservableCollection<InventoryTransactionType> TransactionTypes { get; }

    /// <summary>Refreshes history.</summary>
    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    /// <summary>Searches history.</summary>
    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IsBusy = true;
        var result = await _historyHandler.HandleAsync(new GetInventoryHistoryRequest(null, ToOffset(DateFrom), ToOffset(DateTo), TransactionType, null, PageNumber, PageSize));
        Transactions.Clear();
        if (result.IsSuccess && result.Value is not null)
        {
            foreach (var item in result.Value.Items)
            {
                Transactions.Add(item);
            }

            TotalCount = result.Value.TotalCount;
        }
        else
        {
            _notificationService.Show("Inventory", result.Error ?? "Unable to load stock ledger.", NotificationSeverity.Error);
        }

        IsBusy = false;
    }

    private static DateTimeOffset? ToOffset(DateTime? value)
    {
        return value.HasValue ? new DateTimeOffset(value.Value) : null;
    }
}
