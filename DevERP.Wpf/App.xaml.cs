using System.Windows;
using DevERP.Core.Interfaces;
using DevERP.Infrastructure.Data;
using DevERP.Infrastructure.Services;
using DevERP.Desktop.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Appearance;

namespace DevERP.Desktop;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply Windows 11 Dark Theme by default
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);

        // Configure Dependency Injection container
        var services = new ServiceCollection();

        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            var dbPath = AppDbContext.GetDatabasePath();
            options.UseSqlite($"Data Source={dbPath}");
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Services
        services.AddSingleton<IInvoicePdfService, InvoicePdfService>();

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ClientsViewModel>();
        services.AddSingleton<InvoicesViewModel>();
        services.AddSingleton<CashFlowViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        // Windows
        services.AddSingleton<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Auto create database and seed default profile / sample data
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DatabaseInitializer.InitializeAsync(dbContext);
        }

        // Initialize MainViewModel
        var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        await mainViewModel.InitializeAsync();

        // Launch MainWindow
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
