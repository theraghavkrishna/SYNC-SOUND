using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SyncSound.App.Services;

/// <summary>
/// Applies native Windows 11 DWM rounded corner attributes cleanly without harming ClearType rendering.
/// </summary>
public static class DwmWindowHelper
{
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWCP_ROUND = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static void ApplyRoundedCorners(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            var preference = DWMWCP_ROUND;
            _ = DwmSetWindowAttribute(hwnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
        }
        catch
        {
            // Gracefully no-op on Windows 10 or systems where DWM rounded attributes are unavailable.
        }
    }
}
