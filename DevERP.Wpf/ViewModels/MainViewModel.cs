using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.Input;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using DevERP.Desktop.Views;
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

    public IRelayCommand LockAppCommand { get; }

    // Command Palette Quick Switcher
    private bool _isCommandPaletteOpen;
    public bool IsCommandPaletteOpen
    {
        get => _isCommandPaletteOpen;
        set => SetProperty(ref _isCommandPaletteOpen, value);
    }

    private string _commandPaletteSearchText = string.Empty;
    public string CommandPaletteSearchText
    {
        get => _commandPaletteSearchText;
        set
        {
            if (SetProperty(ref _commandPaletteSearchText, value))
            {
                _ = FilterCommandPaletteAsync(value);
            }
        }
    }

    public ObservableCollection<CommandPaletteItem> CommandPaletteResults { get; } = new();

    public IRelayCommand OpenCommandPaletteCommand { get; }
    public IRelayCommand CloseCommandPaletteCommand { get; }
    public IRelayCommand<CommandPaletteItem> ExecutePaletteItemCommand { get; }

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
        DashboardVM.RequestInvoiceDetails = inv =>
        {
            NavigateTo("Invoices");
            InvoicesVM.SelectedInvoice = inv;
        };
        DashboardVM.RequestNewClient = () =>
        {
            NavigateTo("Clients");
            ClientsVM.OpenAddClientCommand.Execute(null);
        };
        DashboardVM.RequestNewInvoice = () =>
        {
            NavigateTo("Invoices");
            InvoicesVM.OpenCreateInvoiceCommand.Execute(null);
        };
        DashboardVM.RequestNewTransaction = () =>
        {
            NavigateTo("CashFlow");
            CashFlowVM.OpenAddTransactionCommand.Execute(null);
        };
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

        LockAppCommand = new RelayCommand(LockApp);

        OpenCommandPaletteCommand = new RelayCommand(() =>
        {
            IsCommandPaletteOpen = true;
            CommandPaletteSearchText = string.Empty;
            _ = FilterCommandPaletteAsync(string.Empty);
        });

        CloseCommandPaletteCommand = new RelayCommand(() =>
        {
            IsCommandPaletteOpen = false;
        });

        ExecutePaletteItemCommand = new RelayCommand<CommandPaletteItem>(item =>
        {
            if (item != null)
            {
                IsCommandPaletteOpen = false;
                item.Execute?.Invoke();
            }
        });
    }

    private void LockApp()
    {
        var pin = string.IsNullOrWhiteSpace(Profile.SecurityPin) ? "1234" : Profile.SecurityPin;
        var pinWindow = new PinLoginWindow(pin, Profile.FullName, Profile.ProfessionalTitle);
        if (Application.Current.MainWindow != null)
        {
            pinWindow.Owner = Application.Current.MainWindow;
        }

        bool? unlocked = pinWindow.ShowDialog();
        if (unlocked != true)
        {
            Application.Current.Shutdown();
        }
        else
        {
            TriggerNotification("DevERP workspace unlocked successfully.", "Welcome Back", Wpf.Ui.Controls.InfoBarSeverity.Success);
        }
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

    private async Task FilterCommandPaletteAsync(string query)
    {
        CommandPaletteResults.Clear();
        var q = query?.Trim().ToLowerInvariant() ?? string.Empty;

        // 1. Navigation items
        var navItems = new List<CommandPaletteItem>
        {
            new() { Title = "Dashboard & Overview", Subtitle = "View high-level metrics, cash flow trajectory & KPIs", Category = "Navigation", Icon = "Home24", IconColor = "#38BDF8", Execute = () => NavigateTo("dashboard") },
            new() { Title = "Clients & Projects", Subtitle = "Manage client roster, contracts, and active deliverables", Category = "Navigation", Icon = "PeopleTeam24", IconColor = "#A78BFA", Execute = () => NavigateTo("clients") },
            new() { Title = "Invoices & Billing", Subtitle = "Create invoices, track payments, send WhatsApp links", Category = "Navigation", Icon = "DocumentBulletList24", IconColor = "#34D399", Execute = () => NavigateTo("invoices") },
            new() { Title = "Cash Flow Ledger", Subtitle = "Track income deposits and business disbursements", Category = "Navigation", Icon = "Money24", IconColor = "#F59E0B", Execute = () => NavigateTo("cashflow") },
            new() { Title = "Profile & Settings", Subtitle = "Configure company details, bank coordinates, and security PIN", Category = "Navigation", Icon = "Settings24", IconColor = "#94A3B8", Execute = () => NavigateTo("settings") },
        };

        // 2. Fast Actions
        var actionItems = new List<CommandPaletteItem>
        {
            new() { Title = "Create New Invoice", Subtitle = "Draft a new billable client invoice", Category = "Action", Icon = "Add24", IconColor = "#34D399", Execute = () => { NavigateTo("invoices"); InvoicesVM.OpenCreateInvoiceCommand.Execute(null); } },
            new() { Title = "Add New Client", Subtitle = "Register a new client or company account", Category = "Action", Icon = "PersonAdd24", IconColor = "#A78BFA", Execute = () => { NavigateTo("clients"); ClientsVM.OpenAddClientCommand.Execute(null); } },
            new() { Title = "Record Expense / Revenue", Subtitle = "Add a transaction entry to the cash flow ledger", Category = "Action", Icon = "ReceiptMoney24", IconColor = "#F59E0B", Execute = () => { NavigateTo("cashflow"); CashFlowVM.OpenAddTransactionCommand.Execute(null); } },
            new() { Title = "Lock DevERP Workspace", Subtitle = "Trigger immediate security PIN protection", Category = "Security", Icon = "LockClosed24", IconColor = "#FB7185", Execute = LockApp },
        };

        if (string.IsNullOrWhiteSpace(q))
        {
            foreach (var a in actionItems) CommandPaletteResults.Add(a);
            foreach (var n in navItems) CommandPaletteResults.Add(n);
            return;
        }

        // Filter Actions & Nav
        foreach (var a in actionItems.Where(x => x.Title.ToLowerInvariant().Contains(q) || x.Subtitle.ToLowerInvariant().Contains(q)))
        {
            CommandPaletteResults.Add(a);
        }
        foreach (var n in navItems.Where(x => x.Title.ToLowerInvariant().Contains(q) || x.Subtitle.ToLowerInvariant().Contains(q)))
        {
            CommandPaletteResults.Add(n);
        }

        // Filter Invoices from DB
        try
        {
            var invoices = await _dbContext.Invoices
                .Include(i => i.Client)
                .Where(i => i.InvoiceNumber.ToLower().Contains(q) ||
                            (i.Client != null && i.Client.Name.ToLower().Contains(q)))
                .Take(5)
                .ToListAsync();

            foreach (var inv in invoices)
            {
                CommandPaletteResults.Add(new CommandPaletteItem
                {
                    Title = $"{inv.InvoiceNumber} • {inv.TotalAmount:N2} {inv.Currency}",
                    Subtitle = $"Client: {inv.Client?.Name} | Status: {inv.Status} | Due: {inv.DueDate:yyyy-MM-dd}",
                    Category = "Invoice",
                    Icon = "DocumentBulletList24",
                    IconColor = "#38BDF8",
                    Execute = () =>
                    {
                        NavigateTo("invoices");
                        InvoicesVM.SelectedInvoice = inv;
                    }
                });
            }
        }
        catch { }

        // Filter Clients from DB
        try
        {
            var clients = await _dbContext.Clients
                .Where(c => c.Name.ToLower().Contains(q) ||
                            (c.Company != null && c.Company.ToLower().Contains(q)) ||
                            (c.Email != null && c.Email.ToLower().Contains(q)))
                .Take(5)
                .ToListAsync();

            foreach (var client in clients)
            {
                CommandPaletteResults.Add(new CommandPaletteItem
                {
                    Title = client.Name,
                    Subtitle = $"{client.Company} • {client.Email}",
                    Category = "Client",
                    Icon = "Person24",
                    IconColor = "#A78BFA",
                    Execute = () =>
                    {
                        NavigateTo("clients");
                        ClientsVM.SelectedClient = client;
                    }
                });
            }
        }
        catch { }

        // Filter Cash Flow Transactions
        try
        {
            var txs = await _dbContext.CashFlowTransactions
                .Where(t => t.Description.ToLower().Contains(q) || t.Category.ToLower().Contains(q))
                .Take(5)
                .ToListAsync();

            foreach (var tx in txs)
            {
                CommandPaletteResults.Add(new CommandPaletteItem
                {
                    Title = $"{tx.Description} ({tx.Amount:N2} {tx.Currency})",
                    Subtitle = $"{tx.Type} • {tx.Category} • {tx.Date:yyyy-MM-dd}",
                    Category = "Ledger",
                    Icon = "Money24",
                    IconColor = tx.Type == Core.Enums.TransactionType.Inflow ? "#34D399" : "#FB7185",
                    Execute = () => NavigateTo("cashflow")
                });
            }
        }
        catch { }

        if (CommandPaletteResults.Count == 0)
        {
            CommandPaletteResults.Add(new CommandPaletteItem
            {
                Title = "No matching results found",
                Subtitle = string.IsNullOrWhiteSpace(q) ? "Type to search actions, invoices, clients, or ledger entries" : $"No records matched \"{q}\"",
                Category = "Search",
                Icon = "Info24",
                IconColor = "#64748B",
                Execute = null
            });
        }
    }
}

public class CommandPaletteItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = "Search24";
    public string IconColor { get; set; } = "#38BDF8";
    public Action? Execute { get; set; }
}

