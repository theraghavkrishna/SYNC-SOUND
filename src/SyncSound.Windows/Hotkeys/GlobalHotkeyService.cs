using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using Serilog;

namespace SyncSound.Windows.Hotkeys;

public interface IGlobalHotkeyService : IDisposable
{
    string CurrentShortcut { get; }
    bool IsRegistered { get; }

    event EventHandler? HotkeyPressed;

    bool TryRegister(string shortcut, out string? errorMessage);
    void Unregister();
}

/// <summary>
/// Manages global Windows hotkeys via Win32 RegisterHotKey on a hidden message-only HwndSource.
/// </summary>
public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x5359; // "SY"

    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;
    private const uint ModNoRepeat = 0x4000;

    private readonly ILogger _logger;
    private HwndSource? _hwndSource;
    private bool _disposed;

    public GlobalHotkeyService(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
    }

    public string CurrentShortcut { get; private set; } = "Ctrl+Shift+S";
    public bool IsRegistered { get; private set; }

    public event EventHandler? HotkeyPressed;

    public static bool TryParseShortcut(string? shortcut, out uint modifiers, out uint virtualKey, out string normalized)
    {
        modifiers = 0;
        virtualKey = 0;
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(shortcut))
        {
            return false;
        }

        var parts = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Key? parsedKey = null;
        var hasCtrl = false;
        var hasShift = false;
        var hasAlt = false;

        foreach (var part in parts)
        {
            if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                part.Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                hasCtrl = true;
                modifiers |= ModControl;
            }
            else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                hasShift = true;
                modifiers |= ModShift;
            }
            else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                hasAlt = true;
                modifiers |= ModAlt;
            }
            else if (Enum.TryParse<Key>(part, ignoreCase: true, out var key) && key != Key.None)
            {
                parsedKey = key;
            }
            else
            {
                return false;
            }
        }

        if (parsedKey is null || modifiers == 0)
        {
            return false;
        }

        virtualKey = (uint)KeyInterop.VirtualKeyFromKey(parsedKey.Value);
        if (virtualKey == 0)
        {
            return false;
        }

        var tokens = new List<string>();
        if (hasCtrl) tokens.Add("Ctrl");
        if (hasShift) tokens.Add("Shift");
        if (hasAlt) tokens.Add("Alt");
        tokens.Add(parsedKey.Value.ToString());
        normalized = string.Join("+", tokens);
        return true;
    }

    public bool TryRegister(string shortcut, out string? errorMessage)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!TryParseShortcut(shortcut, out var modifiers, out var virtualKey, out var normalized))
        {
            errorMessage = "Choose a shortcut with Ctrl, Shift, or Alt plus a key.";
            return false;
        }

        EnsureHwndSource();
        Unregister();

        var hwnd = _hwndSource!.Handle;
        var registered = RegisterHotKey(hwnd, HotkeyId, modifiers | ModNoRepeat, virtualKey);
        if (!registered)
        {
            var err = Marshal.GetLastWin32Error();
            _logger.Warning("RegisterHotKey failed for {Shortcut} (Win32 error {Error})", normalized, err);
            IsRegistered = false;
            errorMessage = "This shortcut is already being used.";
            return false;
        }

        CurrentShortcut = normalized;
        IsRegistered = true;
        errorMessage = null;
        _logger.Information("Registered global hotkey {Shortcut}", normalized);
        return true;
    }

    public void Unregister()
    {
        if (_hwndSource is not null && IsRegistered)
        {
            UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            IsRegistered = false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Unregister();

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }

    private void EnsureHwndSource()
    {
        if (_hwndSource is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("SyncSoundHotkeySink")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
