using Microsoft.Win32;
using System.Windows;
using SyncSound.Core.Models;

namespace SyncSound.App.Services;

/// <summary>
/// Manages Light, Dark, and System-synchronized theme switching across all windows and popups.
/// </summary>
public sealed class ThemeService : IDisposable
{
    private const string LightPalettePath = "pack://application:,,,/SyncSound.App;component/Themes/LightPalette.xaml";
    private const string DarkPalettePath = "pack://application:,,,/SyncSound.App;component/Themes/DarkPalette.xaml";

    private AppThemeMode _mode = AppThemeMode.System;
    private bool _isDarkActive;

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    public AppThemeMode CurrentMode => _mode;
    public bool IsDarkActive => _isDarkActive;

    public event EventHandler<bool>? ThemeChanged;

    public void ApplyTheme(AppThemeMode mode)
    {
        _mode = mode;
        var shouldBeDark = mode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => IsWindowsDarkMode()
        };

        ApplyPaletteDictionary(shouldBeDark);
    }

    private void ApplyPaletteDictionary(bool dark)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        var merged = app.Resources.MergedDictionaries;
        var targetUri = new Uri(dark ? DarkPalettePath : LightPalettePath, UriKind.RelativeOrAbsolute);

        // Replace only the palette dictionary (index 0 or matching *Palette.xaml)
        var existingIndex = -1;
        for (var i = 0; i < merged.Count; i++)
        {
            var src = merged[i].Source?.OriginalString;
            if (src is not null && src.EndsWith("Palette.xaml", StringComparison.OrdinalIgnoreCase))
            {
                existingIndex = i;
                break;
            }
        }

        var newDict = new ResourceDictionary { Source = targetUri };
        if (existingIndex >= 0)
        {
            merged[existingIndex] = newDict;
        }
        else
        {
            merged.Insert(0, newDict);
        }

        if (_isDarkActive != dark)
        {
            _isDarkActive = dark;
            ThemeChanged?.Invoke(this, dark);
        }
        else
        {
            _isDarkActive = dark;
        }
    }

    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (_mode != AppThemeMode.System)
        {
            return;
        }

        var app = Application.Current;
        app?.Dispatcher.BeginInvoke(() => ApplyTheme(AppThemeMode.System));
    }

    private static bool IsWindowsDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
            {
                return value == 0;
            }
        }
        catch
        {
            // Default to Warm Cream Light Mode if registry cannot be read.
        }

        return false;
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
    }
}
