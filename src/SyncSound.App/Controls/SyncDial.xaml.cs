using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SyncSound.App.Controls;

public partial class SyncDial : UserControl
{
    private const double StartAngleDeg = -135.0;
    private const double SweepAngleDeg = 270.0;
    private const double DialSize = 148.0;

    private bool _isDragging;
    private Point _lastMousePos;
    private DateTime _lastClickUtc = DateTime.MinValue;

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(SyncDial),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

    public static readonly DependencyProperty IsMutedProperty =
        DependencyProperty.Register(
            nameof(IsMuted),
            typeof(bool),
            typeof(SyncDial),
            new PropertyMetadata(false, OnValueChanged));

    public static readonly RoutedEvent MuteToggledEvent =
        EventManager.RegisterRoutedEvent(
            nameof(MuteToggled),
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(SyncDial));

    public SyncDial()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            BuildTickMarks();
            UpdateVisuals();
        };
    }

    public event RoutedEventHandler MuteToggled
    {
        add => AddHandler(MuteToggledEvent, value);
        remove => RemoveHandler(MuteToggledEvent, value);
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsMuted
    {
        get => (bool)GetValue(IsMutedProperty);
        set => SetValue(IsMutedProperty, value);
    }

    private static object CoerceValue(DependencyObject d, object baseValue) =>
        Math.Clamp((double)baseValue, 0.0, 1.0);

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SyncDial dial)
        {
            dial.UpdateVisuals();
        }
    }

    private void BuildTickMarks()
    {
        TickCanvas.Children.Clear();
        const int tickCount = 24;
        var center = DialSize / 2.0;
        var outerR = DialSize / 2.0 - 2.0;

        for (var i = 0; i <= tickCount; i++)
        {
            var fraction = i / (double)tickCount;
            var angleDeg = StartAngleDeg + fraction * SweepAngleDeg;
            var rad = (angleDeg - 90.0) * Math.PI / 180.0;
            var isMajor = i % 6 == 0;
            var innerR = isMajor ? outerR - 6.0 : outerR - 3.5;

            var line = new Line
            {
                X1 = center + innerR * Math.Cos(rad),
                Y1 = center + innerR * Math.Sin(rad),
                X2 = center + outerR * Math.Cos(rad),
                Y2 = center + outerR * Math.Sin(rad),
                StrokeThickness = isMajor ? 1.4 : 1.0,
                Opacity = isMajor ? 0.55 : 0.30
            };
            line.SetResourceReference(Shape.StrokeProperty, "Brush.Ink.Secondary");
            TickCanvas.Children.Add(line);
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
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
        var delta = (dy + dx * 0.5) / 130.0;

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

    private void OnLostMouseCapture(object sender, MouseEventArgs e) => _isDragging = false;

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var step = e.Delta > 0 ? 0.04 : -0.04;
        Value = Math.Clamp(Value + step, 0.0, 1.0);
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
            case Key.Right:
                Value = Math.Clamp(Value + 0.05, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.Down:
            case Key.Left:
                Value = Math.Clamp(Value - 0.05, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.PageUp:
                Value = Math.Clamp(Value + 0.15, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.PageDown:
                Value = Math.Clamp(Value - 0.15, 0.0, 1.0);
                e.Handled = true;
                break;
            case Key.M:
                RaiseEvent(new RoutedEventArgs(MuteToggledEvent));
                e.Handled = true;
                break;
        }
    }

    private void UpdateVisuals()
    {
        var value = Math.Clamp(Value, 0.0, 1.0);
        var angle = StartAngleDeg + value * SweepAngleDeg;
        IndicatorRotation.Angle = angle;

        var pct = (int)Math.Round(value * 100);
        PercentLabel.Text = IsMuted || pct == 0 ? "MUTED" : $"{pct}%";
        PercentLabel.FontSize = IsMuted || pct == 0 ? 14 : 20;
        StateLabel.Text = IsMuted || pct == 0 ? "TAP TO RESTORE" : "SESSION VOL";

        var radius = DialSize / 2.0 - 12.0;
        var center = new Point(DialSize / 2.0, DialSize / 2.0);
        TrackArc.Data = BuildArcGeometry(center, radius, StartAngleDeg, StartAngleDeg + SweepAngleDeg);

        if (value <= 0.005 || IsMuted)
        {
            ValueArc.Data = Geometry.Empty;
        }
        else
        {
            ValueArc.Data = BuildArcGeometry(center, radius, StartAngleDeg, angle);
        }
    }

    private static Geometry BuildArcGeometry(Point center, double radius, double startAngleDeg, double endAngleDeg)
    {
        if (Math.Abs(endAngleDeg - startAngleDeg) < 0.5)
        {
            return Geometry.Empty;
        }

        var startRad = (startAngleDeg - 90.0) * Math.PI / 180.0;
        var endRad = (endAngleDeg - 90.0) * Math.PI / 180.0;

        var startPoint = new Point(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
        var endPoint = new Point(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));
        var isLargeArc = Math.Abs(endAngleDeg - startAngleDeg) > 180.0;

        var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
        figure.Segments.Add(new ArcSegment
        {
            Point = endPoint,
            Size = new Size(radius, radius),
            SweepDirection = SweepDirection.Clockwise,
            IsLargeArc = isLargeArc
        });

        return new PathGeometry([figure]);
    }
}
