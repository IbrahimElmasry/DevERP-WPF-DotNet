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
    public ObservableCollection<MonthlyCashFlowBar> MonthlyTrends { get; } = new();

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

            // 6-Month Monthly Trends
            MonthlyTrends.Clear();
            var sixMonthsAgo = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-5);
            var historyTxs = await _dbContext.CashFlowTransactions
                .Where(t => t.Date >= sixMonthsAgo)
                .ToListAsync();

            var monthlyData = new List<MonthlyCashFlowBar>();
            decimal maxFlow = 1m;

            for (int i = -5; i <= 0; i++)
            {
                var targetMonth = DateTime.UtcNow.AddMonths(i);
                var monthStart = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1);

                var monthTxs = historyTxs.Where(t => t.Date >= monthStart && t.Date < monthEnd).ToList();
                var mInflow = monthTxs.Where(t => t.Type == TransactionType.Inflow).Sum(t => t.AmountInBaseCurrency);
                var mOutflow = monthTxs.Where(t => t.Type == TransactionType.Outflow).Sum(t => t.AmountInBaseCurrency);
                var mNet = mInflow - mOutflow;

                if (mInflow > maxFlow) maxFlow = mInflow;
                if (mOutflow > maxFlow) maxFlow = mOutflow;

                monthlyData.Add(new MonthlyCashFlowBar
                {
                    MonthLabel = targetMonth.ToString("MMM yy"),
                    Inflow = mInflow,
                    Outflow = mOutflow,
                    Net = mNet,
                    Tooltip = $"{targetMonth:MMMM yyyy}\nInflow: {mInflow:N2} {BaseCurrency}\nOutflow: {mOutflow:N2} {BaseCurrency}\nNet: {mNet:N2} {BaseCurrency}"
                });
            }

            const double maxHeight = 80.0;
            foreach (var m in monthlyData)
            {
                m.InflowHeight = Math.Max(4, (double)(m.Inflow / maxFlow) * maxHeight);
                m.OutflowHeight = Math.Max(4, (double)(m.Outflow / maxFlow) * maxHeight);
                MonthlyTrends.Add(m);
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

public class MonthlyCashFlowBar
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
    public decimal Net { get; set; }
    public double InflowHeight { get; set; }
    public double OutflowHeight { get; set; }
    public string Tooltip { get; set; } = string.Empty;
}
