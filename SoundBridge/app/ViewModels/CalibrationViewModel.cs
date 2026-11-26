using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using SoundBridge.Gui.Services;

namespace SoundBridge.Gui.ViewModels;

public sealed class CalibrationViewModel : ViewModelBase
{
    private readonly IAudioRouterService _service;
    private DeviceWorkspaceViewModel? _activeDevice;
    private string _stepTitle = "Step 1 of 3";
    private string _instructions = "Review the active multi-output device, run a quick test, then press OK to start routing.";
    private string _referenceDevice = "Sony WH-1000XM5 (Bluetooth)";
    private string _statusText = "Waiting for calibration";
    private bool _isRoutingStarted;

    public CalibrationViewModel(IAudioRouterService service)
    {
        _service = service;
        DelayTargets = new ObservableCollection<DeviceViewModel>();
        ActiveOutputs = new ObservableCollection<DeviceViewModel>();
        ReferenceDevices = new[]
        {
            "Sony WH-1000XM5 (Bluetooth)",
            "USB Headset Pro",
            "Realtek HD Audio"
        };

        DelayTargets.Add(new DeviceViewModel { Name = "USB Headset Pro", DelayMs = 80 });
        DelayTargets.Add(new DeviceViewModel { Name = "Realtek HD Audio", DelayMs = 80 });
    }

    public event Action<bool>? RoutingStateChanged;

    public ObservableCollection<DeviceViewModel> DelayTargets { get; }

    public ObservableCollection<DeviceViewModel> ActiveOutputs { get; }

    public IReadOnlyList<string> ReferenceDevices { get; }

    public DeviceWorkspaceViewModel? ActiveDevice
    {
        get => _activeDevice;
        private set
        {
            if (SetProperty(ref _activeDevice, value))
            {
                RaisePropertyChanged(nameof(HasActiveDevice));
                RaisePropertyChanged(nameof(ActiveDeviceName));
                RaisePropertyChanged(nameof(ActiveDeviceSampleRate));
                RaisePropertyChanged(nameof(ActiveDeviceMaster));
            }
        }
    }

    public bool HasActiveDevice => ActiveDevice is not null;

    public string ActiveDeviceName => ActiveDevice?.Name ?? "No active multi-output device";

    public string ActiveDeviceSampleRate => ActiveDevice is null ? "--" : $"{ActiveDevice.SampleRateKhz} kHz";

    public string ActiveDeviceMaster => ActiveDevice?.SelectedSource?.FriendlyName ?? "--";

    public string StepTitle
    {
        get => _stepTitle;
        private set => SetProperty(ref _stepTitle, value);
    }

    public string Instructions
    {
        get => _instructions;
        private set => SetProperty(ref _instructions, value);
    }

    public string ReferenceDevice
    {
        get => _referenceDevice;
        set => SetProperty(ref _referenceDevice, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsRoutingStarted
    {
        get => _isRoutingStarted;
        private set
        {
            if (SetProperty(ref _isRoutingStarted, value))
            {
                RaisePropertyChanged(nameof(RoutingToggleButtonText));
            }
        }
    }

    public string RoutingToggleButtonText => IsRoutingStarted ? "Disable" : "OK";

    public RelayCommand PlayTestCommand => new(() =>
    {
        if (IsRoutingStarted)
        {
            try
            {
                _service.PlayCalibrationBeeps();
                StatusText = "Native calibration beeps played through active outputs";
            }
            catch (InvalidOperationException ex)
            {
                StatusText = ex.Message;
            }

            return;
        }

        var beepPath = Path.Combine(AppContext.BaseDirectory, "asset", "beep.wav");
        if (!File.Exists(beepPath))
        {
            StatusText = "Start routing to play native calibration beeps. Fallback beep.wav is missing.";
            return;
        }

        try
        {
            using var player = new SoundPlayer(beepPath);
            player.Play();
            StatusText = "Play Test sent once";
        }
        catch (Exception ex)
        {
            StatusText = $"Play Test failed: {ex.Message}";
        }
    });

    public RelayCommand StartRoutingCommand => new(() =>
    {
        if (ActiveDevice is null)
        {
            StatusText = "Select an active multi-output device first.";
            return;
        }

        if (IsRoutingStarted)
        {
            ActiveDevice.StopRouting();
            IsRoutingStarted = false;
            StepTitle = "Ready";
            StatusText = "Audio routing disabled";
            RoutingStateChanged?.Invoke(false);
            return;
        }

        if (ActiveDevice.TryStartRouting(out var message))
        {
            StepTitle = "Routing active";
            IsRoutingStarted = true;
            StatusText = message;
            RoutingStateChanged?.Invoke(true);
            return;
        }

        StatusText = message;
    });

    public RelayCommand UpdateRoutingCommand => new(() =>
    {
        if (!IsRoutingStarted || ActiveDevice is null)
        {
            StatusText = "Start routing before updating routing metadata.";
            return;
        }

        if (ActiveDevice.TryUpdateRouting(out var message))
        {
            RefreshActiveOutputs();
            RoutingStateChanged?.Invoke(true);
        }

        StatusText = message;
    });

    public void SetActiveDevice(DeviceWorkspaceViewModel activeDevice)
    {
        ActiveDevice = activeDevice;
        RefreshActiveOutputs();

        ReferenceDevice = ActiveDeviceMaster == "--" ? ReferenceDevice : ActiveDeviceMaster;
        IsRoutingStarted = activeDevice.IsRoutingActive;
        RoutingStateChanged?.Invoke(IsRoutingStarted);
        StepTitle = "Ready";
        StatusText = $"{activeDevice.Name} ready for calibration";
    }

    public void MarkRoutingUpdatePending()
    {
        RefreshActiveOutputs();
        StatusText = "Routing changes detected. Review and click Update Routing.";
    }

    public void ClearActiveDevice()
    {
        ActiveDevice = null;
        ActiveOutputs.Clear();
        IsRoutingStarted = false;
        RoutingStateChanged?.Invoke(false);
        StepTitle = "No active template";
        StatusText = "Select a multi-output device to enable calibration";
    }

    private void RefreshActiveOutputs()
    {
        ActiveOutputs.Clear();

        if (ActiveDevice is null)
        {
            return;
        }

        foreach (var output in ActiveDevice.Outputs.Where(output => output.IsEnabled))
        {
            ActiveOutputs.Add(output);
        }
    }
}
