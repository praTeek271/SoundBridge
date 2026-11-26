using System.Collections.ObjectModel;
using System.Windows.Threading;
using SoundBridge.Gui.Models;
using SoundBridge.Gui.Services;

namespace SoundBridge.Gui.ViewModels;

public sealed class DevicesViewModel : ViewModelBase
{
    private readonly IAudioRouterService _service;
    private readonly ObservableCollection<DeviceViewModel> _overviewOutputs = new();
    private readonly ObservableCollection<DeviceViewModel> _liveOutputTiles = new();
    private readonly DispatcherTimer _liveStatusTimer;
    private AudioDeviceDescriptor? _selectedSource;
    private string? _selectedSourceId;
    private string _scanStatus = "Ready";
    private string _routingStatusText = "Routing disabled";
    private bool _showSystemDefaultAlias;
    private bool _isConfigured;
    private DeviceWorkspaceViewModel? _activeTemplate;
    private bool _isRoutingActive;
    private bool _isConsoleExpanded;
    private int _activeOutputCount;
    private int _totalOutputCount;
    private double _levelPercent;
    private string _windowsDefaultDeviceName = "Detecting...";
    private int _refreshTickCounter;

    public event Action? OpenCalibrationRequested;

    public DevicesViewModel(IAudioRouterService service)
    {
        _service = service;
        SourceDevices = new ObservableCollection<AudioDeviceDescriptor>();
        Devices = new ObservableCollection<DeviceViewModel>();
        Scan();

        _liveStatusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _liveStatusTimer.Tick += (_, _) => RefreshLiveStatus();
        _liveStatusTimer.Start();
    }

    public ObservableCollection<DeviceViewModel> Devices { get; }

    public ObservableCollection<AudioDeviceDescriptor> SourceDevices { get; }

    public ObservableCollection<DeviceViewModel> OverviewOutputs => _overviewOutputs;

    public ObservableCollection<DeviceViewModel> LiveOutputTiles => _liveOutputTiles;

    public string PageTitle => "Devices";

    public string MasterDeviceDisplay => ActiveTemplate?.SelectedSourceDisplayName ?? "Virtual Device (default)";

    public string WindowsDefaultDeviceName
    {
        get => _windowsDefaultDeviceName;
        private set => SetProperty(ref _windowsDefaultDeviceName, value);
    }

    public string SampleRateDisplay => ActiveTemplate is null ? "44.1" : ActiveTemplate.SampleRateKhz;

    public bool HasActiveTemplate => ActiveTemplate is not null;

    public string RoutingStatusText
    {
        get => _routingStatusText;
        private set => SetProperty(ref _routingStatusText, value);
    }

    public string SourceStatusText => ActiveTemplate?.SelectedSourceDisplayName ?? "Virtual Device (default)";

    public bool CanOpenCalibration => IsRoutingActive;

    public int ActiveOutputCount
    {
        get => _activeOutputCount;
        private set => SetProperty(ref _activeOutputCount, value);
    }

    public int TotalOutputCount
    {
        get => _totalOutputCount;
        private set => SetProperty(ref _totalOutputCount, value);
    }

    public double LevelPercent
    {
        get => _levelPercent;
        private set => SetProperty(ref _levelPercent, value);
    }

    public bool IsConsoleExpanded
    {
        get => _isConsoleExpanded;
        private set
        {
            if (SetProperty(ref _isConsoleExpanded, value))
            {
                RaisePropertyChanged(nameof(IsConsoleContentVisible));
            }
        }
    }

    public bool IsConsoleContentVisible => IsRoutingActive || IsConsoleExpanded;

    public bool IsRoutingActive
    {
        get => _isRoutingActive;
        private set
        {
            if (SetProperty(ref _isRoutingActive, value))
            {
                IsConfigured = value;
                RaisePropertyChanged(nameof(CanOpenCalibration));
                RaisePropertyChanged(nameof(IsConsoleContentVisible));
            }
        }
    }

    public DeviceWorkspaceViewModel? ActiveTemplate
    {
        get => _activeTemplate;
        private set
        {
            if (SetProperty(ref _activeTemplate, value))
            {
                RaisePropertyChanged(nameof(PageTitle));
                RaisePropertyChanged(nameof(MasterDeviceDisplay));
                RaisePropertyChanged(nameof(SampleRateDisplay));
                RaisePropertyChanged(nameof(HasActiveTemplate));
                RaisePropertyChanged(nameof(SourceStatusText));
            }
        }
    }

