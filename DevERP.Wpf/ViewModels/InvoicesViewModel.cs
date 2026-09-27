using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using DevERP.Core.Enums;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace DevERP.Desktop.ViewModels;

public class InvoicesViewModel : ViewModelBase
{
    private readonly IAppDbContext _dbContext;
    private readonly IInvoicePdfService _pdfService;
    public Action<string, string, Wpf.Ui.Controls.InfoBarSeverity>? ShowNotification { get; set; }

    private void Notify(string message, string title = "DevERP", Wpf.Ui.Controls.InfoBarSeverity severity = Wpf.Ui.Controls.InfoBarSeverity.Success)
    {
        ShowNotification?.Invoke(message, title, severity);
    }

    private ObservableCollection<Invoice> _allInvoices = new();
    public ObservableCollection<Invoice> FilteredInvoices { get; } = new();

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

    private string _statusFilter = "All";
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                ApplyFilter();
            }
        }
    }

    private Invoice? _selectedInvoice;
    public Invoice? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                OnPropertyChanged(nameof(CanMarkAsPaid));
            }
        }
    }

    public bool CanMarkAsPaid => SelectedInvoice != null && SelectedInvoice.Status != InvoiceStatus.Paid;

    // New Invoice Form
    private bool _isCreateInvoiceOpen;
    public bool IsCreateInvoiceOpen
    {
        get => _isCreateInvoiceOpen;
        set => SetProperty(ref _isCreateInvoiceOpen, value);
    }

    public ObservableCollection<Client> AvailableClients { get; } = new();
    private Client? _newInvoiceClient;
    public Client? NewInvoiceClient
    {
        get => _newInvoiceClient;
        set => SetProperty(ref _newInvoiceClient, value);
    }

    public string NewInvoiceNumber { get; set; } = string.Empty;
    public DateTime NewInvoiceIssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime NewInvoiceDueDate { get; set; } = DateTime.UtcNow.Date.AddDays(14);
    public string NewInvoiceCurrency { get; set; } = "EGP";
    public decimal NewInvoiceExchangeRate { get; set; } = 1.0m;
    public string? NewInvoiceNotes { get; set; }

    // Line items for new invoice
    public ObservableCollection<InvoiceItem> NewInvoiceItems { get; } = new();
    public string NewItemDescription { get; set; } = string.Empty;
    public decimal NewItemQty { get; set; } = 1;
    public decimal NewItemUnitPrice { get; set; } = 5000;

    // Commands
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand MarkAsPaidCommand { get; }
    public IAsyncRelayCommand ExportPdfCommand { get; }
    public IRelayCommand OpenCreateInvoiceCommand { get; }
    public IRelayCommand CloseCreateInvoiceCommand { get; }
    public IRelayCommand AddLineItemCommand { get; }
    public IRelayCommand<InvoiceItem> RemoveLineItemCommand { get; }
    public IAsyncRelayCommand SaveInvoiceCommand { get; }

    public InvoicesViewModel(IAppDbContext dbContext, IInvoicePdfService pdfService)
    {
        _dbContext = dbContext;
        _pdfService = pdfService;

        RefreshCommand = new AsyncRelayCommand(LoadInvoicesAsync);
        MarkAsPaidCommand = new AsyncRelayCommand(MarkAsPaidAsync);
        ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync);

        CloseCreateInvoiceCommand = new RelayCommand(() => IsCreateInvoiceOpen = false);

        OpenCreateInvoiceCommand = new RelayCommand(async () =>
        {
            await PrepareCreateInvoiceAsync();
            IsCreateInvoiceOpen = true;
        });

        AddLineItemCommand = new RelayCommand(() =>
        {
            if (string.IsNullOrWhiteSpace(NewItemDescription)) return;
            var item = new InvoiceItem
            {
                Description = NewItemDescription.Trim(),
                Quantity = NewItemQty > 0 ? NewItemQty : 1,
                UnitPrice = NewItemUnitPrice,
                TotalPrice = Math.Round((NewItemQty > 0 ? NewItemQty : 1) * NewItemUnitPrice, 2)
            };
            NewInvoiceItems.Add(item);
            NewItemDescription = string.Empty;
            NewItemQty = 1;
            NewItemUnitPrice = 0;
            OnPropertyChanged(nameof(NewItemDescription));
            OnPropertyChanged(nameof(NewItemQty));
            OnPropertyChanged(nameof(NewItemUnitPrice));
        });

        RemoveLineItemCommand = new RelayCommand<InvoiceItem>(item =>
        {
            if (item != null)
            {
                NewInvoiceItems.Remove(item);
            }
        });

        SaveInvoiceCommand = new AsyncRelayCommand(SaveInvoiceAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadInvoicesAsync();
    }

    public async Task LoadInvoicesAsync()
    {
        IsBusy = true;
        try
        {
            var invoices = await _dbContext.Invoices
                .Include(i => i.Client)
                .Include(i => i.Items)
                .OrderByDescending(i => i.IssueDate)
                .ThenByDescending(i => i.Id)
                .ToListAsync();

            _allInvoices = new ObservableCollection<Invoice>(invoices);
            ApplyFilter();

            if (SelectedInvoice != null)
            {
                SelectedInvoice = _allInvoices.FirstOrDefault(i => i.Id == SelectedInvoice.Id) ?? _allInvoices.FirstOrDefault();
            }
            else
            {
                SelectedInvoice = _allInvoices.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading invoices: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredInvoices.Clear();
        var query = _allInvoices.AsEnumerable();

        if (StatusFilter != "All")
        {
            if (Enum.TryParse<InvoiceStatus>(StatusFilter, out var status))
            {
                query = query.Where(i => i.Status == status);
            }
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(i =>
                i.InvoiceNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (i.Client != null && i.Client.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (i.Client?.Company != null && i.Client.Company.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var inv in query)
        {
            FilteredInvoices.Add(inv);
        }
    }

    private async Task MarkAsPaidAsync()
    {
        if (SelectedInvoice == null || SelectedInvoice.Status == InvoiceStatus.Paid) return;

        try
        {
            SelectedInvoice.Status = InvoiceStatus.Paid;
            SelectedInvoice.PaidAt = DateTime.UtcNow;

            var profile = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync();
            var fxRate = SelectedInvoice.ExchangeRateToBase > 0 ? SelectedInvoice.ExchangeRateToBase : 1.0m;
            var amountInBase = Math.Round(SelectedInvoice.TotalAmount * fxRate, 2);

            // Automatically create Inflow transaction in Cash Flow ledger
            var transaction = new CashFlowTransaction
            {
                Date = DateTime.UtcNow.Date,
                Type = TransactionType.Inflow,
                Amount = SelectedInvoice.TotalAmount,
                Currency = SelectedInvoice.Currency,
                ExchangeRate = fxRate,
                AmountInBaseCurrency = amountInBase,
                Category = "Client Payment",
                Description = $"Payment received for #{SelectedInvoice.InvoiceNumber} ({SelectedInvoice.Client?.Name})",
                Reference = SelectedInvoice.InvoiceNumber,
                InvoiceId = SelectedInvoice.Id
            };

            _dbContext.CashFlowTransactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            SelectedInvoice.CashFlowTransactionId = transaction.Id;
            await _dbContext.SaveChangesAsync();

            OnPropertyChanged(nameof(CanMarkAsPaid));
            Notify($"Invoice #{SelectedInvoice.InvoiceNumber} marked as PAID. +{amountInBase:N2} {profile?.BaseCurrency ?? "EGP"} synced to Cash Flow ledger!", "Invoice Paid", Wpf.Ui.Controls.InfoBarSeverity.Success);
            await LoadInvoicesAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error marking invoice as paid: {ex.Message}";
        }
    }

    private async Task ExportPdfAsync()
    {
        if (SelectedInvoice == null)
        {
            Notify("Please select an invoice first to export.", "Selection Required", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        try
        {
            var profile = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync() ?? new DeveloperProfile();

            var dialog = new SaveFileDialog
            {
                Title = "Export Invoice PDF",
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Invoice_{SelectedInvoice.InvoiceNumber}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                await _pdfService.GenerateInvoicePdfAsync(SelectedInvoice, profile, dialog.FileName);
                Notify($"Invoice #{SelectedInvoice.InvoiceNumber} exported successfully to:\n{dialog.FileName}", "PDF Saved Successfully", Wpf.Ui.Controls.InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error exporting PDF: {ex.Message}";
            Notify($"Export failed: {ex.Message}", "Export Failed", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }

    private async Task PrepareCreateInvoiceAsync()
    {
        var clients = await _dbContext.Clients.OrderBy(c => c.Name).ToListAsync();
        AvailableClients.Clear();
        foreach (var c in clients) AvailableClients.Add(c);
        NewInvoiceClient = AvailableClients.FirstOrDefault();

        var count = await _dbContext.Invoices.CountAsync() + 1;
        NewInvoiceNumber = $"INV-{DateTime.UtcNow.Year}-{count:D3}";
        NewInvoiceIssueDate = DateTime.UtcNow.Date;
        NewInvoiceDueDate = DateTime.UtcNow.Date.AddDays(14);
        NewInvoiceCurrency = "EGP";
        NewInvoiceExchangeRate = 1.0m;
        NewInvoiceNotes = "Payment terms: Net 14. Bank wire details on invoice.";

        NewInvoiceItems.Clear();
        NewItemDescription = "Consulting & Software Development Services";
        NewItemQty = 1;
        NewItemUnitPrice = 10000m;

        OnPropertyChanged(nameof(NewInvoiceNumber));
        OnPropertyChanged(nameof(NewInvoiceIssueDate));
        OnPropertyChanged(nameof(NewInvoiceDueDate));
        OnPropertyChanged(nameof(NewInvoiceCurrency));
        OnPropertyChanged(nameof(NewInvoiceExchangeRate));
        OnPropertyChanged(nameof(NewInvoiceNotes));
        OnPropertyChanged(nameof(NewItemDescription));
        OnPropertyChanged(nameof(NewItemQty));
        OnPropertyChanged(nameof(NewItemUnitPrice));
    }

    private async Task SaveInvoiceAsync()
    {
        if (NewInvoiceClient == null)
        {
            Notify("Please select a client for this invoice.", "Missing Client", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        if (!NewInvoiceItems.Any())
        {
            Notify("Please add at least one line item to the invoice.", "Missing Items", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        try
        {
            var invoice = new Invoice
            {
                InvoiceNumber = string.IsNullOrWhiteSpace(NewInvoiceNumber) ? $"INV-{DateTime.UtcNow.Ticks}" : NewInvoiceNumber.Trim(),
                ClientId = NewInvoiceClient.Id,
                IssueDate = NewInvoiceIssueDate,
                DueDate = NewInvoiceDueDate,
                Currency = NewInvoiceCurrency,
                ExchangeRateToBase = NewInvoiceExchangeRate > 0 ? NewInvoiceExchangeRate : 1.0m,
                Status = InvoiceStatus.Sent,
                Notes = NewInvoiceNotes?.Trim()
            };

            foreach (var item in NewInvoiceItems)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                });
            }

            invoice.RecalculateTotals();

            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync();

            IsCreateInvoiceOpen = false;
            await LoadInvoicesAsync();
            SelectedInvoice = invoice;
            Notify($"Invoice #{invoice.InvoiceNumber} created for {invoice.TotalAmount:N2} {invoice.Currency}!", "Invoice Created", Wpf.Ui.Controls.InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving invoice: {ex.Message}";
        }
    }
}
