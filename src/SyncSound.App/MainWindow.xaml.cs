using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Animation;
using SyncSound.App.ViewModels;
using SyncSound.Core.Interfaces;

namespace SyncSound.App;

public partial class MainWindow : Window
{
    private readonly ISettingsService _settingsService;

    public MainWindow(MainViewModel viewModel, ISettingsService settingsService)
    {
        InitializeComponent();
        DataContext = viewModel;
        _settingsService = settingsService;
        Loaded += OnLoaded;
    }

    public bool AllowClose { get; set; }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose && _settingsService?.Current?.CloseToTray == true)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Fit comfortably inside the monitor work area across 100%-200% Windows scaling
        var area = SystemParameters.WorkArea;
        if (Width > area.Width - 20)
        {
            Width = Math.Max(MinWidth, area.Width - 20);
        }

        if (Height > area.Height - 20)
        {
            Height = Math.Max(MinHeight, area.Height - 20);
        }

        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, (area.Height - Height) / 2);

        if (SystemParameters.ClientAreaAnimation)
        {
            Opacity = 0;
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
