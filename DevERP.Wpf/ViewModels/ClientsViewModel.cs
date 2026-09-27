using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevERP.Core.Enums;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevERP.Desktop.ViewModels;

public class SelectableMilestone : ObservableObject
{
    public Milestone Milestone { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public SelectableMilestone(Milestone milestone)
    {
        Milestone = milestone;
    }

    public bool IsCompleted => Milestone.IsCompleted;
    public string StatusText => Milestone.IsCompleted ? "Completed" : "Mark Complete";
    public string StatusBackground => Milestone.IsCompleted ? "#064E3B" : "#1E293B";
    public string StatusBorder => Milestone.IsCompleted ? "#059669" : "#334155";
    public string StatusForeground => Milestone.IsCompleted ? "#34D399" : "#94A3B8";

    public void Refresh()
    {
        OnPropertyChanged(nameof(Milestone));
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusBackground));
        OnPropertyChanged(nameof(StatusBorder));
        OnPropertyChanged(nameof(StatusForeground));
    }
}

public class ClientsViewModel : ViewModelBase
{
    private readonly IAppDbContext _dbContext;
    public Action<string>? RequestNavigation { get; set; }
    public Action<string, string, Wpf.Ui.Controls.InfoBarSeverity>? ShowNotification { get; set; }

    private void Notify(string message, string title = "DevERP", Wpf.Ui.Controls.InfoBarSeverity severity = Wpf.Ui.Controls.InfoBarSeverity.Success)
    {
        ShowNotification?.Invoke(message, title, severity);
    }

    private ObservableCollection<Client> _allClients = new();
    public ObservableCollection<Client> FilteredClients { get; } = new();

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

    private Client? _selectedClient;
    public Client? SelectedClient
    {
        get => _selectedClient;
        set
        {
            if (SetProperty(ref _selectedClient, value))
            {
                OnSelectedClientChanged();
            }
        }
    }

    public ObservableCollection<Project> ClientProjects { get; } = new();

