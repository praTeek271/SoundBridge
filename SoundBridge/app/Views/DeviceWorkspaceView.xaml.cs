using System.Windows.Controls;
using SoundBridge.Gui.ViewModels;

namespace SoundBridge.Gui.Views;

public partial class DeviceWorkspaceView : UserControl
{
    public DeviceWorkspaceView()
    {
        InitializeComponent();
    }

    private void DelayAdjustButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not DeviceViewModel device)
        {
            return;
        }

        if (!int.TryParse(button.Tag?.ToString(), out var delta))
        {
            return;
        }

        device.DelayMs = Math.Clamp(device.DelayMs + delta, 0, 500);
    }
}
