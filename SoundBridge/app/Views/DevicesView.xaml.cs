using System.Windows.Controls;
using System.Windows.Input;
using SoundBridge.Gui.ViewModels;

namespace SoundBridge.Gui.Views;

public partial class DevicesView : UserControl
{
    public DevicesView()
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

    private void RoutingConsoleHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is DevicesViewModel viewModel
            && viewModel.ToggleConsoleCommand.CanExecute(null))
        {
            viewModel.ToggleConsoleCommand.Execute(null);
        }
    }
}
