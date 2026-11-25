using SyncSound.Core.Models;

namespace SyncSound.Core.Interfaces;

public interface IDeviceManagerService : IDisposable
{
    IReadOnlyList<AudioDevice> Devices { get; }
    IReadOnlyList<AudioDevice> SelectedDevices { get; }
    AudioDevice? DefaultDevice { get; }

    event EventHandler? DevicesUpdated;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    void Refresh();
    void SetDeviceSelected(string deviceId, bool isSelected);
    void SelectAllEligible();
    void ClearSelection();
}

public interface IMusicPlayerService : IAsyncDisposable
{
    PlaybackState CurrentState { get; }

    event EventHandler<PlaybackState>? StateChanged;

    Task LoadFilesAsync(IEnumerable<string> filePaths, bool autoPlay = true);
    Task PlayTrackAtAsync(int index);
    Task TogglePlayPauseAsync();
    Task StopAsync();
    Task NextAsync();
    Task PreviousAsync();
    void Seek(TimeSpan position);
    void SetVolume(float volume);
    void RemoveFromQueue(int index);
    void ClearQueue();
}

public interface ISystemSharingService : IAsyncDisposable
{
    SharingState CurrentState { get; }

    event EventHandler<SharingState>? StateChanged;

    Task<SharingState> StartSharingAsync(CancellationToken cancellationToken = default);
    Task StopSharingAsync();
    Task RefreshTargetsAsync();
}
