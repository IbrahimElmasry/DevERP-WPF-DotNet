using System.Diagnostics;
using System.IO;

namespace DevERP.Desktop.Services;

public static class DesktopShortcutService
{
    public static (bool Success, string Message) CreateShortcut()
    {
        try
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            var shortcutPath = Path.Combine(desktop, "DevERP.lnk");

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                using var curProcess = Process.GetCurrentProcess();
                exePath = curProcess.MainModule?.FileName;
            }

            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                return (false, "Could not determine application executable path.");
            }

            var appDir = Path.GetDirectoryName(exePath) ?? AppContext.BaseDirectory;

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return (false, "Windows Shell Host is not available on this system.");
            }

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null)
            {
                return (false, "Failed to initialize Windows Shell Host.");
            }

            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = appDir;
            shortcut.Description = "DevERP — Business Operating System for Developers";
            shortcut.IconLocation = $"{exePath},0";
            shortcut.Save();

            return (true, $"Desktop shortcut created successfully at: {shortcutPath}");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to create desktop shortcut: {ex.Message}");
        }
    }
}
