using System.Collections;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SoundBridge.Gui.ViewModels;

namespace SoundBridge.Gui.Views;

public sealed class DevicesFlowchartControl : FrameworkElement
{
    private const double LogicalWidth = 800;
    private const double MinimumHeight = 400;
    private const double HeightPerNode = 140;
    private const double NodeWidth = 150;
    private const double NodeHeight = 92;
    private const double DisplayNameWidth = 110;
    private const double Tension = 80;

    private readonly Pen _mutedPen = new(new SolidColorBrush(Color.FromRgb(136, 136, 136)), 2.0);
    private readonly Pen _activePen = new(new SolidColorBrush(Color.FromRgb(96, 205, 255)), 2.4);
    private readonly Brush _mutedBrush = new SolidColorBrush(Color.FromRgb(136, 136, 136));
    private readonly Brush _textBrush = Brushes.White;
    private readonly Brush _subtleTextBrush = new SolidColorBrush(Color.FromRgb(170, 170, 170));
    private readonly Brush _dotBrush = new SolidColorBrush(Color.FromRgb(96, 205, 255));
    private readonly List<NodeHitArea> _nodeHitAreas = new();
    private DateTime _lastFrame = DateTime.UtcNow;
    private double _progress;
    private double _lastScale = 1.0;

    public static readonly DependencyProperty SourceNameProperty =
        DependencyProperty.Register(nameof(SourceName), typeof(string), typeof(DevicesFlowchartControl),
            new FrameworkPropertyMetadata("System-Default", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutputsProperty =
        DependencyProperty.Register(nameof(Outputs), typeof(IEnumerable), typeof(DevicesFlowchartControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsConfiguredProperty =
        DependencyProperty.Register(nameof(IsConfigured), typeof(bool), typeof(DevicesFlowchartControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public DevicesFlowchartControl()
    {
        Loaded += (_, _) => CompositionTarget.Rendering += OnRendering;
        Unloaded += (_, _) => CompositionTarget.Rendering -= OnRendering;
        MouseMove += OnMouseMove;
        MouseLeave += (_, _) => ToolTip = null;
    }

    public string SourceName
    {
        get => (string)GetValue(SourceNameProperty);
        set => SetValue(SourceNameProperty, value);
    }

    public IEnumerable? Outputs
    {
        get => (IEnumerable?)GetValue(OutputsProperty);
        set => SetValue(OutputsProperty, value);
    }

    public bool IsConfigured
    {
        get => (bool)GetValue(IsConfiguredProperty);
        set => SetValue(IsConfiguredProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var count = GetActiveOutputs().Count;
        var height = Math.Max(MinimumHeight, Math.Max(1, count) * HeightPerNode);
        var width = double.IsInfinity(availableSize.Width) ? LogicalWidth : availableSize.Width;
        return new Size(width, height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var activeOutputs = GetActiveOutputs();
        var outputs = IsConfigured && activeOutputs.Count > 0
            ? activeOutputs
            : new List<DeviceViewModel> { new() { Name = "Default Audio" } };

        var logicalHeight = Math.Max(MinimumHeight, outputs.Count * HeightPerNode);
        var scale = ActualWidth <= 0 ? 1.0 : ActualWidth / LogicalWidth;
        _lastScale = scale;
        var viewportHeight = Math.Max(ActualHeight, logicalHeight);

        _nodeHitAreas.Clear();
        dc.PushTransform(new ScaleTransform(scale, scale));
        DrawFlowchart(dc, outputs, logicalHeight);
        dc.Pop();

        if (viewportHeight > ActualHeight)
        {
            InvalidateMeasure();
        }
    }

    private void DrawFlowchart(DrawingContext dc, IReadOnlyList<DeviceViewModel> outputs, double totalHeight)
    {
        var centerY = totalHeight / 2;
        var source = new Point(110, centerY);
        var router = new Point(400, centerY);
        var accentPen = IsConfigured ? _activePen : _mutedPen;
        var nodeTextBrush = IsConfigured ? _textBrush : _mutedBrush;
        var mutedTextBrush = IsConfigured ? _subtleTextBrush : _mutedBrush;

        DrawNode(dc, source, "\uE7F5", SourceName, "Master", nodeTextBrush, mutedTextBrush);
        DrawNode(dc, router, "\uE8B8", "SoundBridge", "Router", nodeTextBrush, mutedTextBrush);

        var sourceStart = new Point(source.X + NodeWidth / 2, source.Y);
        var sourceEnd = new Point(router.X - NodeWidth / 2, router.Y);
        DrawConnection(dc, sourceStart, sourceEnd, accentPen);

        for (var i = 0; i < outputs.Count; i++)
        {
            var stepY = totalHeight / (outputs.Count + 1);
            var output = new Point(690, stepY * (i + 1));
            DrawNode(dc, output, "\uE767", outputs[i].Name, "Output", nodeTextBrush, mutedTextBrush);

            var start = new Point(router.X + NodeWidth / 2, router.Y);
            var end = new Point(output.X - NodeWidth / 2, output.Y);
            var geometry = BuildBezier(start, end);
            dc.DrawGeometry(null, accentPen, geometry);

            if (IsConfigured)
            {
                DrawMovingDot(dc, start, end, i);
            }
        }
    }

    private void DrawConnection(DrawingContext dc, Point start, Point end, Pen pen)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.LineTo(end, true, false);
        }

        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    private static StreamGeometry BuildBezier(Point start, Point end)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(start, false, false);
            ctx.BezierTo(
                new Point(start.X + Tension, start.Y),
                new Point(end.X - Tension, end.Y),
                end,
                true,
                false);
        }

        geometry.Freeze();
        return geometry;
    }

    private void DrawNode(DrawingContext dc, Point center, string glyph, string title, string subtitle, Brush titleBrush, Brush subtitleBrush)
    {
        var hitRect = new Rect(center.X - NodeWidth / 2, center.Y - NodeHeight / 2, NodeWidth, NodeHeight);
        _nodeHitAreas.Add(new NodeHitArea(hitRect, title));

        DrawCenteredText(dc, glyph, center.X, center.Y - 39, 65, "Segoe MDL2 Assets", titleBrush, DisplayNameWidth);
        DrawCenteredText(dc, ShortenDeviceName(title), center.X, center.Y + 25, 15, "Segoe UI Variable Text", titleBrush, DisplayNameWidth);
        DrawCenteredText(dc, subtitle, center.X, center.Y + 40, 10, "Segoe UI Variable Text", subtitleBrush, DisplayNameWidth);
    }

    private static string ShortenDeviceName(string name)
    {
        const int maxLength = 14;
        return name.Length <= maxLength ? name : name[..maxLength];
    }

    private void DrawCenteredText(DrawingContext dc, string text, double x, double y, double size, string fontFamily, Brush brush, double maxWidth)
    {
        var formatted = new FormattedText(
            text,
            Thread.CurrentThread.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily(fontFamily), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip)
        {
            MaxTextWidth = maxWidth,
            Trimming = TextTrimming.CharacterEllipsis,
            TextAlignment = TextAlignment.Center
        };

        dc.DrawText(formatted, new Point(x - maxWidth / 2, y - formatted.Height / 2));
    }

    private void DrawMovingDot(DrawingContext dc, Point start, Point end, int index)
    {
        var t = (_progress + index * 0.18) % 1.0;
        var point = CubicBezier(
            start,
            new Point(start.X + Tension, start.Y),
            new Point(end.X - Tension, end.Y),
            end,
            t);

        dc.DrawEllipse(_dotBrush, null, point, 4.5, 4.5);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(110, 96, 205, 255)), 6), point, 9, 9);
    }

    private static Point CubicBezier(Point p0, Point p1, Point p2, Point p3, double t)
    {
        var u = 1.0 - t;
        var tt = t * t;
        var uu = u * u;
        var uuu = uu * u;
        var ttt = tt * t;

        return new Point(
            uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X,
            uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y);
    }

    private List<DeviceViewModel> GetActiveOutputs()
    {
        if (Outputs is null)
        {
            return new List<DeviceViewModel>();
        }

        return Outputs
            .OfType<DeviceViewModel>()
            .Where(device => device.IsEnabled)
            .ToList();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (!IsConfigured)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var elapsed = (now - _lastFrame).TotalSeconds;
        _lastFrame = now;
        _progress = (_progress + elapsed * 0.32) % 1.0;
        InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        var logical = new Point(position.X / _lastScale, position.Y / _lastScale);
        var hit = _nodeHitAreas.FirstOrDefault(area => area.Bounds.Contains(logical));
        ToolTip = hit?.FullName;
    }

    private sealed record NodeHitArea(Rect Bounds, string FullName);
}
