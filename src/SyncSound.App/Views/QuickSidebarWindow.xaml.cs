using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using SyncSound.App.ViewModels;

namespace SyncSound.App.Views;

public partial class QuickSidebarWindow : Window
{
    public QuickSidebarWindow(QuickSidebarViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Topmost = viewModel.AlwaysOnTop;
    }

    public void ShowAnimated()
    {
        if (DataContext is QuickSidebarViewModel vm)
        {
            Topmost = vm.AlwaysOnTop;
        }

        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 8;
        Top = area.Top + Math.Max(16, (area.Height - Height) / 2);

        if (!SystemParameters.ClientAreaAnimation)
        {
            Opacity = 1;
            SlideTransform.X = 0;
            Show();
            Activate();
            return;
        }

        Opacity = 0;
        SlideTransform.X = 28;
        Show();
        Activate();

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
        SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty,
            new DoubleAnimation(28, 0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease });
    }

    public void HideAnimated()
    {
        if (!IsVisible)
        {
            return;
        }

        Hide();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            HideAnimated();
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (DataContext is QuickSidebarViewModel vm && vm.ClickOutsideToClose && IsVisible)
        {
            HideAnimated();
        }
    }
}
