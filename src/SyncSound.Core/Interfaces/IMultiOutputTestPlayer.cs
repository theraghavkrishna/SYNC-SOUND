using SyncSound.Core.Models;

namespace SyncSound.Core.Interfaces;

public sealed class OutputBranchStatusChangedEventArgs : EventArgs
{
    public OutputBranchStatusChangedEventArgs(OutputBranchStatus status)
    {
        Status = status;
    }

    public OutputBranchStatus Status { get; }
}

/// <summary>
/// Plays a synthesized test signal simultaneously across one or more Windows audio endpoints.
/// </summary>
public interface IMultiOutputTestPlayer : IAsyncDisposable
{
    bool IsPlaying { get; }
    IReadOnlyList<OutputBranchStatus> ActiveBranches { get; }

    event EventHandler<OutputBranchStatusChangedEventArgs>? OutputStatusChanged;
    event EventHandler<string>? LogMessage;
    event EventHandler<bool>? PlaybackStateChanged;

    Task<MultiOutputStartResult> StartAsync(
        IReadOnlyList<AudioDevice> targets,
        AudioTestMode mode,
        CancellationToken cancellationToken = default);

    Task StopAsync();
}
