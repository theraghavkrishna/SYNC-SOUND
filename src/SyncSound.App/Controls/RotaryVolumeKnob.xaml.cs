using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SyncSound.App.Controls;

public partial class RotaryVolumeKnob : UserControl
{
    private const double StartAngleDeg = -135.0;
    private const double SweepAngleDeg = 270.0;

    private bool _isDragging;
    private Point _lastMousePos;
    private DateTime _lastClickUtc = DateTime.MinValue;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(RotaryVolumeKnob),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty DialSizeProperty =
        DependencyProperty.Register(
            nameof(DialSize),
            typeof(double),
            typeof(RotaryVolumeKnob),
            new PropertyMetadata(44.0, OnDialSizeChanged));

    public RotaryVolumeKnob()
    {
        InitializeComponent();
        Loaded += (_, _) => UpdateVisuals();
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double DialSize
    {
        get => (double)GetValue(DialSizeProperty);
        set => SetValue(DialSizeProperty, value);
    }

    private static object CoerceValue(DependencyObject d, object baseValue)
    {
        var val = (double)baseValue;
        return Math.Clamp(val, 0.0, 1.0);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RotaryVolumeKnob knob)
        {
            knob.UpdateVisuals();
        }
    }

    private static void OnDialSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RotaryVolumeKnob knob)
        {
            var size = Math.Max(28.0, (double)e.NewValue);
            knob.DialHost.Width = size;
            knob.DialHost.Height = size;
            knob.UpdateVisuals();
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastClickUtc).TotalMilliseconds < 280)
        {
            Value = 1.0;
            _lastClickUtc = DateTime.MinValue;
            e.Handled = true;
            return;
        }

        _lastClickUtc = now;
        _isDragging = true;
        _lastMousePos = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var current = e.GetPosition(this);
        var dy = _lastMousePos.Y - current.Y;
        var dx = current.X - _lastMousePos.X;
        var delta = (dy + dx * 0.5) / 110.0;

        if (Math.Abs(delta) > 0.0005)
        {
            Value = Math.Clamp(Value + delta, 0.0, 1.0);
            _lastMousePos = current;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void OnLostMouseCapture(object sender, MouseEventArgs e)
    {
        _isDragging = false;
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var step = e.Delta > 0 ? 0.05 : -0.05;
        Value = Math.Clamp(Value + step, 0.0, 1.0);
        e.Handled = true;
    }

    private void UpdateVisuals()
    {
        var v = Math.Clamp(Value, 0.0, 1.0);
        var angle = StartAngleDeg + v * SweepAngleDeg;
        IndicatorRotation.Angle = angle;

        PercentLabel.Text = $"{(int)Math.Round(v * 100)}%";

        var size = DialHost.Width > 0 ? DialHost.Width : 44.0;
        var center = new Point(size / 2.0, size / 2.0);
        var radius = (size / 2.0) - 3.5;

        TrackArc.Data = CreateArcGeometry(center, radius, StartAngleDeg, StartAngleDeg + SweepAngleDeg);

        if (v <= 0.005)
        {
            ValueArc.Data = null;
        }
        else
        {
            ValueArc.Data = CreateArcGeometry(center, radius, StartAngleDeg, angle);
        }
    }

    private static Geometry CreateArcGeometry(Point center, double radius, double startAngleDeg, double endAngleDeg)
    {
        var startRad = (startAngleDeg - 90.0) * Math.PI / 180.0;
        var endRad = (endAngleDeg - 90.0) * Math.PI / 180.0;

        var startPoint = new Point(
            center.X + radius * Math.Cos(startRad),
            center.Y + radius * Math.Sin(startRad));
        var endPoint = new Point(
            center.X + radius * Math.Cos(endRad),
            center.Y + radius * Math.Sin(endRad));

        var isLargeArc = Math.Abs(endAngleDeg - startAngleDeg) > 180.0;

        var segment = new ArcSegment(
            endPoint,
            new Size(radius, radius),
            0,
            isLargeArc,
            SweepDirection.Clockwise,
            isStroked: true);

        var figure = new PathFigure(startPoint, [segment], closed: false);
        var geometry = new PathGeometry([figure]);
        geometry.Freeze();
        return geometry;
    }
}