    public AudioDeviceDescriptor? SelectedSource
    {
        get => _selectedSource;
        private set
        {
            if (SetProperty(ref _selectedSource, value) && value is not null)
            {
                SetProperty(ref _selectedSourceId, value.Id, nameof(SelectedSourceId));
                _service.SetSourceDevice(value.Id);
                IsConfigured = false;
                RaisePropertyChanged(nameof(SelectedSourceDisplayName));
            }
        }
    }

    public string SelectedSourceDisplayName
    {
        get
        {
            if (SelectedSource is null)
            {
                return "Select device";
            }

            return _showSystemDefaultAlias && SelectedSource.IsSystemDefaultSpeaker
                ? "System-Default"
                : SelectedSource.FriendlyName;
        }
    }

    public string? SelectedSourceId
    {
        get => _selectedSourceId;
        set
        {
            if (SetProperty(ref _selectedSourceId, value))
            {
                _showSystemDefaultAlias = false;
                SelectedSource = SourceDevices.FirstOrDefault(device =>
                    string.Equals(device.Id, value, StringComparison.Ordinal));
                RaisePropertyChanged(nameof(SelectedSourceDisplayName));
            }
        }
    }

    public string ScanStatus
    {
        get => _scanStatus;
        private set => SetProperty(ref _scanStatus, value);
    }

    public bool IsConfigured
    {
        get => _isConfigured;
        private set => SetProperty(ref _isConfigured, value);
    }

    public RelayCommand ScanCommand => new(Scan);

    public RelayCommand ApplyCommand => new(Apply);

    public RelayCommand TestAudioCommand => new(() =>
    {
        if (!IsRoutingActive)
        {
            ScanStatus = "Start routing from Calibration before testing audio";
            return;
        }

        try
        {
            _service.PlayCalibrationBeeps();
            ScanStatus = "Test audio sent through active routing";
        }
        catch (InvalidOperationException ex)
        {
            ScanStatus = ex.Message;
        }
    });

    public RelayCommand ToggleConsoleCommand => new(() => IsConsoleExpanded = !IsConsoleExpanded);

    public RelayCommand OpenCalibrationCommand => new(() =>
    {
        if (CanOpenCalibration)
        {
            OpenCalibrationRequested?.Invoke();
        }
    });

    public void SetActiveTemplate(DeviceWorkspaceViewModel? template)
    {
        ActiveTemplate = template;
        RefreshOverviewOutputs();
        RefreshLiveStatus();
        RaisePropertyChanged(nameof(MasterDeviceDisplay));
        RaisePropertyChanged(nameof(SampleRateDisplay));
        RefreshWindowsDefaultOutput();
        RaisePropertyChanged(nameof(WindowsDefaultDeviceName));

        if (template is null)
        {
            IsRoutingActive = false;
            ScanStatus = "No active routing template";
            return;
        }

        IsRoutingActive = template.IsRoutingActive;
        ScanStatus = $"{template.Name} loaded";
    }

    public void SetRoutingActive(bool isRoutingActive)
    {
        IsRoutingActive = isRoutingActive;
        RefreshOverviewOutputs();
        RefreshLiveStatus();
        RefreshWindowsDefaultOutput();
        ScanStatus = isRoutingActive ? "Routing active" : "Routing disabled";
    }

    public void RefreshLiveStatus()
    {
        var selectedOutputs = ActiveTemplate?.Outputs.Where(output => output.IsEnabled).ToList()
            ?? new List<DeviceViewModel>();

        ActiveOutputCount = selectedOutputs.Count;
        TotalOutputCount = Devices.Count;
        IsRoutingActive = _service.IsRoutingActive || ActiveTemplate?.IsRoutingActive == true;
        RoutingStatusText = IsRoutingActive ? "Routing active" : "Routing disabled";
        LevelPercent = IsRoutingActive
            ? Math.Min(1.0, Math.Max(0.05, _service.PeakLevel))
            : 0;

        _liveOutputTiles.Clear();
        foreach (var output in selectedOutputs)
        {
            var tile = CloneDevice(output);
            tile.IsActive = IsRoutingActive;
            _liveOutputTiles.Add(tile);
        }

        RefreshOverviewOutputs();

        // Throttle: refresh default device name roughly every 5 seconds
        // (timer fires at 120 ms → ~42 ticks per 5 s)
        _refreshTickCounter++;
        if (_refreshTickCounter >= 42)
        {
            _refreshTickCounter = 0;
            RefreshWindowsDefaultOutput();
        }
    }

