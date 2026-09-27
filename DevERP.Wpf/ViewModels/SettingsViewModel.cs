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
    private readonly ICurrencySyncService _currencyService;

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
    public IAsyncRelayCommand RestoreDatabaseBackupCommand { get; }
    public IRelayCommand CreateDesktopShortcutCommand { get; }
    public IRelayCommand<string> OpenUrlCommand { get; }
    public IAsyncRelayCommand SyncLiveFxRatesCommand { get; }
    public IRelayCommand UploadLogoCommand { get; }
    public IRelayCommand RemoveLogoCommand { get; }

    public SettingsViewModel(IAppDbContext dbContext, ICurrencySyncService currencyService)
    {
        _dbContext = dbContext;
        _currencyService = currencyService;
        DatabasePath = AppDbContext.GetDatabasePath();

        SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
        ExportDatabaseBackupCommand = new AsyncRelayCommand(ExportDatabaseBackupAsync);
        RestoreDatabaseBackupCommand = new AsyncRelayCommand(RestoreDatabaseBackupAsync);
        CreateDesktopShortcutCommand = new RelayCommand(CreateDesktopShortcut);
        SyncLiveFxRatesCommand = new AsyncRelayCommand(SyncLiveFxRatesAsync);
        UploadLogoCommand = new RelayCommand(UploadLogo);
        RemoveLogoCommand = new RelayCommand(RemoveLogo);

        OpenUrlCommand = new RelayCommand<string>(url =>
        {
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch { }
            }
        });
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

            if (Profile.IsPinEnabled)
            {
                if (string.IsNullOrWhiteSpace(Profile.SecurityPin) ||
                    Profile.SecurityPin.Length != 4 ||
                    !Profile.SecurityPin.All(char.IsDigit))
                {
                    Notify("Security PIN must be exactly 4 numeric digits (e.g. 1234).", "Validation Error", Wpf.Ui.Controls.InfoBarSeverity.Warning);
                    return;
                }
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

    private async Task SyncLiveFxRatesAsync()
    {
        IsBusy = true;
        try
        {
            var res = await _currencyService.FetchLatestRatesToEgpAsync();
            if (res.Success)
            {
                Profile.UsdToEgpRate = res.UsdToEgp;
                Profile.EurToEgpRate = res.EurToEgp;
                Profile.SarToEgpRate = res.SarToEgp;
                OnPropertyChanged(nameof(Profile));
                Notify($"Live FX Rates updated successfully:\n1 USD = {res.UsdToEgp:F2} EGP\n1 EUR = {res.EurToEgp:F2} EGP\n1 SAR = {res.SarToEgp:F2} EGP", "FX Rates Synced", Wpf.Ui.Controls.InfoBarSeverity.Success);
            }
            else
            {
                Notify($"Unable to fetch live rates: {res.ErrorMessage}", "Sync Notice", Wpf.Ui.Controls.InfoBarSeverity.Warning);
            }
        }
        catch (Exception ex)
        {
            Notify($"Sync error: {ex.Message}", "Error", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UploadLogo()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Company or Brand Logo",
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var dir = Path.GetDirectoryName(AppDbContext.GetDatabasePath()) ?? "";
                var ext = Path.GetExtension(dialog.FileName);
                var dest = Path.Combine(dir, $"invoice_logo{ext}");
                File.Copy(dialog.FileName, dest, overwrite: true);

                Profile.LogoPath = dest;
                OnPropertyChanged(nameof(Profile));
                Notify("Invoice logo uploaded successfully! Click 'Save Changes' to apply.", "Logo Uploaded", Wpf.Ui.Controls.InfoBarSeverity.Success);
            }
        }
        catch (Exception ex)
        {
            Notify($"Error uploading logo: {ex.Message}", "Upload Failed", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }

    private void RemoveLogo()
    {
        Profile.LogoPath = null;
        OnPropertyChanged(nameof(Profile));
        Notify("Logo removed from invoices.", "Logo Removed", Wpf.Ui.Controls.InfoBarSeverity.Informational);
    }

    private async Task RestoreDatabaseBackupAsync()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select SQLite Database to Restore",
                Filter = "SQLite Database (*.db)|*.db|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                var confirm = System.Windows.MessageBox.Show(
                    "Restoring a database will replace all current clients, invoices, and ledger records.\n\nAre you sure you want to proceed?",
                    "Confirm Database Restore",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (confirm == System.Windows.MessageBoxResult.Yes)
                {
                    var targetDb = AppDbContext.GetDatabasePath();
                    // Copy over
                    File.Copy(dialog.FileName, targetDb, overwrite: true);
                    Notify("Database restored successfully! Please restart DevERP to reload all data.", "Restore Complete", Wpf.Ui.Controls.InfoBarSeverity.Success);
                    await LoadSettingsAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Notify($"Database restore failed: {ex.Message}", "Restore Failed", Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }
}
