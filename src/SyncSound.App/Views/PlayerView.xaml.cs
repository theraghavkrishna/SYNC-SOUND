using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SyncSound.App.ViewModels;

namespace SyncSound.App.Views;

public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
    }

    private void SeekSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.BeginSeek();
        }
    }

    private void SeekSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PlayerViewModel vm)
        {
            vm.CommitSeek();
        }
    }

    private void PlayerView_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private async void PlayerView_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is PlayerViewModel vm &&
            e.Data.GetData(DataFormats.FileDrop) is string[] files &&
            files.Length > 0)
        {
            await vm.HandleDroppedFilesAsync(files);
        }
    }
}
