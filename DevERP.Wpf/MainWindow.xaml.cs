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

        // Command Palette Auto-Focus
        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsCommandPaletteOpen) && viewModel.IsCommandPaletteOpen)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PaletteSearchBox.Focus();
                    PaletteSearchBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
        };
    }

    private void Backdrop_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsCommandPaletteOpen = false;
        }
    }

    private void PaletteSearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        if (e.Key == System.Windows.Input.Key.Escape)
        {
            vm.IsCommandPaletteOpen = false;
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Enter)
        {
            if (vm.CommandPaletteResults.Count > 0)
            {
                var first = vm.CommandPaletteResults[0];
                if (first.Execute != null)
                {
                    vm.ExecutePaletteItemCommand.Execute(first);
                    e.Handled = true;
                }
            }
        }
    }
}