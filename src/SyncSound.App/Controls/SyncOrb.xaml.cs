using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SyncSound.App.Services;

namespace SyncSound.App.Controls;

public partial class SyncOrb : UserControl
{
    private Storyboard? _breatheStoryboard;
    private Storyboard? _stateStoryboard;
    private DispatcherTimer? _reactiveTimer;
    private double _smoothedLevel;
    private double _phase;

    public static readonly DependencyProperty VisualStateProperty =
        DependencyProperty.Register(
            nameof(VisualState),
            typeof(SyncVisualState),
            typeof(SyncOrb),
            new PropertyMetadata(SyncVisualState.Ready, OnOrbPropertyChanged));

    public static readonly DependencyProperty AudioLevelProperty =
        DependencyProperty.Register(
            nameof(AudioLevel),
            typeof(double),
            typeof(SyncOrb),
            new PropertyMetadata(0.0));

    public static readonly DependencyProperty SelectedNodeCountProperty =
        DependencyProperty.Register(
            nameof(SelectedNodeCount),
            typeof(int),
            typeof(SyncOrb),
            new PropertyMetadata(0, OnNodesChanged));

    public static readonly DependencyProperty ReducedMotionProperty =
        DependencyProperty.Register(
            nameof(ReducedMotion),
            typeof(bool),
            typeof(SyncOrb),
            new PropertyMetadata(false, OnOrbPropertyChanged));

    public SyncOrb()
    {
        InitializeComponent();
        RenderConnectedNodes();
        UpdateStateVisuals();
        Loaded += (_, _) =>
        {
            RenderConnectedNodes();
            UpdateStateVisuals();
        };
        Unloaded += (_, _) => StopAllAnimations();
    }

    public SyncVisualState VisualState
    {
        get => (SyncVisualState)GetValue(VisualStateProperty);
        set => SetValue(VisualStateProperty, value);
    }

    public double AudioLevel
    {
        get => (double)GetValue(AudioLevelProperty);
        set => SetValue(AudioLevelProperty, value);
    }

    public int SelectedNodeCount
    {
        get => (int)GetValue(SelectedNodeCountProperty);
        set => SetValue(SelectedNodeCountProperty, value);
    }

    public bool ReducedMotion
    {
        get => (bool)GetValue(ReducedMotionProperty);
        set => SetValue(ReducedMotionProperty, value);
    }

