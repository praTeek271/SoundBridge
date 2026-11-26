using System.Windows;
using System.Windows.Controls;
using SoundBridge.Gui.ViewModels;

namespace SoundBridge.Gui.Views.Controls;

public partial class OutputTileControl : UserControl
{
    public static readonly DependencyProperty DeviceProperty =
        DependencyProperty.Register(
            nameof(Device),
            typeof(DeviceViewModel),
            typeof(OutputTileControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty IsRoutingActiveProperty =
        DependencyProperty.Register(
            nameof(IsRoutingActive),
            typeof(bool),
            typeof(OutputTileControl),
            new PropertyMetadata(false));

    public static readonly DependencyProperty LevelPercentProperty =
        DependencyProperty.Register(
            nameof(LevelPercent),
            typeof(double),
            typeof(OutputTileControl),
            new PropertyMetadata(0.0));

    public OutputTileControl()
    {
        InitializeComponent();
    }

    public DeviceViewModel? Device
    {
        get => (DeviceViewModel?)GetValue(DeviceProperty);
        set => SetValue(DeviceProperty, value);
    }

    public bool IsRoutingActive
    {
        get => (bool)GetValue(IsRoutingActiveProperty);
        set => SetValue(IsRoutingActiveProperty, value);
    }

    public double LevelPercent
    {
        get => (double)GetValue(LevelPercentProperty);
        set => SetValue(LevelPercentProperty, value);
    }
}
