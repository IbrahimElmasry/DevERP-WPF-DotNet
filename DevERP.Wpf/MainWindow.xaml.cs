using DevERP.Desktop.Services;
using DevERP.Desktop.ViewModels;
using Wpf.Ui.Controls;

namespace DevERP.Desktop;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Restore window bounds and window state
        WindowStateService.Restore(this);

        Closing += (s, e) =>
        {
            WindowStateService.Save(this);
        };
    }
}