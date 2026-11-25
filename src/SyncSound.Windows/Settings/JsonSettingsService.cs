using System.IO;
using System.Text.Json;
using Serilog;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Windows.Settings;

/// <summary>
/// Persists SYNC SOUND settings locally in %LocalAppData%\SyncSound\settings.json.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsPath;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _ioGate = new(1, 1);

    public JsonSettingsService(string? settingsPath = null, ILogger? logger = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SyncSound",
            "settings.json");
        _logger = logger ?? Log.Logger;
        Current = new AppSettings();
    }

    public AppSettings Current { get; private set; }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _ioGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_settingsPath))
            {
                Current = new AppSettings();
                return Current;
            }

            await using var stream = File.OpenRead(_settingsPath);
            var loaded = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            Current = loaded ?? new AppSettings();
            Current.SelectedDeviceIds ??= [];
            Current.PreferredDeviceIds ??= [];
            Current.PerDeviceDelayMs ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            return Current;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load settings from {Path}; using defaults", _settingsPath);
            Current = new AppSettings();
            return Current;
        }
        finally
        {
            _ioGate.Release();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await _ioGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tempPath = _settingsPath + ".tmp";
            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, Current, JsonOptions, cancellationToken).ConfigureAwait(false);
            }

            File.Move(tempPath, _settingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save settings to {Path}", _settingsPath);
        }
        finally
        {
            _ioGate.Release();
        }
    }
}
