namespace SyncSound.Windows.WindowManagement;

/// <summary>
/// Ensures only one SYNC SOUND instance runs per user session and signals the active
/// instance to show/focus its window when launched a second time.
/// </summary>
public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexName = @"Local\SyncSound.SingleInstance.Mutex";
    private const string ActivationEventName = @"Local\SyncSound.SingleInstance.Activate";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activationEvent;
    private readonly CancellationTokenSource _cts = new();
    private readonly bool _ownsMutex;
    private Task? _listenerTask;

    private SingleInstanceManager(Mutex mutex, EventWaitHandle activationEvent, bool ownsMutex)
    {
        _mutex = mutex;
        _activationEvent = activationEvent;
        _ownsMutex = ownsMutex;
    }

    public event EventHandler? SecondInstanceLaunched;

    public static SingleInstanceManager Acquire(out bool isPrimaryInstance)
    {
        var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        var activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
        isPrimaryInstance = createdNew;
        return new SingleInstanceManager(mutex, activationEvent, createdNew);
    }

    public void StartListening()
    {
        if (!_ownsMutex || _listenerTask is not null)
        {
            return;
        }

        _listenerTask = Task.Run(() =>
        {
            var handles = new[] { _activationEvent, _cts.Token.WaitHandle };
            while (!_cts.IsCancellationRequested)
            {
                var signaled = WaitHandle.WaitAny(handles);
                if (signaled == 0 && !_cts.IsCancellationRequested)
                {
                    SecondInstanceLaunched?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    break;
                }
            }
        });
    }

    public void SignalPrimaryInstance()
    {
        try
        {
            _activationEvent.Set();
        }
        catch
        {
            // Ignore signaling failures if primary instance is exiting.
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _listenerTask?.Wait(500);
        }
        catch
        {
            // Ignore teardown timeout.
        }

        _cts.Dispose();
        _activationEvent.Dispose();

        if (_ownsMutex)
        {
            try
            {
                _mutex.ReleaseMutex();
            }
            catch
            {
                // Ignore if already released.
            }
        }

        _mutex.Dispose();
    }
}
