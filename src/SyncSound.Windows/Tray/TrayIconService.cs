using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SyncSound.Windows.Tray;

public interface ITrayIconService : IDisposable
{
    event EventHandler? OpenRequested;
    event EventHandler? ToggleSidebarRequested;
    event EventHandler? ToggleSharingRequested;
    event EventHandler? QuitRequested;

    void Initialize();
    void UpdateSharingState(bool isSharing, int activeDeviceCount);
}

/// <summary>
/// Manages the Windows notification area icon and context menu for background SYNC SOUND operation.
/// </summary>
public sealed class TrayIconService : ITrayIconService
{
    private NotifyIcon? _notifyIcon;
    private ToolStripMenuItem? _shareMenuItem;
    private Icon? _appIcon;
    private bool _disposed;

    public event EventHandler? OpenRequested;
    public event EventHandler? ToggleSidebarRequested;
    public event EventHandler? ToggleSharingRequested;
    public event EventHandler? QuitRequested;

    public void Initialize()
    {
        if (_notifyIcon is not null || _disposed)
        {
            return;
        }

        _appIcon = CreateSyncSoundIcon();

        var menu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("Open SYNC SOUND");
        openItem.Font = new Font(openItem.Font, FontStyle.Bold);
        openItem.Click += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);

        var sidebarItem = new ToolStripMenuItem("Toggle Quick Sidebar");
        sidebarItem.Click += (_, _) => ToggleSidebarRequested?.Invoke(this, EventArgs.Empty);

        _shareMenuItem = new ToolStripMenuItem("Start sharing");
        _shareMenuItem.Click += (_, _) => ToggleSharingRequested?.Invoke(this, EventArgs.Empty);

        var quitItem = new ToolStripMenuItem("Quit SYNC SOUND");
        quitItem.Click += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);

        menu.Items.Add(openItem);
        menu.Items.Add(sidebarItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_shareMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(quitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "SYNC SOUND — Ready to listen",
            Visible = true,
            ContextMenuStrip = menu
        };

        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSharingState(bool isSharing, int activeDeviceCount)
    {
        if (_notifyIcon is null || _shareMenuItem is null)
        {
            return;
        }

        _shareMenuItem.Text = isSharing ? "Stop sharing" : "Start sharing";
        var tooltip = isSharing
            ? $"SYNC SOUND — Sharing to {activeDeviceCount} device(s)"
            : "SYNC SOUND — Ready to listen";

        // NotifyIcon.Text has a 63-character Windows limit.
        _notifyIcon.Text = tooltip.Length > 63 ? tooltip[..63] : tooltip;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _appIcon?.Dispose();
        _appIcon = null;
    }

    private static Icon CreateSyncSoundIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var bgBrush = new SolidBrush(Color.FromArgb(232, 93, 63)); // Signal Coral #E85D3F
            g.FillEllipse(bgBrush, 1, 1, 30, 30);

            using var wavePen = new Pen(Color.White, 2.6f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            g.DrawLine(wavePen, 9, 13, 9, 19);
            g.DrawLine(wavePen, 14, 9, 14, 23);
            g.DrawLine(wavePen, 19, 11, 19, 21);
            g.DrawLine(wavePen, 24, 14, 24, 18);
        }

        var hIcon = bmp.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);
}
