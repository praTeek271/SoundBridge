using System.Windows;
using System.Windows.Media;

namespace SoundBridge.Gui.Views.Controls;

public sealed class SmoothMeterControl : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(SmoothMeterControl),
            new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender, OnValueChanged));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(
            nameof(IsActive),
            typeof(bool),
            typeof(SmoothMeterControl),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    private double _displayValue;

    public SmoothMeterControl()
    {
        Loaded += (_, _) => CompositionTarget.Rendering += OnRendering;
        Unloaded += (_, _) => CompositionTarget.Rendering -= OnRendering;
    }

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(120, Math.Max(6, Height > 0 ? Height : 10));
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var radius = ActualHeight / 2;
        var track = new Rect(0, 0, ActualWidth, ActualHeight);
        var trackBrush = TryFindResource("PanelAltBrush") as Brush ?? Brushes.DimGray;
        var fillBrush = IsActive
            ? TryFindResource("AccentBrush") as Brush ?? Brushes.DeepSkyBlue
            : Brushes.Transparent;

        drawingContext.DrawRoundedRectangle(trackBrush, null, track, radius, radius);

        var fillWidth = Math.Max(0, Math.Min(ActualWidth, ActualWidth * _displayValue));
        if (fillWidth > 0.5)
        {
            drawingContext.DrawRoundedRectangle(fillBrush, null, new Rect(0, 0, fillWidth, ActualHeight), radius, radius);
        }
    }

    private static void OnValueChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs _)
    {
        if (dependencyObject is SmoothMeterControl meter && !meter.IsLoaded)
        {
            meter._displayValue = meter.CoerceValue(meter.Value);
        }
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var target = IsActive ? CoerceValue(Value) : 0;
        var delta = target - _displayValue;

        if (Math.Abs(delta) < 0.002)
        {
            _displayValue = target;
        }
        else
        {
            _displayValue += delta * 0.14;
        }

        InvalidateVisual();
    }

    private double CoerceValue(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return Math.Max(0, Math.Min(1, value));
    }
}
