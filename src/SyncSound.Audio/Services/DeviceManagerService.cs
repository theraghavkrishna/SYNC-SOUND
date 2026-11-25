using Serilog;
using SyncSound.Core.Interfaces;
using SyncSound.Core.Models;

namespace SyncSound.Audio.Services;

/// <summary>
/// Manages real Windows audio endpoints, selection state, and local settings persistence.
/// </summary>
public sealed class DeviceManagerService : IDeviceManagerService
{
    private readonly IAudioDeviceEnumerator _enumerator;
    private readonly ISettingsService _settingsService;
    private readonly ILogger _logger;
    private readonly Lock _gate = new();
    private List<AudioDevice> _devices = [];
    private bool _disposed;

    public DeviceManagerService(
        IAudioDeviceEnumerator enumerator,
        ISettingsService settingsService,
        ILogger? logger = null)
    {
        _enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _logger = logger ?? Log.Logger;
        _enumerator.DeviceChanged += OnEndpointChanged;
    }

    public IReadOnlyList<AudioDevice> Devices
    {
        get
        {
            lock (_gate)
            {
                return _devices.ToList();
            }
        }
    }

    public IReadOnlyList<AudioDevice> SelectedDevices
    {
        get
        {
            lock (_gate)
            {
                return _devices.Where(d => d.IsSelected && d.IsAvailable).ToList();
            }
        }
    }

    public AudioDevice? DefaultDevice
    {
        get
        {
            lock (_gate)
            {
                return _devices.FirstOrDefault(d => d.IsDefault && d.IsAvailable);
            }
        }
    }

    public event EventHandler? DevicesUpdated;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);
        Refresh();
    }

    public void Refresh()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var endpoints = _enumerator.GetRenderEndpoints(includeInactive: false);
            var savedSelected = _settingsService.Current.SelectedDeviceIds
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var currentSelected = _devices.Count > 0
                ? _devices.Where(d => d.IsSelected).Select(d => d.Id).ToHashSet(StringComparer.OrdinalIgnoreCase)
                : savedSelected;

            var updated = new List<AudioDevice>(endpoints.Count);
            foreach (var endpoint in endpoints)
            {
                var shouldSelect = _settingsService.Current.AutoSelectPreferredDevices
                    ? currentSelected.Contains(endpoint.Id) || savedSelected.Contains(endpoint.Id)
                    : currentSelected.Contains(endpoint.Id);

                endpoint.IsSelected = shouldSelect;
                endpoint.IsSource = endpoint.IsDefault;
                updated.Add(endpoint);
            }

            _devices = updated;
        }

        DevicesUpdated?.Invoke(this, EventArgs.Empty);
    }

    public void SetDeviceSelected(string deviceId, bool isSelected)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return;
        }

        lock (_gate)
        {
            var device = _devices.FirstOrDefault(d => string.Equals(d.Id, deviceId, StringComparison.OrdinalIgnoreCase));
            if (device is null || device.IsSelected == isSelected)
            {
                return;
            }

            device.IsSelected = isSelected;
            PersistSelectionLocked();
        }

        DevicesUpdated?.Invoke(this, EventArgs.Empty);
        _ = _settingsService.SaveAsync();
    }

    public void SelectAllEligible()
    {
        lock (_gate)
        {
            foreach (var device in _devices)
            {
                if (device.IsAvailable && !device.IsHandsFreeProfile)
                {
                    device.IsSelected = true;
                }
            }

            PersistSelectionLocked();
        }

        DevicesUpdated?.Invoke(this, EventArgs.Empty);
        _ = _settingsService.SaveAsync();
    }

    public void ClearSelection()
    {
        lock (_gate)
        {
            foreach (var device in _devices)
            {
                device.IsSelected = false;
            }

            PersistSelectionLocked();
        }

        DevicesUpdated?.Invoke(this, EventArgs.Empty);
        _ = _settingsService.SaveAsync();
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _enumerator.DeviceChanged -= OnEndpointChanged;
    }

    private void PersistSelectionLocked()
    {
        var selectedIds = _devices
            .Where(d => d.IsSelected)
            .Select(d => d.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _settingsService.Current.SelectedDeviceIds = selectedIds;
        if (selectedIds.Count > 0)
        {
            _settingsService.Current.PreferredDeviceIds = selectedIds;
        }
    }

    private void OnEndpointChanged(object? sender, AudioDeviceChangedEventArgs e)
    {
        _logger.Debug("Endpoint change detected: {ChangeKind} ({DeviceId})", e.ChangeKind, e.DeviceId);
        Refresh();
    }
}
