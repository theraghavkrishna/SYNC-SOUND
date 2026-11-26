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
        var area = SystemParameters.WorkArea;
        if (DataContext is QuickSidebarViewModel vm)
        {
            Topmost = vm.AlwaysOnTop;
            var s = vm.SettingsService.Current;
            if (s.SidebarLeft.HasValue && s.SidebarTop.HasValue)
            {
                Left = Math.Clamp(s.SidebarLeft.Value, area.Left, Math.Max(area.Left, area.Right - Width));
                Top = Math.Clamp(s.SidebarTop.Value, area.Top, Math.Max(area.Top, area.Bottom - Height));
            }
            else
            {
                Left = area.Right - Width - 12;
                Top = area.Top + Math.Max(16, (area.Height - Height) / 2);
            }
        }
        else
        {
            Left = area.Right - Width - 12;
            Top = area.Top + Math.Max(16, (area.Height - Height) / 2);
        }

        if (!SystemParameters.ClientAreaAnimation)
        {
            Opacity = 1;
            SlideTransform.X = 0;
            Show();
            Activate();
            return;
        }

        Opacity = 0;
        SlideTransform.X = 24;
        Show();
        Activate();

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
        SlideTransform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty,
            new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(240)) { EasingFunction = ease });
    }

    public void HideAnimated()
    {
        if (!IsVisible)
        {
            return;
        }

        Hide();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        DragMove();
        if (DataContext is QuickSidebarViewModel vm)
        {
            vm.SettingsService.Current.SidebarLeft = Left;
            vm.SettingsService.Current.SidebarTop = Top;
            _ = vm.SettingsService.SaveAsync();
        }
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
