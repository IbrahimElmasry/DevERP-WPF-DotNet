using System.IO;
using CommunityToolkit.Mvvm.Input;
using DevERP.Core.Interfaces;
using DevERP.Core.Models;
using DevERP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace DevERP.Desktop.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IAppDbContext _dbContext;
    public Action<string, string, Wpf.Ui.Controls.InfoBarSeverity>? ShowNotification { get; set; }
    public Action? OnProfileUpdated { get; set; }

    private void Notify(string message, string title = "DevERP", Wpf.Ui.Controls.InfoBarSeverity severity = Wpf.Ui.Controls.InfoBarSeverity.Success)
    {
        ShowNotification?.Invoke(message, title, severity);
    }

    private DeveloperProfile _profile = new();
    public DeveloperProfile Profile
    {
        get => _profile;
        set => SetProperty(ref _profile, value);
    }

    private string _databasePath = string.Empty;
    public string DatabasePath
    {
        get => _databasePath;
        set => SetProperty(ref _databasePath, value);
    }

    public IAsyncRelayCommand SaveSettingsCommand { get; }
    public IAsyncRelayCommand ExportDatabaseBackupCommand { get; }
    public IRelayCommand CreateDesktopShortcutCommand { get; }

    public SettingsViewModel(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
        DatabasePath = AppDbContext.GetDatabasePath();

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ExportDatabaseBackupCommand = new AsyncRelayCommand(ExportDatabaseBackupAsync);
        CreateDesktopShortcutCommand = new RelayCommand(CreateDesktopShortcut);
    }

    public override async Task InitializeAsync()
    {
        await LoadSettingsAsync();
    }

    public async Task LoadSettingsAsync()
    {
        IsBusy = true;
        try
        {
            var p = await _dbContext.DeveloperProfiles.FirstOrDefaultAsync();
            if (p != null)
            {
                Profile = p;
            }
            DatabasePath = AppDbContext.GetDatabasePath();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading settings: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveSettingsAsync()
    {
        IsBusy = true;
        try
        {
            if (string.IsNullOrWhiteSpace(Profile.FullName))
            {
                Notify("Developer name cannot be empty.", "Validation Error", Wpf.Ui.Controls.InfoBarSeverity.Warning);
                return;
            }

            _dbContext.DeveloperProfiles.Update(Profile);
            await _dbContext.SaveChangesAsync();

            OnProfileUpdated?.Invoke();
            Notify("Developer Profile and Banking coordinates saved successfully!", "Settings Saved", Wpf.Ui.Controls.InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving settings: {ex.Message}";
            Notify($"Error saving settings: {ex.Message}", "Save Failed", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExportDatabaseBackupAsync()
    {
        try
        {
            var srcPath = AppDbContext.GetDatabasePath();
            if (!File.Exists(srcPath))
            {
                Notify("Database file does not exist yet.", "Backup Notice", Wpf.Ui.Controls.InfoBarSeverity.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Export SQLite Database Backup",
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*",
                FileName = $"DevERP_Backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.db"
            };

            if (dialog.ShowDialog() == true)
            {
                // Flush WAL/changes
                await _dbContext.SaveChangesAsync();

                // Copy file safely
                File.Copy(srcPath, dialog.FileName, overwrite: true);
                Notify($"Database backup successfully exported to:\n{dialog.FileName}", "Backup Complete", Wpf.Ui.Controls.InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Backup failed: {ex.Message}";
            Notify($"Backup failed: {ex.Message}", "Backup Failed", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }

    private void CreateDesktopShortcut()
    {
        var result = DevERP.Desktop.Services.DesktopShortcutService.CreateShortcut();
        Notify(result.Message, result.Success ? "Shortcut Created" : "Shortcut Failed", result.Success ? Wpf.Ui.Controls.InfoBarSeverity.Success : Wpf.Ui.Controls.InfoBarSeverity.Error);
    }
}
