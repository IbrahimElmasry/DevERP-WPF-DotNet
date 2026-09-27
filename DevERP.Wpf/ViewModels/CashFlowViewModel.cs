using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DevERP.Core.Enums;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevERP.Desktop.ViewModels;

public class CashFlowViewModel : ViewModelBase
{
    private readonly IAppDbContext _dbContext;
    public Action<string, string, Wpf.Ui.Controls.InfoBarSeverity>? ShowNotification { get; set; }

    private void Notify(string message, string title = "DevERP", Wpf.Ui.Controls.InfoBarSeverity severity = Wpf.Ui.Controls.InfoBarSeverity.Success)
    {
        ShowNotification?.Invoke(message, title, severity);
    }

    private ObservableCollection<CashFlowTransaction> _allTransactions = new();
    public ObservableCollection<CashFlowTransaction> FilteredTransactions { get; } = new();

    private string _typeFilter = "All";
    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (SetProperty(ref _typeFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    private string _categoryFilter = "All";
    public string CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            if (SetProperty(ref _categoryFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    private string _currencyFilter = "All";
    public string CurrencyFilter
    {
        get => _currencyFilter;
        set
        {
            if (SetProperty(ref _currencyFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    // Totals for filtered view
    private decimal _totalInflows;
    public decimal TotalInflows
    {
        get => _totalInflows;
        set => SetProperty(ref _totalInflows, value);
    }

    private decimal _totalOutflows;
    public decimal TotalOutflows
    {
        get => _totalOutflows;
        set => SetProperty(ref _totalOutflows, value);
    }

    private decimal _netFlow;
    public decimal NetFlow
    {
        get => _netFlow;
        set => SetProperty(ref _netFlow, value);
    }

    private string _baseCurrency = "EGP";
    public string BaseCurrency
    {
        get => _baseCurrency;
        set => SetProperty(ref _baseCurrency, value);
    }

    // Add Transaction Drawer
    private bool _isAddTransactionOpen;
    public bool IsAddTransactionOpen
    {
        get => _isAddTransactionOpen;
        set => SetProperty(ref _isAddTransactionOpen, value);
    }

    public TransactionType NewTxType { get; set; } = TransactionType.Inflow;
    public DateTime NewTxDate { get; set; } = DateTime.UtcNow.Date;
    public decimal NewTxAmount { get; set; } = 1000m;
    public string NewTxCurrency { get; set; } = "EGP";
    public decimal NewTxExchangeRate { get; set; } = 1.0m;
    public string NewTxCategory { get; set; } = "Client Payment";
    public string NewTxDescription { get; set; } = string.Empty;
    public string? NewTxReference { get; set; }

    public ObservableCollection<string> Categories { get; } = new()
    {
        "Client Payment",
        "VPS/Tools",
        "Tax",
        "Personal",
        "Software Licenses",
        "Hardware",
        "Office",
        "Other"
    };

    // Commands
    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand OpenAddTransactionCommand { get; }
    public IAsyncRelayCommand SaveTransactionCommand { get; }
    public IAsyncRelayCommand<CashFlowTransaction> DeleteTransactionCommand { get; }

    public CashFlowViewModel(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
        RefreshCommand = new AsyncRelayCommand(LoadTransactionsAsync);

        OpenAddTransactionCommand = new RelayCommand(async () =>
        {
            var profile = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync();
            NewTxType = TransactionType.Inflow;
            NewTxDate = DateTime.UtcNow.Date;
            NewTxAmount = 1000m;
            NewTxCurrency = profile?.BaseCurrency ?? "EGP";
            NewTxExchangeRate = 1.0m;
            NewTxCategory = "Client Payment";
            NewTxDescription = string.Empty;
            NewTxReference = string.Empty;

            OnPropertyChanged(nameof(NewTxType));
            OnPropertyChanged(nameof(NewTxDate));
            OnPropertyChanged(nameof(NewTxAmount));
            OnPropertyChanged(nameof(NewTxCurrency));
            OnPropertyChanged(nameof(NewTxExchangeRate));
            OnPropertyChanged(nameof(NewTxCategory));
            OnPropertyChanged(nameof(NewTxDescription));
            OnPropertyChanged(nameof(NewTxReference));

            IsAddTransactionOpen = true;
        });

        SaveTransactionCommand = new AsyncRelayCommand(SaveTransactionAsync);
        DeleteTransactionCommand = new AsyncRelayCommand<CashFlowTransaction>(DeleteTransactionAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadTransactionsAsync();
    }

    public async Task LoadTransactionsAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync();
            if (profile != null)
            {
                BaseCurrency = profile.BaseCurrency;
            }

            var transactions = await _dbContext.CashFlowTransactions
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .ToListAsync();

            _allTransactions = new ObservableCollection<CashFlowTransaction>(transactions);
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading transactions: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredTransactions.Clear();
        var query = _allTransactions.AsEnumerable();

        if (TypeFilter != "All")
        {
            if (Enum.TryParse<TransactionType>(TypeFilter, out var type))
            {
                query = query.Where(t => t.Type == type);
            }
        }

        if (CategoryFilter != "All")
        {
            query = query.Where(t => t.Category.Equals(CategoryFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (CurrencyFilter != "All")
        {
            query = query.Where(t => t.Currency.Equals(CurrencyFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(t =>
                t.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                t.Category.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (t.Reference != null && t.Reference.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        var list = query.ToList();
        foreach (var t in list)
        {
            FilteredTransactions.Add(t);
        }

        TotalInflows = list.Where(t => t.Type == TransactionType.Inflow).Sum(t => t.AmountInBaseCurrency);
        TotalOutflows = list.Where(t => t.Type == TransactionType.Outflow).Sum(t => t.AmountInBaseCurrency);
        NetFlow = TotalInflows - TotalOutflows;
    }

    private async Task SaveTransactionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTxDescription))
        {
            Notify("Description is required.", "Validation Error", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        if (NewTxAmount <= 0)
        {
            Notify("Amount must be greater than zero.", "Validation Error", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        try
        {
            var rate = NewTxExchangeRate > 0 ? NewTxExchangeRate : 1.0m;
            var tx = new CashFlowTransaction
            {
                Type = NewTxType,
                Date = NewTxDate,
                Amount = NewTxAmount,
                Currency = string.IsNullOrWhiteSpace(NewTxCurrency) ? BaseCurrency : NewTxCurrency.ToUpperInvariant(),
                ExchangeRate = rate,
                AmountInBaseCurrency = Math.Round(NewTxAmount * rate, 2),
                Category = NewTxCategory,
                Description = NewTxDescription.Trim(),
                Reference = NewTxReference?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.CashFlowTransactions.Add(tx);
            await _dbContext.SaveChangesAsync();

            IsAddTransactionOpen = false;
            await LoadTransactionsAsync();
            Notify($"Transaction of {tx.Amount:N2} {tx.Currency} ({tx.Type}) recorded.", "Ledger Updated", Wpf.Ui.Controls.InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving transaction: {ex.Message}";
        }
    }

    private async Task DeleteTransactionAsync(CashFlowTransaction? tx)
    {
        if (tx == null) return;
        try
        {
            _dbContext.CashFlowTransactions.Remove(tx);
            await _dbContext.SaveChangesAsync();
            await LoadTransactionsAsync();
            Notify("Transaction removed from cash flow ledger.", "Transaction Deleted", Wpf.Ui.Controls.InfoBarSeverity.Informational);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting transaction: {ex.Message}";
        }
    }
}
