using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SyncSound.App.Services;

namespace SyncSound.App.Controls;

public partial class SyncOrb : UserControl
{
    private Storyboard? _breatheStoryboard;
    private Storyboard? _listeningStoryboard;

    public static readonly DependencyProperty VisualStateProperty =
        DependencyProperty.Register(
            nameof(VisualState),
            typeof(SyncVisualState),
            typeof(SyncOrb),
            new PropertyMetadata(SyncVisualState.Ready, OnVisualStateChanged));

    public SyncOrb()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateStateAnimations();
        Unloaded += (_, _) => StopAllAnimations();
    }

    public SyncVisualState VisualState
    {
        get => (SyncVisualState)GetValue(VisualStateProperty);
        set => SetValue(VisualStateProperty, value);
    }

    private static void OnVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SyncOrb orb && orb.IsLoaded)
        {
            orb.UpdateStateAnimations();
        }
    }

    private void UpdateStateAnimations()
    {
        StopAllAnimations();

        if (!SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        // BREATHE: calm, restrained atmospheric presence in Ready/Listening states
        if (VisualState is SyncVisualState.Ready or SyncVisualState.Listening or SyncVisualState.OneDeviceConnected)
        {
            var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
            var sx = new DoubleAnimation(0.96, 1.04, TimeSpan.FromMilliseconds(2800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = ease
            };
            var sy = new DoubleAnimation(0.96, 1.04, TimeSpan.FromMilliseconds(2800))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = ease
            };

            _breatheStoryboard = new Storyboard();
            Storyboard.SetTarget(sx, BreatheHalo);
            Storyboard.SetTargetProperty(sx, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(sy, BreatheHalo);
            Storyboard.SetTargetProperty(sy, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            _breatheStoryboard.Children.Add(sx);
            _breatheStoryboard.Children.Add(sy);
            _breatheStoryboard.Begin();
        }

        // SIGNAL + MORPH: active acoustic pulse and reactive equalizer only while genuinely Listening
        if (VisualState == SyncVisualState.Listening)
        {
            _listeningStoryboard = new Storyboard();

            var pulseX = new DoubleAnimation(0.82, 1.32, TimeSpan.FromMilliseconds(1800)) { RepeatBehavior = RepeatBehavior.Forever };
            var pulseY = new DoubleAnimation(0.82, 1.32, TimeSpan.FromMilliseconds(1800)) { RepeatBehavior = RepeatBehavior.Forever };
            var pulseOpacity = new DoubleAnimation(0.72, 0.0, TimeSpan.FromMilliseconds(1800)) { RepeatBehavior = RepeatBehavior.Forever };

            Storyboard.SetTarget(pulseX, SignalRing);
            Storyboard.SetTargetProperty(pulseX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(pulseY, SignalRing);
            Storyboard.SetTargetProperty(pulseY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTarget(pulseOpacity, SignalRing);
            Storyboard.SetTargetProperty(pulseOpacity, new PropertyPath("Opacity"));

            _listeningStoryboard.Children.Add(pulseX);
            _listeningStoryboard.Children.Add(pulseY);
            _listeningStoryboard.Children.Add(pulseOpacity);

            AddBarAnimation(_listeningStoryboard, Bar1, 0.65, 1.45, 420);
            AddBarAnimation(_listeningStoryboard, Bar2, 1.25, 0.50, 340);
            AddBarAnimation(_listeningStoryboard, Bar3, 0.55, 1.20, 290);
            AddBarAnimation(_listeningStoryboard, Bar4, 1.25, 0.50, 360);
            AddBarAnimation(_listeningStoryboard, Bar5, 0.65, 1.40, 440);

            _listeningStoryboard.Begin();
        }
        else if (VisualState == SyncVisualState.Recovering)
        {
            _listeningStoryboard = new Storyboard();
            var pulseOpacity = new DoubleAnimation(0.65, 0.15, TimeSpan.FromMilliseconds(900))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(pulseOpacity, SignalRing);
            Storyboard.SetTargetProperty(pulseOpacity, new PropertyPath("Opacity"));
            _listeningStoryboard.Children.Add(pulseOpacity);
            _listeningStoryboard.Begin();
        }
    }

    private static void AddBarAnimation(Storyboard sb, Border bar, double from, double to, int durationMs)
    {
        var anim = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(durationMs))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        Storyboard.SetTarget(anim, bar);
        Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
        sb.Children.Add(anim);
    }

    private void StopAllAnimations()
    {
        _breatheStoryboard?.Stop();
        _breatheStoryboard = null;
        _listeningStoryboard?.Stop();
        _listeningStoryboard = null;
        SignalRing.Opacity = 0;
    }
}
