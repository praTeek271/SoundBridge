using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using SoundBridge.Gui.ViewModels;
using WinForms = System.Windows.Forms;

namespace SoundBridge.Gui;

public partial class MainWindow : Window
{
    private WinForms.NotifyIcon? _trayIcon;
    private bool _allowExit;
    private bool _isSidebarCollapsed;

    public MainWindow()
    {
        InitializeComponent();
        UpdateWindowChromeState();
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
        CreateTrayIcon();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximized();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximized();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_allowExit)
        {
            return;
        }

        if (DataContext is MainViewModel viewModel && viewModel.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        if (DataContext is MainViewModel { RouterService.IsRoutingActive: true } activeViewModel)
        {
            var result = MessageBox.Show(
                "Audio routing is active. Stop routing and exit SoundBridge?",
                "SoundBridge",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            activeViewModel.RouterService.StopRouting();
            activeViewModel.Devices.SetRoutingActive(false);
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private void ToggleSidebarButton_Click(object sender, RoutedEventArgs e)
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        SidebarColumn.Width = new GridLength(_isSidebarCollapsed ? 56 : 232);
        SidebarToggleButton.Content = "\uE700";
        SidebarToggleButton.ToolTip = _isSidebarCollapsed ? "Expand navigation" : "Collapse navigation";

        if (DataContext is ViewModels.MainViewModel viewModel)
        {
            viewModel.IsSidebarCollapsed = _isSidebarCollapsed;
            viewModel.IsDeviceCreateMenuOpen = false;
        }
    }

    private void ToggleMaximized()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        UpdateWindowChromeState();
    }

    private void UpdateWindowChromeState()
    {
        if (WindowState == WindowState.Maximized)
        {
            DialogRoot.CornerRadius = new CornerRadius(0);
            MaximizeRestoreButton.Content = "\uE923";
        }
        else
        {
            DialogRoot.CornerRadius = new CornerRadius(0);
            MaximizeRestoreButton.Content = "\uE922";
        }
    }

    private void CreateTrayIcon()
    {
        var processPath = Environment.ProcessPath;
        var trayIcon = string.IsNullOrWhiteSpace(processPath)
            ? System.Drawing.SystemIcons.Application
            : System.Drawing.Icon.ExtractAssociatedIcon(processPath) ?? System.Drawing.SystemIcons.Application;

        _trayIcon = new WinForms.NotifyIcon
        {
            Text = "SoundBridge",
            Icon = trayIcon,
            Visible = true,
            ContextMenuStrip = new WinForms.ContextMenuStrip()
        };

        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        _trayIcon.ContextMenuStrip.Items.Add("Open SoundBridge", null, (_, _) => RestoreFromTray());
        _trayIcon.ContextMenuStrip.Items.Add("Stop Routing", null, (_, _) => StopRoutingFromTray());
        _trayIcon.ContextMenuStrip.Items.Add(new WinForms.ToolStripSeparator());
        _trayIcon.ContextMenuStrip.Items.Add("Exit", null, (_, _) => ExitFromTray());
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _trayIcon?.ShowBalloonTip(
            1200,
            "SoundBridge is still running",
            "Open it again from the tray icon.",
            WinForms.ToolTipIcon.Info);
    }

    private void RestoreFromTray()
    {
        ShowInTaskbar = true;
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void StopRoutingFromTray()
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        viewModel.RouterService.StopRouting();
        viewModel.Devices.SetRoutingActive(false);
    }

    private void ExitFromTray()
    {
        if (DataContext is MainViewModel { RouterService.IsRoutingActive: true })
        {
            var result = MessageBox.Show(
                "Audio routing is active. Stop routing and exit SoundBridge?",
                "SoundBridge",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        StopRoutingFromTray();
        _allowExit = true;
        Close();
    }
}