    private Project? _selectedProject;
    public Project? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (SetProperty(ref _selectedProject, value))
            {
                OnSelectedProjectChanged();
            }
        }
    }

    public ObservableCollection<SelectableMilestone> ProjectMilestones { get; } = new();

    // Dialog / Drawer states
    private bool _isAddClientOpen;
    public bool IsAddClientOpen
    {
        get => _isAddClientOpen;
        set => SetProperty(ref _isAddClientOpen, value);
    }

    private bool _isAddProjectOpen;
    public bool IsAddProjectOpen
    {
        get => _isAddProjectOpen;
        set => SetProperty(ref _isAddProjectOpen, value);
    }

    private bool _isAddMilestoneOpen;
    public bool IsAddMilestoneOpen
    {
        get => _isAddMilestoneOpen;
        set => SetProperty(ref _isAddMilestoneOpen, value);
    }

    // New Client fields
    public string NewClientName { get; set; } = string.Empty;
    public string? NewClientCompany { get; set; }
    public string? NewClientEmail { get; set; }
    public string? NewClientPhone { get; set; }
    public string? NewClientAddress { get; set; }
    public string? NewClientNotes { get; set; }

    // New Project fields
    public string NewProjectName { get; set; } = string.Empty;
    public string? NewProjectDescription { get; set; }
    public decimal NewProjectBudget { get; set; } = 10000m;
    public BillingType NewProjectBillingType { get; set; } = BillingType.FixedMilestone;

    // New Milestone fields
    public string NewMilestoneTitle { get; set; } = string.Empty;
    public string? NewMilestoneDescription { get; set; }
    public decimal NewMilestoneAmount { get; set; } = 5000m;
    public DateTime? NewMilestoneDueDate { get; set; } = DateTime.UtcNow.AddDays(14);

    // Commands
    public IAsyncRelayCommand RefreshCommand { get; }
    public IRelayCommand OpenAddClientCommand { get; }
    public IAsyncRelayCommand SaveClientCommand { get; }
    public IAsyncRelayCommand<Client> DeleteClientCommand { get; }
    public IRelayCommand ClearSelectionCommand { get; }
    public IRelayCommand ClearSearchCommand { get; }
    public IRelayCommand OpenAddProjectCommand { get; }
    public IAsyncRelayCommand SaveProjectCommand { get; }
    public IAsyncRelayCommand<Project> DeleteProjectCommand { get; }
    public IRelayCommand OpenAddMilestoneCommand { get; }
    public IAsyncRelayCommand SaveMilestoneCommand { get; }
    public IAsyncRelayCommand<SelectableMilestone> ToggleMilestoneCompleteCommand { get; }
    public IAsyncRelayCommand<SelectableMilestone> DeleteMilestoneCommand { get; }
    public IAsyncRelayCommand GenerateInvoiceFromMilestonesCommand { get; }

    public ClientsViewModel(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
        RefreshCommand = new AsyncRelayCommand(LoadClientsAsync);

        ClearSelectionCommand = new RelayCommand(() =>
        {
            SelectedClient = null;
            SelectedProject = null;
            ClientProjects.Clear();
            ProjectMilestones.Clear();
        });

        ClearSearchCommand = new RelayCommand(() =>
        {
            SearchText = string.Empty;
        });

        DeleteClientCommand = new AsyncRelayCommand<Client>(DeleteClientAsync);
        DeleteProjectCommand = new AsyncRelayCommand<Project>(DeleteProjectAsync);
        DeleteMilestoneCommand = new AsyncRelayCommand<SelectableMilestone>(DeleteMilestoneAsync);

        OpenAddClientCommand = new RelayCommand(() =>
        {
            NewClientName = string.Empty;
            NewClientCompany = string.Empty;
            NewClientEmail = string.Empty;
            NewClientPhone = string.Empty;
            NewClientAddress = string.Empty;
            NewClientNotes = string.Empty;
            OnPropertyChanged(nameof(NewClientName));
            OnPropertyChanged(nameof(NewClientCompany));
            OnPropertyChanged(nameof(NewClientEmail));
            OnPropertyChanged(nameof(NewClientPhone));
            OnPropertyChanged(nameof(NewClientAddress));
            OnPropertyChanged(nameof(NewClientNotes));
            IsAddClientOpen = true;
        });

        SaveClientCommand = new AsyncRelayCommand(SaveClientAsync);

        OpenAddProjectCommand = new RelayCommand(() =>
        {
            if (SelectedClient == null)
            {
                Notify("Please select a client first.", "Selection Required", Wpf.Ui.Controls.InfoBarSeverity.Warning);
                return;
            }
            NewProjectName = string.Empty;
            NewProjectDescription = string.Empty;
            NewProjectBudget = 10000m;
            OnPropertyChanged(nameof(NewProjectName));
            OnPropertyChanged(nameof(NewProjectDescription));
            OnPropertyChanged(nameof(NewProjectBudget));
            IsAddProjectOpen = true;
        });

        SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync);

        OpenAddMilestoneCommand = new RelayCommand(() =>
        {
            if (SelectedProject == null)
            {
                Notify("Please select a project first.", "Selection Required", Wpf.Ui.Controls.InfoBarSeverity.Warning);
                return;
            }
            NewMilestoneTitle = string.Empty;
            NewMilestoneDescription = string.Empty;
            NewMilestoneAmount = 5000m;
            NewMilestoneDueDate = DateTime.UtcNow.AddDays(14);
            OnPropertyChanged(nameof(NewMilestoneTitle));
            OnPropertyChanged(nameof(NewMilestoneDescription));
            OnPropertyChanged(nameof(NewMilestoneAmount));
            OnPropertyChanged(nameof(NewMilestoneDueDate));
            IsAddMilestoneOpen = true;
        });

        SaveMilestoneCommand = new AsyncRelayCommand(SaveMilestoneAsync);

        ToggleMilestoneCompleteCommand = new AsyncRelayCommand<SelectableMilestone>(async item =>
        {
            if (item == null) return;
            item.Milestone.IsCompleted = !item.Milestone.IsCompleted;
            item.Milestone.CompletedAt = item.Milestone.IsCompleted ? DateTime.UtcNow : null;
            await _dbContext.SaveChangesAsync();
            item.Refresh();
            Notify($"Milestone '{item.Milestone.Title}' {(item.Milestone.IsCompleted ? "marked complete" : "marked incomplete")}.", "Milestone Updated", Wpf.Ui.Controls.InfoBarSeverity.Informational);
        });

        GenerateInvoiceFromMilestonesCommand = new AsyncRelayCommand(GenerateInvoiceFromSelectedMilestonesAsync);
    }

    public override async Task InitializeAsync()
    {
        await LoadClientsAsync();
    }

    public async Task LoadClientsAsync()
    {
        IsBusy = true;
        try
        {
            var clients = await _dbContext.Clients
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Milestones)
                .OrderBy(c => c.Name)
                .ToListAsync();

            _allClients = new ObservableCollection<Client>(clients);
            ApplyFilter();

            if (SelectedClient != null)
            {
                SelectedClient = _allClients.FirstOrDefault(c => c.Id == SelectedClient.Id) ?? _allClients.FirstOrDefault();
            }
            else
            {
                SelectedClient = _allClients.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading clients: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        FilteredClients.Clear();
        var query = _allClients.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(c =>
                c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(c.Company) && c.Company.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.Email) && c.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var c in query)
        {
            FilteredClients.Add(c);
        }
    }

    private void OnSelectedClientChanged()
    {
        ClientProjects.Clear();
        ProjectMilestones.Clear();
        SelectedProject = null;

        if (SelectedClient != null)
        {
            foreach (var p in SelectedClient.Projects)
            {
                ClientProjects.Add(p);
            }
            SelectedProject = ClientProjects.FirstOrDefault();
        }
    }

    private void OnSelectedProjectChanged()
    {
        ProjectMilestones.Clear();
        if (SelectedProject != null)
        {
            foreach (var m in SelectedProject.Milestones.OrderBy(m => m.DueDate))
            {
                ProjectMilestones.Add(new SelectableMilestone(m));
            }
        }
    }

    private async Task SaveClientAsync()
    {
        if (string.IsNullOrWhiteSpace(NewClientName))
        {
            Notify("Client name is required.", "Validation Error", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        var client = new Client
        {
            Name = NewClientName.Trim(),
            Company = NewClientCompany?.Trim(),
            Email = NewClientEmail?.Trim(),
            Phone = NewClientPhone?.Trim(),
            Address = NewClientAddress?.Trim(),
            Notes = NewClientNotes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync();

        IsAddClientOpen = false;
        await LoadClientsAsync();
        SelectedClient = client;
        Notify($"Client '{client.Name}' created successfully.", "Client Added", Wpf.Ui.Controls.InfoBarSeverity.Success);
    }

    private async Task SaveProjectAsync()
    {
        if (SelectedClient == null || string.IsNullOrWhiteSpace(NewProjectName))
        {
            Notify("Project name is required.", "Validation Error", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        var project = new Project
        {
            Name = NewProjectName.Trim(),
            Description = NewProjectDescription?.Trim(),
            TotalBudget = NewProjectBudget,
            BillingType = NewProjectBillingType,
            Status = ProjectStatus.Active,
            ClientId = SelectedClient.Id,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();

        IsAddProjectOpen = false;
        await LoadClientsAsync();
        SelectedProject = ClientProjects.FirstOrDefault(p => p.Id == project.Id);
        Notify($"Project '{project.Name}' added to {SelectedClient.Name}.", "Project Added", Wpf.Ui.Controls.InfoBarSeverity.Success);
    }

    private async Task SaveMilestoneAsync()
    {
        if (SelectedProject == null || string.IsNullOrWhiteSpace(NewMilestoneTitle))
        {
            Notify("Milestone title is required.", "Validation Error", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        var milestone = new Milestone
        {
            Title = NewMilestoneTitle.Trim(),
            Description = NewMilestoneDescription?.Trim(),
            Amount = NewMilestoneAmount,
            DueDate = NewMilestoneDueDate,
            ProjectId = SelectedProject.Id,
            IsCompleted = false,
            IsInvoiced = false
        };

        _dbContext.Milestones.Add(milestone);
        await _dbContext.SaveChangesAsync();

        IsAddMilestoneOpen = false;
        await LoadClientsAsync();
        Notify($"Milestone '{milestone.Title}' added.", "Milestone Added", Wpf.Ui.Controls.InfoBarSeverity.Success);
    }

    private async Task GenerateInvoiceFromSelectedMilestonesAsync()
    {
        var selected = ProjectMilestones.Where(m => m.IsSelected && !m.Milestone.IsInvoiced).ToList();
        if (!selected.Any())
        {
            Notify("Please select at least one milestone that is not already invoiced.", "Selection Required", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }

        if (SelectedClient == null) return;

        try
        {
            var profile = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync();
            var currency = profile?.BaseCurrency ?? "EGP";

            // Generate invoice number
            var count = await _dbContext.Invoices.CountAsync() + 1;
            var invoiceNum = $"INV-{DateTime.UtcNow.Year}-{count:D3}";

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNum,
                IssueDate = DateTime.UtcNow.Date,
                DueDate = DateTime.UtcNow.Date.AddDays(14),
                Status = InvoiceStatus.Sent,
                ClientId = SelectedClient.Id,
                Currency = currency,
                ExchangeRateToBase = 1.0m,
                TaxRate = 0m,
                Notes = $"Invoice generated for completed milestone(s) on project: {SelectedProject?.Name}."
            };

            foreach (var sel in selected)
            {
                invoice.Items.Add(new InvoiceItem
                {
                    Description = $"{sel.Milestone.Title} (Milestone)",
                    Quantity = 1,
                    UnitPrice = sel.Milestone.Amount,
                    TotalPrice = sel.Milestone.Amount
                });

                sel.Milestone.IsInvoiced = true;
                sel.Milestone.Invoice = invoice;
            }

            invoice.RecalculateTotals();
            _dbContext.Invoices.Add(invoice);
            await _dbContext.SaveChangesAsync();

            Notify($"Invoice #{invoice.InvoiceNumber} created for {invoice.TotalAmount:N2} {invoice.Currency}!", "Invoice Created", Wpf.Ui.Controls.InfoBarSeverity.Success);
            RequestNavigation?.Invoke("Invoices");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to create invoice: {ex.Message}";
        }
    }

    private async Task DeleteClientAsync(Client? client)
    {
        client ??= SelectedClient;
        if (client == null) return;

        var invoiceCount = await _dbContext.Invoices.CountAsync(i => i.ClientId == client.Id);
        string confirmMessage = invoiceCount > 0
            ? $"Client '{client.Name}' has {invoiceCount} invoice(s) on record.\n\nDeleting this client will permanently remove the client, all related projects, milestones, and invoice records.\n\nDo you want to proceed?"
            : $"Are you sure you want to delete client '{client.Name}' and all associated projects & milestones?";

        var result = System.Windows.MessageBox.Show(
            confirmMessage,
            "Confirm Delete Client",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var clientToDelete = await _dbContext.Clients
                .Include(c => c.Projects)
                    .ThenInclude(p => p.Milestones)
                .Include(c => c.Invoices)
                    .ThenInclude(i => i.Items)
                .FirstOrDefaultAsync(c => c.Id == client.Id);

            if (clientToDelete != null)
            {
                if (clientToDelete.Invoices.Any())
                {
                    _dbContext.Invoices.RemoveRange(clientToDelete.Invoices);
                }

                _dbContext.Clients.Remove(clientToDelete);
                await _dbContext.SaveChangesAsync();

                var clientName = client.Name;
                await LoadClientsAsync();
                SelectedClient = FilteredClients.FirstOrDefault();
                Notify($"Client '{clientName}' has been permanently deleted.", "Client Removed", Wpf.Ui.Controls.InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting client: {ex.Message}";
            Notify($"Failed to delete client: {ex.Message}", "Delete Error", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }

    private async Task DeleteProjectAsync(Project? project)
    {
        project ??= SelectedProject;
        if (project == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete project '{project.Name}' and its associated milestones?",
            "Confirm Delete Project",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            var projToDelete = await _dbContext.Projects
                .Include(p => p.Milestones)
                .FirstOrDefaultAsync(p => p.Id == project.Id);

            if (projToDelete != null)
            {
                _dbContext.Projects.Remove(projToDelete);
                await _dbContext.SaveChangesAsync();
                ClientProjects.Remove(project);
                SelectedProject = ClientProjects.FirstOrDefault();
                Notify($"Project '{project.Name}' deleted.", "Project Removed", Wpf.Ui.Controls.InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting project: {ex.Message}";
            Notify($"Failed to delete project: {ex.Message}", "Delete Error", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }

    private async Task DeleteMilestoneAsync(SelectableMilestone? item)
    {
        if (item == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Are you sure you want to delete milestone '{item.Milestone.Title}'?",
            "Confirm Delete Milestone",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            _dbContext.Milestones.Remove(item.Milestone);
            await _dbContext.SaveChangesAsync();
            ProjectMilestones.Remove(item);
            Notify($"Milestone '{item.Milestone.Title}' deleted.", "Milestone Removed", Wpf.Ui.Controls.InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting milestone: {ex.Message}";
            Notify($"Failed to delete milestone: {ex.Message}", "Delete Error", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }
}
