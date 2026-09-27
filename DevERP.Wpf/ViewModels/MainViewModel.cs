using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevERP.Desktop.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IAppDbContext _dbContext;

    public DashboardViewModel DashboardVM { get; }
    public ClientsViewModel ClientsVM { get; }
    public InvoicesViewModel InvoicesVM { get; }
    public CashFlowViewModel CashFlowVM { get; }
    public SettingsViewModel SettingsVM { get; }

    private ViewModelBase _currentViewModel;
    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetProperty(ref _currentViewModel, value);
    }

    private string _currentViewTitle = "Dashboard";
    public string CurrentViewTitle
    {
        get => _currentViewTitle;
        set => SetProperty(ref _currentViewTitle, value);
    }

    private string _currentViewTag = "Dashboard";
    public string CurrentViewTag
    {
        get => _currentViewTag;
        set => SetProperty(ref _currentViewTag, value);
    }

    // Header Developer Profile info
    private DeveloperProfile _profile = new();
    public DeveloperProfile Profile
    {
        get => _profile;
        set => SetProperty(ref _profile, value);
    }

    // Toast / InfoBar notification
    private string? _statusMessage;
    public string? StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private string _notificationTitle = "Notification";
    public string NotificationTitle
    {
        get => _notificationTitle;
        set => SetProperty(ref _notificationTitle, value);
    }

    private Wpf.Ui.Controls.InfoBarSeverity _notificationSeverity = Wpf.Ui.Controls.InfoBarSeverity.Success;
    public Wpf.Ui.Controls.InfoBarSeverity NotificationSeverity
    {
        get => _notificationSeverity;
        set => SetProperty(ref _notificationSeverity, value);
    }

    private bool _isStatusMessageVisible;
    public bool IsStatusMessageVisible
    {
        get => _isStatusMessageVisible;
        set => SetProperty(ref _isStatusMessageVisible, value);
    }

    private DispatcherTimer? _notificationTimer;

    public IRelayCommand<string> NavigateCommand { get; }
    public IRelayCommand DismissNotificationCommand { get; }

    public MainViewModel(
        IAppDbContext dbContext,
        DashboardViewModel dashboardVM,
        ClientsViewModel clientsVM,
        InvoicesViewModel invoicesVM,
        CashFlowViewModel cashFlowVM,
        SettingsViewModel settingsVM)
    {
        _dbContext = dbContext;
        DashboardVM = dashboardVM;
        ClientsVM = clientsVM;
        InvoicesVM = invoicesVM;
        CashFlowVM = cashFlowVM;
        SettingsVM = settingsVM;

        _currentViewModel = dashboardVM;

        // Wire navigation callbacks between ViewModels
        DashboardVM.RequestNavigation = NavigateTo;
        ClientsVM.RequestNavigation = NavigateTo;
        ClientsVM.ShowNotification = (msg, title, sev) => TriggerNotification(msg, title, sev);
        InvoicesVM.ShowNotification = (msg, title, sev) => TriggerNotification(msg, title, sev);
        CashFlowVM.ShowNotification = (msg, title, sev) => TriggerNotification(msg, title, sev);
        SettingsVM.ShowNotification = (msg, title, sev) => TriggerNotification(msg, title, sev);
        SettingsVM.OnProfileUpdated = () => _ = LoadProfileAsync();

        NavigateCommand = new RelayCommand<string>(viewName =>
        {
            if (!string.IsNullOrEmpty(viewName))
            {
                NavigateTo(viewName);
            }
        });

        DismissNotificationCommand = new RelayCommand(() =>
        {
            IsStatusMessageVisible = false;
        });
    }

    public override async Task InitializeAsync()
    {
        await LoadProfileAsync();
        await DashboardVM.InitializeAsync();
    }

    public async Task LoadProfileAsync()
    {
        try
        {
            var p = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync();
            if (p != null)
            {
                Profile = p;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading profile: {ex.Message}";
        }
    }

    public void NavigateTo(string viewName)
    {
        switch (viewName.ToLowerInvariant())
        {
            case "dashboard":
                CurrentViewModel = DashboardVM;
                CurrentViewTitle = "Dashboard & Overview";
                CurrentViewTag = "Dashboard";
                _ = DashboardVM.LoadDataAsync();
                break;
            case "clients":
            case "clients & projects":
                CurrentViewModel = ClientsVM;
                CurrentViewTitle = "Clients & Projects";
                CurrentViewTag = "Clients";
                _ = ClientsVM.LoadClientsAsync();
                break;
            case "invoices":
                CurrentViewModel = InvoicesVM;
                CurrentViewTitle = "Invoices & Billing";
                CurrentViewTag = "Invoices";
                _ = InvoicesVM.LoadInvoicesAsync();
                break;
            case "cashflow":
            case "cash flow ledger":
                CurrentViewModel = CashFlowVM;
                CurrentViewTitle = "Cash Flow Ledger";
                CurrentViewTag = "CashFlow";
                _ = CashFlowVM.LoadTransactionsAsync();
                break;
            case "settings":
                CurrentViewModel = SettingsVM;
                CurrentViewTitle = "Profile & Settings";
                CurrentViewTag = "Settings";
                _ = SettingsVM.LoadSettingsAsync();
                break;
        }
    }

    public void TriggerNotification(string message)
    {
        TriggerNotification(message, "Notice", Wpf.Ui.Controls.InfoBarSeverity.Success);
    }

    public void TriggerNotification(string message, string title, Wpf.Ui.Controls.InfoBarSeverity severity = Wpf.Ui.Controls.InfoBarSeverity.Success)
    {
        StatusMessage = message;
        NotificationTitle = string.IsNullOrWhiteSpace(title) ? "DevERP" : title;
        NotificationSeverity = severity;
        IsStatusMessageVisible = true;

        _notificationTimer?.Stop();
        _notificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _notificationTimer.Tick += (s, e) =>
        {
            IsStatusMessageVisible = false;
            _notificationTimer.Stop();
        };
        _notificationTimer.Start();
    }
}
