using Microsoft.Win32;
using Serilog;

namespace SyncSound.Windows.Startup;

public interface IStartupRegistrationService
{
    bool IsRegistered();
    bool SetEnabled(bool enabled, bool startMinimized);
}

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    private const string RunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SyncSound";
    private readonly ILogger _logger;

    public StartupRegistrationService(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
    }

    public bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunSubKey, writable: false);
            return key?.GetValue(ValueName) is string val && !string.IsNullOrWhiteSpace(val);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to inspect startup registration");
            return false;
        }
    }

    public bool SetEnabled(bool enabled, bool startMinimized)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunSubKey, writable: true);
            if (key is null)
            {
                return false;
            }

            if (!enabled)
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
                return true;
            }

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                return false;
            }

            var command = startMinimized ? $"\"{exePath}\" --minimized" : $"\"{exePath}\"";
            key.SetValue(ValueName, command);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to update Windows startup registration");
            return false;
        }
    }
}
