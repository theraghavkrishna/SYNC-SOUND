using System.Diagnostics;
using System.IO;
using Serilog;

namespace SyncSound.Windows.Bluetooth;

public interface IWindowsSettingsLauncher
{
    bool OpenBluetoothSettings();
    bool OpenSoundSettings();
    bool OpenLogsFolder();
}

public sealed class WindowsSettingsLauncher : IWindowsSettingsLauncher
{
    private readonly string _logsFolder;
    private readonly ILogger _logger;

    public WindowsSettingsLauncher(string? logsFolder = null, ILogger? logger = null)
    {
        _logsFolder = logsFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SyncSound",
            "logs");
        _logger = logger ?? Log.Logger;
    }

    public bool OpenBluetoothSettings() => LaunchUri("ms-settings:bluetooth");

    public bool OpenSoundSettings() => LaunchUri("ms-settings:sound");

    public bool OpenLogsFolder()
    {
        try
        {
            Directory.CreateDirectory(_logsFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = _logsFolder,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to open logs folder {LogsFolder}", _logsFolder);
            return false;
        }
    }

    private bool LaunchUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to open Windows settings URI {Uri}", uri);
            return false;
        }
    }
}
