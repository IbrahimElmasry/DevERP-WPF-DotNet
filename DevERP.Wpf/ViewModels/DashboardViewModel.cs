using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DevERP.Core.Enums;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevERP.Desktop.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IAppDbContext _dbContext;
    public Action<string>? RequestNavigation { get; set; }

    private decimal _thirtyDaysInflow;
    public decimal ThirtyDaysInflow
    {
        get => _thirtyDaysInflow;
        set => SetProperty(ref _thirtyDaysInflow, value);
    }

    private decimal _thirtyDaysOutflow;
    public decimal ThirtyDaysOutflow
    {
        get => _thirtyDaysOutflow;
        set => SetProperty(ref _thirtyDaysOutflow, value);
    }

    private decimal _netBalance;
    public decimal NetBalance
    {
        get => _netBalance;
        set => SetProperty(ref _netBalance, value);
    }

    private int _activeClientsCount;
    public int ActiveClientsCount
    {
        get => _activeClientsCount;
        set => SetProperty(ref _activeClientsCount, value);
    }

    private int _pendingInvoicesCount;
    public int PendingInvoicesCount
    {
        get => _pendingInvoicesCount;
        set => SetProperty(ref _pendingInvoicesCount, value);
    }

    private decimal _pendingInvoicesAmount;
    public decimal PendingInvoicesAmount
    {
        get => _pendingInvoicesAmount;
        set => SetProperty(ref _pendingInvoicesAmount, value);
    }

    private string _baseCurrency = "EGP";
    public string BaseCurrency
    {
        get => _baseCurrency;
        set => SetProperty(ref _baseCurrency, value);
    }

    public ObservableCollection<CashFlowTransaction> RecentTransactions { get; } = new();
    public ObservableCollection<Invoice> PendingInvoices { get; } = new();

    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand<string> NavigateCommand { get; }

    public DashboardViewModel(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        NavigateCommand = new RelayCommand<string>(tag =>
        {
            if (!string.IsNullOrEmpty(tag))
            {
                RequestNavigation?.Invoke(tag);
            }
        });
    }

    public override async Task InitializeAsync()
    {
        await LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var profile = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync();
            if (profile != null)
            {
                BaseCurrency = profile.BaseCurrency;
            }

            var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-30);

            // Compute 30-day cash flows
            var transactions = await _dbContext.CashFlowTransactions
                .Where(t => t.Date >= thirtyDaysAgo)
                .ToListAsync();

            ThirtyDaysInflow = transactions
                .Where(t => t.Type == TransactionType.Inflow)
                .Sum(t => t.AmountInBaseCurrency);

            ThirtyDaysOutflow = transactions
                .Where(t => t.Type == TransactionType.Outflow)
                .Sum(t => t.AmountInBaseCurrency);

            NetBalance = ThirtyDaysInflow - ThirtyDaysOutflow;

            // Clients
            ActiveClientsCount = await _dbContext.Clients
                .CountAsync(c => c.Projects.Any(p => p.Status == ProjectStatus.Active));

            // Invoices pending
            var pending = await _dbContext.Invoices
                .Include(i => i.Client)
                .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Draft || i.Status == InvoiceStatus.Overdue)
                .OrderByDescending(i => i.DueDate)
                .ToListAsync();

            PendingInvoicesCount = pending.Count;
            PendingInvoicesAmount = pending.Sum(i => i.TotalAmount * (i.ExchangeRateToBase > 0 ? i.ExchangeRateToBase : 1m));

            // Recent transactions
            var recentTx = await _dbContext.CashFlowTransactions
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .Take(7)
                .ToListAsync();

            RecentTransactions.Clear();
            foreach (var tx in recentTx)
            {
                RecentTransactions.Add(tx);
            }

            // Pending invoices list
            PendingInvoices.Clear();
            foreach (var inv in pending.Take(5))
            {
                PendingInvoices.Add(inv);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading dashboard: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