    private static void OnOrbPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SyncOrb orb)
        {
            orb.RenderConnectedNodes();
            orb.UpdateStateVisuals();
        }
    }

    private static void OnNodesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SyncOrb orb)
        {
            orb.RenderConnectedNodes();
        }
    }

    private void RenderConnectedNodes()
    {
        NodesCanvas.Children.Clear();
        var count = Math.Clamp(SelectedNodeCount, 0, 8);
        if (count == 0)
        {
            return;
        }

        const double center = 88.0;
        const double radius = 72.0;

        for (var i = 0; i < count; i++)
        {
            var angleRad = (-90.0 + i * (360.0 / count)) * Math.PI / 180.0;
            var x = center + radius * Math.Cos(angleRad);
            var y = center + radius * Math.Sin(angleRad);

            var halo = new Ellipse
            {
                Width = 14,
                Height = 14,
                Opacity = VisualState == SyncVisualState.Listening ? 0.35 : 0.16
            };
            halo.SetResourceReference(Shape.FillProperty, VisualState == SyncVisualState.Recovering ? "Brush.Status.Warning" : "Brush.Accent.Primary");
            Canvas.SetLeft(halo, x - 7);
            Canvas.SetTop(halo, y - 7);
            NodesCanvas.Children.Add(halo);

            var dot = new Ellipse
            {
                Width = 7.5,
                Height = 7.5,
                Stroke = Brushes.White,
                StrokeThickness = 1.2
            };
            var dotBrushKey = VisualState switch
            {
                SyncVisualState.Listening => "Brush.Accent.Primary",
                SyncVisualState.Recovering => "Brush.Status.Warning",
                SyncVisualState.Muted => "Brush.Ink.Muted",
                _ => "Brush.Status.Success"
            };
            dot.SetResourceReference(Shape.FillProperty, dotBrushKey);
            Canvas.SetLeft(dot, x - 3.75);
            Canvas.SetTop(dot, y - 3.75);
            NodesCanvas.Children.Add(dot);
        }
    }

    private void UpdateStateVisuals()
    {
        StopAllAnimations();

        // Update calm ring & bar baseline immediately (state-accurate even with Reduced Motion)
        var isMuted = VisualState == SyncVisualState.Muted;
        var isIdleLow = VisualState is SyncVisualState.NoAdditionalHeadphones or SyncVisualState.Disconnected;
        BreatheHalo.Opacity = isIdleLow ? 0.35 : (isMuted ? 0.25 : 0.72);
        ParticlesCanvas.Opacity = VisualState == SyncVisualState.Listening ? 0.85 : 0.0;
        ResetBars(isMuted ? 0.38 : (isIdleLow ? 0.65 : 1.0));

        if (ReducedMotion || !SystemParameters.ClientAreaAnimation)
        {
            return;
        }

        // BREATHE: gentle 5.5s atmospheric cadence in active / ready states
        if (VisualState is SyncVisualState.Ready or SyncVisualState.Listening or SyncVisualState.OneDeviceConnected)
        {
            var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
            var sx = new DoubleAnimation(0.965, 1.035, TimeSpan.FromMilliseconds(2750))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = ease
            };
            var sy = new DoubleAnimation(0.965, 1.035, TimeSpan.FromMilliseconds(2750))
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

        if (VisualState == SyncVisualState.Listening)
        {
            _stateStoryboard = new Storyboard();

            var pulseX = new DoubleAnimation(0.80, 1.30, TimeSpan.FromMilliseconds(1850)) { RepeatBehavior = RepeatBehavior.Forever };
            var pulseY = new DoubleAnimation(0.80, 1.30, TimeSpan.FromMilliseconds(1850)) { RepeatBehavior = RepeatBehavior.Forever };
            var pulseOpacity = new DoubleAnimation(0.68, 0.0, TimeSpan.FromMilliseconds(1850)) { RepeatBehavior = RepeatBehavior.Forever };

            Storyboard.SetTarget(pulseX, SignalRing);
            Storyboard.SetTargetProperty(pulseX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(pulseY, SignalRing);
            Storyboard.SetTargetProperty(pulseY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTarget(pulseOpacity, SignalRing);
            Storyboard.SetTargetProperty(pulseOpacity, new PropertyPath("Opacity"));

            var orbitRot = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(18)) { RepeatBehavior = RepeatBehavior.Forever };
            Storyboard.SetTarget(orbitRot, OrbitRing);
            Storyboard.SetTargetProperty(orbitRot, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            var particleRot = new DoubleAnimation(0, -360, TimeSpan.FromSeconds(24)) { RepeatBehavior = RepeatBehavior.Forever };
            Storyboard.SetTarget(particleRot, ParticlesCanvas);
            Storyboard.SetTargetProperty(particleRot, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));

            _stateStoryboard.Children.Add(pulseX);
            _stateStoryboard.Children.Add(pulseY);
            _stateStoryboard.Children.Add(pulseOpacity);
            _stateStoryboard.Children.Add(orbitRot);
            _stateStoryboard.Children.Add(particleRot);
            _stateStoryboard.Begin();

            _reactiveTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _reactiveTimer.Tick += OnReactiveTick;
            _reactiveTimer.Start();
        }
        else if (VisualState == SyncVisualState.Recovering)
        {
            _stateStoryboard = new Storyboard();
            var pulseOpacity = new DoubleAnimation(0.68, 0.16, TimeSpan.FromMilliseconds(850))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(pulseOpacity, SignalRing);
            Storyboard.SetTargetProperty(pulseOpacity, new PropertyPath("Opacity"));
            _stateStoryboard.Children.Add(pulseOpacity);
            _stateStoryboard.Begin();
        }
    }

    private void OnReactiveTick(object? sender, EventArgs e)
    {
        var raw = Math.Clamp(AudioLevel, 0.0, 1.0);
        _smoothedLevel = raw > _smoothedLevel
            ? _smoothedLevel + (raw - _smoothedLevel) * 0.42
            : _smoothedLevel * 0.85;

        // Subtle baseline alive glow even during quiet dialogue passages, surging cleanly with real audio energy
        var energy = 0.08 + _smoothedLevel * 0.92;
        _phase += 0.22;

        Bar1Scale.ScaleY = 0.45 + energy * (0.45 + 0.35 * Math.Sin(_phase * 1.1));
        Bar2Scale.ScaleY = 0.55 + energy * (0.50 + 0.35 * Math.Sin(_phase * 1.45 + 0.9));
        Bar3Scale.ScaleY = 0.62 + energy * (0.52 + 0.38 * Math.Sin(_phase * 1.85 + 1.8));
        Bar4Scale.ScaleY = 0.55 + energy * (0.50 + 0.35 * Math.Sin(_phase * 1.35 + 2.7));
        Bar5Scale.ScaleY = 0.45 + energy * (0.45 + 0.35 * Math.Sin(_phase * 1.05 + 3.5));

        var coreBoost = 1.0 + _smoothedLevel * 0.032;
        CoreScale.ScaleX = coreBoost;
        CoreScale.ScaleY = coreBoost;
    }

    private void ResetBars(double scale)
    {
        Bar1Scale.ScaleY = scale;
        Bar2Scale.ScaleY = scale;
        Bar3Scale.ScaleY = scale;
        Bar4Scale.ScaleY = scale;
        Bar5Scale.ScaleY = scale;
        CoreScale.ScaleX = 1.0;
        CoreScale.ScaleY = 1.0;
    }

    private void StopAllAnimations()
    {
        _reactiveTimer?.Stop();
        _reactiveTimer = null;
        _breatheStoryboard?.Stop();
        _breatheStoryboard = null;
        _stateStoryboard?.Stop();
        _stateStoryboard = null;
        SignalRing.Opacity = 0;
        OrbitRotation.Angle = 0;
        ParticleRotation.Angle = 0;
    }
}
