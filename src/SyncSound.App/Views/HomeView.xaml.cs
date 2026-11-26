using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SyncSound.App.ViewModels;

namespace SyncSound.App.Views;

public partial class HomeView : UserControl
{
    private MainViewModel? _boundVm;

    public HomeView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => AttachVm(DataContext as MainViewModel);
        Unloaded += (_, _) => AttachVm(null);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        AttachVm(e.NewValue as MainViewModel);
    }

    private void AttachVm(MainViewModel? vm)
    {
        if (ReferenceEquals(_boundVm, vm))
        {
            return;
        }

        if (_boundVm is not null)
        {
            _boundVm.ChooseMusicFileRequested -= OnChooseMusicFileRequested;
        }

        _boundVm = vm;
        if (_boundVm is not null)
        {
            _boundVm.ChooseMusicFileRequested += OnChooseMusicFileRequested;
        }
    }

    private async void OnChooseMusicFileRequested(object? sender, EventArgs e)
    {
        if (_boundVm is null)
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Choose audio file to test across your listening group",
            Filter = "Audio files|*.mp3;*.wav;*.flac;*.m4a;*.wma;*.aac;*.ogg|All files|*.*",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
        {
            await _boundVm.LoadMusicFilesAsync(dialog.FileNames);
        }
    }

    private void OnDialMuteToggled(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ToggleMuteCommand.Execute(null);
        }
    }

    private void OnDialDetentCrossed(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PlayVolumeDetentCue();
        }
    }
}
