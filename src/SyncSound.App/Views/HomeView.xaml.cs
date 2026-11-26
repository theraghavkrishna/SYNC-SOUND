using System.Windows;
using System.Windows.Controls;
using SyncSound.App.ViewModels;

namespace SyncSound.App.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    private void OnDialMuteToggled(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.ToggleMuteCommand.CanExecute(null))
        {
            vm.ToggleMuteCommand.Execute(null);
        }
    }
}
