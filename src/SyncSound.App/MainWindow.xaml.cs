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
        if (!AllowClose && _settingsService.Current.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (SystemParameters.ClientAreaAnimation)
        {
            Opacity = 0;
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
        }
    }
}