    public void ResetConsoleExpansionForNavigation()
    {
        IsConsoleExpanded = false;
    }

    private void RefreshWindowsDefaultOutput()
    {
        var name = SystemDefaultAudioEndpointService.GetDefaultRenderEndpointName();
        WindowsDefaultDeviceName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
    }

    private void Scan()
    {
        _service.ScanDevices();
        SourceDevices.Clear();
        Devices.Clear();

        var renderDevices = _service.GetRenderDevices();
        foreach (var device in renderDevices)
        {
            SourceDevices.Add(device);
        }

        foreach (var device in renderDevices)
        {
            Devices.Add(new DeviceViewModel
            {
                Id = device.Id,
                Name = device.FriendlyName,
                IsDefault = device.IsDefault,
                IsEnabled = device.Id != "sb-virtual",
                Volume = device.Id == "sb-virtual" ? 100 : 80,
                DelayMs = device.Id == "sb-virtual" ? 0 : 80,
                IsActive = device.Id != "sb-virtual",
                SampleRateHz = device.SampleRateHz,
                ChannelCount = device.ChannelCount,
                BitsPerSample = device.BitsPerSample,
                VolumePercent = device.VolumePercent,
                FormFactor = device.FormFactor,
                Category = device.DeviceKindDisplay,
                IconPath = device.IconPath
            });

            Devices[^1].PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(DeviceViewModel.IsEnabled))
                {
                    IsConfigured = false;
                }
            };
        }

        SetSelectedSource(GetPreferredSourceDevice(), showSystemDefaultAlias: false);
        ScanStatus = $"Scanned {Devices.Count} devices";
        RefreshLiveStatus();
        RefreshWindowsDefaultOutput();
    }

    private void SetSelectedSource(AudioDeviceDescriptor? device, bool showSystemDefaultAlias)
    {
        _showSystemDefaultAlias = showSystemDefaultAlias && device?.IsSystemDefaultSpeaker == true;
        SelectedSource = device;
        RaisePropertyChanged(nameof(SelectedSourceDisplayName));
    }

    private AudioDeviceDescriptor? GetPreferredSourceDevice()
    {
        return SourceDevices.FirstOrDefault(d => d.IsDefault)
            ?? SourceDevices.FirstOrDefault(d => d.IsSystemDefaultSpeaker)
            ?? SourceDevices.FirstOrDefault();
    }

    private void Apply()
    {
        foreach (var device in Devices)
        {
            _service.SetOutputEnabled(device.Id, device.IsEnabled);
            _service.SetOutputVolume(device.Id, device.Volume);
            _service.SetOutputDelayMs(device.Id, device.DelayMs);
        }

        _service.ApplyConfiguration();
        IsConfigured = true;
        ScanStatus = "Configuration applied";
    }

    private void RefreshOverviewOutputs()
    {
        _overviewOutputs.Clear();

        if (ActiveTemplate is null)
        {
            _overviewOutputs.Add(new DeviceViewModel { Name = "Default Audio", DelayMs = 0 });
            return;
        }

        foreach (var output in ActiveTemplate.Outputs.Where(output => output.IsEnabled))
        {
            _overviewOutputs.Add(CloneDevice(output));
        }

        if (_overviewOutputs.Count == 0)
        {
            _overviewOutputs.Add(new DeviceViewModel { Name = "Default Audio", DelayMs = 0 });
        }
    }

    private static DeviceViewModel CloneDevice(DeviceViewModel source)
    {
        return new DeviceViewModel
        {
            Id = source.Id,
            Name = source.Name,
            IsDefault = source.IsDefault,
            IsEnabled = source.IsEnabled,
            Volume = source.Volume,
            DelayMs = source.DelayMs,
            IsActive = source.IsActive,
            SampleRateHz = source.SampleRateHz,
            ChannelCount = source.ChannelCount,
            BitsPerSample = source.BitsPerSample,
            VolumePercent = source.VolumePercent,
            FormFactor = source.FormFactor,
            Category = source.Category,
            IconPath = source.IconPath
        };
    }
}
