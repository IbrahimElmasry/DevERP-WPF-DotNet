using System.IO;
using System.Text.Json;
using System.Windows;

namespace DevERP.Desktop.Services;

public class WindowStateData
{
    public double Width { get; set; } = 1280;
    public double Height { get; set; } = 820;
    public double? Left { get; set; }
    public double? Top { get; set; }
    public WindowState WindowState { get; set; } = WindowState.Normal;
}

public static class WindowStateService
{
    private static string GetConfigFilePath()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevERP");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "window_state.json");
    }

    public static void Restore(Window window)
    {
        try
        {
            var path = GetConfigFilePath();
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var state = JsonSerializer.Deserialize<WindowStateData>(json);
                if (state != null)
                {
                    if (state.Width >= window.MinWidth) window.Width = state.Width;
                    if (state.Height >= window.MinHeight) window.Height = state.Height;

                    // Verify if saved position is within virtual screen
                    if (state.Left.HasValue && state.Top.HasValue)
                    {
                        var virtualLeft = SystemParameters.VirtualScreenLeft;
                        var virtualTop = SystemParameters.VirtualScreenTop;
                        var virtualWidth = SystemParameters.VirtualScreenWidth;
                        var virtualHeight = SystemParameters.VirtualScreenHeight;

                        if (state.Left.Value >= virtualLeft && state.Left.Value + 100 <= virtualLeft + virtualWidth &&
                            state.Top.Value >= virtualTop && state.Top.Value + 100 <= virtualTop + virtualHeight)
                        {
                            window.WindowStartupLocation = WindowStartupLocation.Manual;
                            window.Left = state.Left.Value;
                            window.Top = state.Top.Value;
                        }
                        else
                        {
                            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        }
                    }
                    else
                    {
                        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    }

                    if (state.WindowState == WindowState.Maximized)
                    {
                        window.WindowState = WindowState.Maximized;
                    }
                    return;
                }
            }
        }
        catch
        {
            // Fallback to center screen
        }

        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    public static void Save(Window window)
    {
        try
        {
            var state = new WindowStateData
            {
                WindowState = window.WindowState,
                Width = window.WindowState == WindowState.Normal ? window.Width : window.RestoreBounds.Width,
                Height = window.WindowState == WindowState.Normal ? window.Height : window.RestoreBounds.Height,
                Left = window.WindowState == WindowState.Normal ? window.Left : window.RestoreBounds.Left,
                Top = window.WindowState == WindowState.Normal ? window.Top : window.RestoreBounds.Top
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetConfigFilePath(), json);
        }
        catch
        {
            // Ignore error on shutdown
        }
    }
}
