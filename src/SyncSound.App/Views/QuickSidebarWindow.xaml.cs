using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SyncSound.App.Services;
using SyncSound.App.ViewModels;

namespace SyncSound.App.Views;

public partial class QuickSidebarWindow : Window
{
    private bool _isClosingAnimated;

    public QuickSidebarWindow(QuickSidebarViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Topmost = viewModel.AlwaysOnTop;
        SourceInitialized += (_, _) => DwmWindowHelper.ApplyRoundedCorners(this);
    }

    public void ShowAnimated()
    {
        _isClosingAnimated = false;
        var area = SystemParameters.WorkArea;
        Height = Math.Clamp(600.0, 440.0, Math.Max(440.0, area.Height - 24));
        Width = Math.Clamp(386.0, 340.0, Math.Max(340.0, area.Width - 24));

        if (DataContext is QuickSidebarViewModel vm)
        {
            Topmost = vm.AlwaysOnTop;
            var s = vm.SettingsService.Current;
            if (vm.RememberSidebarPosition && s.SidebarLeft.HasValue && s.SidebarTop.HasValue)
            {
                Left = Math.Clamp(s.SidebarLeft.Value, area.Left, Math.Max(area.Left, area.Right - Width));
                Top = Math.Clamp(s.SidebarTop.Value, area.Top, Math.Max(area.Top, area.Bottom - Height));
            }
            else
            {
                Left = area.Right - Width - 12;
                Top = area.Top + Math.Max(12, (area.Height - Height) / 2);
            }
        }
        else
        {
            Left = area.Right - Width - 12;
            Top = area.Top + Math.Max(12, (area.Height - Height) / 2);
        }

        var reduceMotion = DataContext is QuickSidebarViewModel qvm && qvm.ReducedMotion;
        if (reduceMotion || !SystemParameters.ClientAreaAnimation)
        {
            BeginAnimation(OpacityProperty, null);
            SlideTransform.BeginAnimation(TranslateTransform.XProperty, null);
            Opacity = 1;
            SlideTransform.X = 0;
            Show();
            Activate();
            return;
        }

        // Interruptible entry from current presentation state
        if (!IsVisible)
        {
            Opacity = 0;
            SlideTransform.X = 22;
            Show();
        }

        Activate();
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease });
        SlideTransform.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(0, TimeSpan.FromMilliseconds(250)) { EasingFunction = ease });
    }

    public void HideAnimated()
    {
        if (!IsVisible || _isClosingAnimated)
        {
            return;
        }

        var reduceMotion = DataContext is QuickSidebarViewModel qvm && qvm.ReducedMotion;
        if (reduceMotion || !SystemParameters.ClientAreaAnimation)
        {
            Hide();
            return;
        }

        _isClosingAnimated = true;
        var ease = new CubicEase { EasingMode = EasingMode.EaseIn };
        var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease };
        fadeOut.Completed += (_, _) =>
        {
            if (_isClosingAnimated)
            {
                _isClosingAnimated = false;
                Hide();
            }
        };

        BeginAnimation(OpacityProperty, fadeOut);
        SlideTransform.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(20, TimeSpan.FromMilliseconds(180)) { EasingFunction = ease });
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        DragMove();
        if (DataContext is QuickSidebarViewModel vm && vm.RememberSidebarPosition)
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
