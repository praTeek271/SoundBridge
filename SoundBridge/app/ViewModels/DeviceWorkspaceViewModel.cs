using System.Collections.ObjectModel;
using SoundBridge.Gui.Models;
using SoundBridge.Gui.Services;

namespace SoundBridge.Gui.ViewModels;

public sealed class DeviceWorkspaceViewModel : ViewModelBase
{
    private readonly IAudioRouterService _service;
    private readonly Action<DeviceWorkspaceViewModel>? _activate;
    private AudioDeviceDescriptor? _selectedSource;
    private string? _selectedSourceId;
    private string _statusText = string.Empty;
    private bool _isActive;
    private bool _showSystemDefaultAlias;
    private bool _isConfigurationApplied;
    private bool _isRoutingActive;
    private bool _hasPendingRoutingUpdate;

    public DeviceWorkspaceViewModel(
        IAudioRouterService service,
        string name,
        string deviceType,
        string? templateId = null,
        Action<DeviceWorkspaceViewModel>? activate = null)
    {
        _service = service;
        _activate = activate;
        TemplateId = string.IsNullOrWhiteSpace(templateId) ? Guid.NewGuid().ToString("N") : templateId;
        Name = name;
        DeviceType = deviceType;
        StatusText = "Draft device ready for configuration";
        SourceDevices = new ObservableCollection<AudioDeviceDescriptor>();
        Outputs = new ObservableCollection<DeviceViewModel>();
        Scan();
    }

    public event Action<DeviceWorkspaceViewModel>? RoutingChanged;

    public event Action<DeviceWorkspaceViewModel>? RoutingUpdateRequested;

    public event Action<DeviceWorkspaceViewModel>? DeleteRequested;

    public string Name { get; }

    public string TemplateId { get; }

    public string DeviceType { get; }

    public bool CanActivate => true;

    public bool CanSelect => IsConfigurationApplied;

    public bool IsRoutingActive
    {
        get => _isRoutingActive;
        private set
        {
            if (SetProperty(ref _isRoutingActive, value))
            {
                RaisePropertyChanged(nameof(SelectButtonText));
            }
        }
    }

    public bool HasPendingRoutingUpdate
    {
        get => _hasPendingRoutingUpdate;
        private set
        {
            if (SetProperty(ref _hasPendingRoutingUpdate, value))
            {
                RaisePropertyChanged(nameof(SelectButtonText));
            }
        }
    }

    public bool IsConfigurationApplied
    {
        get => _isConfigurationApplied;
        private set
        {
            if (SetProperty(ref _isConfigurationApplied, value))
            {
                RaisePropertyChanged(nameof(CanSelect));
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                RaisePropertyChanged(nameof(SelectButtonText));
            }
        }
    }

    public string SelectButtonText => IsActive && IsRoutingActive ? "Update" : IsActive ? "Deselect" : "Select";

    public string RoutingSummary => $"{InputCount} ins / {OutputCount} outs";

    public int InputCount => SelectedSource is null ? 0 : 1;

    public int OutputCount => Outputs.Count(output => output.IsEnabled);

    public ObservableCollection<DeviceViewModel> Outputs { get; }

    public ObservableCollection<AudioDeviceDescriptor> SourceDevices { get; }

    public AudioDeviceDescriptor? SelectedSource
    {
        get => _selectedSource;
        private set
        {
            if (SetProperty(ref _selectedSource, value))
            {
                SetProperty(ref _selectedSourceId, value?.Id, nameof(SelectedSourceId));
                RaisePropertyChanged(nameof(SelectedSourceDisplayName));
                RaisePropertyChanged(nameof(SampleRateKhz));
                InvalidateAppliedConfiguration();
                NotifyRoutingChanged();
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

    public string SampleRateKhz => SelectedSource?.SampleRateHz > 0
        ? $"{SelectedSource.SampleRateHz / 1000.0:0.#}"
        : "--";

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public RelayCommand DeleteCommand => new(() => DeleteRequested?.Invoke(this));

    public RelayCommand ScanCommand => new(Scan);

    public RelayCommand ApplyCommand => new(ApplyConfiguration);

    public RelayCommand SelectCommand => new(() =>
    {
        if (!CanActivate || !CanSelect)
        {
            StatusText = "Apply a valid configuration before selecting this template.";
            return;
        }

        if (IsActive && IsRoutingActive)
        {
            StatusText = HasPendingRoutingUpdate
                ? "Template metadata updated. Continue in Calibration to update routing."
                : "No pending template changes. Continue in Calibration to manage routing.";
            RoutingUpdateRequested?.Invoke(this);
            return;
        }

        _activate?.Invoke(this);

        StatusText = IsActive
            ? $"{Name} template saved and active. Open Calibration to start routing."
            : $"{Name} is now inactive";
    });

    private void Scan()
    {
        _service.ScanDevices();
        SourceDevices.Clear();
        Outputs.Clear();

        var renderDevices = _service.GetRenderDevices();
        foreach (var device in renderDevices)
        {
            SourceDevices.Add(device);
        }

        foreach (var device in renderDevices)
        {
            var output = new DeviceViewModel
            {
                Id = device.Id,
                Name = device.FriendlyName,
                IsDefault = device.IsDefault,
                IsEnabled = device.Id != "sb-virtual",
                Volume = device.Id == "sb-virtual" ? 100 : 80,
                DelayMs = 0,
                IsActive = device.Id != "sb-virtual",
                SampleRateHz = device.SampleRateHz,
                ChannelCount = device.ChannelCount,
                BitsPerSample = device.BitsPerSample,
                VolumePercent = device.VolumePercent,
                FormFactor = device.FormFactor,
                Category = device.DeviceKindDisplay,
                IconPath = device.IconPath
            };

            output.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(DeviceViewModel.IsEnabled)
                    or nameof(DeviceViewModel.Volume)
                    or nameof(DeviceViewModel.DelayMs))
                {
                    InvalidateAppliedConfiguration();
                    NotifyRoutingChanged();
                }
            };

            Outputs.Add(output);
        }

        SetSelectedSource(GetPreferredSourceDevice(), showSystemDefaultAlias: false);
        StatusText = $"Scanned {Outputs.Count} outputs for {Name}";
        InvalidateAppliedConfiguration();
        NotifyRoutingChanged();
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

    private void NotifyRoutingChanged()
    {
        RaisePropertyChanged(nameof(InputCount));
        RaisePropertyChanged(nameof(OutputCount));
        RaisePropertyChanged(nameof(RoutingSummary));
        RoutingChanged?.Invoke(this);
    }

    public void RestoreFrom(PersistedTemplate template)
    {
        if (!string.IsNullOrWhiteSpace(template.SourceDeviceId))
        {
            SelectedSourceId = template.SourceDeviceId;
        }

        foreach (var savedOutput in template.Outputs)
        {
            var output = Outputs.FirstOrDefault(item =>
                string.Equals(item.Id, savedOutput.DeviceId, StringComparison.Ordinal));

            if (output is null)
            {
                continue;
            }

            output.IsEnabled = savedOutput.IsEnabled;
            output.Volume = Math.Clamp(savedOutput.Volume, 0, 100);
            output.DelayMs = Math.Clamp(savedOutput.DelayMs, 0, 2000);
        }

        IsConfigurationApplied = SelectedSource is not null
            && Outputs.Any(output => output.IsEnabled && output.Id != SelectedSource.Id);
        HasPendingRoutingUpdate = false;
        StatusText = $"{Name} restored from saved template";
        NotifyRoutingChanged();
    }

    public PersistedTemplate ToPersistedTemplate(bool isSelected)
    {
        return new PersistedTemplate
        {
            Id = TemplateId,
            Name = Name,
            Type = DeviceType,
            IsSelected = isSelected,
            IsRoutingActiveOnLastExit = IsRoutingActive,
            SourceDeviceId = SelectedSource?.Id,
            SourceDeviceName = SelectedSource?.FriendlyName,
            SampleRateHz = SelectedSource?.SampleRateHz ?? 0,
            Outputs = Outputs.Select(output => new PersistedTemplateOutput
            {
                DeviceId = output.Id,
                DeviceName = output.Name,
                IsEnabled = output.IsEnabled,
                Volume = output.Volume,
                DelayMs = output.DelayMs
            }).ToList(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public bool TryStartRouting(out string message)
    {
        if (!string.Equals(DeviceType, "Multi-output", StringComparison.Ordinal))
        {
            message = "Aggregate routing is not implemented yet.";
            return false;
        }

        if (SelectedSource is null)
        {
            message = "Select a master device before starting routing.";
            return false;
        }

        var selectedOutputs = Outputs
            .Where(output => output.IsEnabled && !string.Equals(output.Id, SelectedSource.Id, StringComparison.Ordinal))
            .ToList();

        if (selectedOutputs.Count == 0)
        {
            message = "Select at least one output device before starting routing.";
            return false;
        }

        if (!IsConfigurationApplied)
        {
            message = "Apply configuration before starting calibration routing.";
            return false;
        }

        try
        {
            if (_service.IsRoutingActive)
            {
                _service.StopRouting();
            }

            _service.SetSourceDevice(SelectedSource.Id);

            foreach (var output in Outputs)
            {
                var enabled = selectedOutputs.Any(selected => string.Equals(selected.Id, output.Id, StringComparison.Ordinal));
                _service.SetOutputEnabled(output.Id, enabled);
                _service.SetOutputVolume(output.Id, output.Volume);
                _service.SetOutputDelayMs(output.Id, output.DelayMs);
            }

            _service.ApplyConfiguration();
            _service.StartRouting();

            message = $"{Name} routing started: {SelectedSource.FriendlyName} -> {selectedOutputs.Count} output(s)";
            IsRoutingActive = true;
            HasPendingRoutingUpdate = false;
            StatusText = message;
            return true;
        }
        catch (Exception ex)
        {
            _service.StopRouting();
            message = $"Routing failed: {ex.Message}";
            StatusText = message;
            return false;
        }
    }

    public void StopRouting()
    {
        if (string.Equals(DeviceType, "Multi-output", StringComparison.Ordinal))
        {
            _service.StopRouting();
        }

        IsRoutingActive = false;
        HasPendingRoutingUpdate = false;
        RaisePropertyChanged(nameof(SelectButtonText));
    }

    public bool TryUpdateRouting(out string message)
    {
        if (!IsRoutingActive)
        {
            message = "Start routing before updating routing metadata.";
            return false;
        }

        try
        {
            if (SelectedSource is not null)
            {
                _service.SetSourceDevice(SelectedSource.Id);
            }

            foreach (var output in Outputs)
            {
                _service.SetOutputEnabled(output.Id, output.IsEnabled && output.Id != SelectedSource?.Id);
                _service.SetOutputVolume(output.Id, output.Volume);
                _service.SetOutputDelayMs(output.Id, output.DelayMs);
            }

            _service.UpdateRouting();
            IsConfigurationApplied = true;
            HasPendingRoutingUpdate = false;
            message = "Routing metadata updated";
            StatusText = message;
            return true;
        }
        catch (Exception ex)
        {
            message = $"Routing update failed: {ex.Message}";
            StatusText = message;
            return false;
        }
    }

    private void ApplyConfiguration()
    {
        if (SelectedSource is null)
        {
            StatusText = "Select a master device before applying configuration.";
            IsConfigurationApplied = false;
            return;
        }

        var selectedOutputs = Outputs
            .Where(output => output.IsEnabled && !string.Equals(output.Id, SelectedSource.Id, StringComparison.Ordinal))
            .ToList();

        if (selectedOutputs.Count == 0)
        {
            StatusText = "Select at least one output device before applying configuration.";
            IsConfigurationApplied = false;
            return;
        }

        try
        {
            if (!IsRoutingActive)
            {
                _service.SetSourceDevice(SelectedSource.Id);

                foreach (var output in Outputs)
                {
                    var enabled = selectedOutputs.Any(selected => string.Equals(selected.Id, output.Id, StringComparison.Ordinal));
                    _service.SetOutputEnabled(output.Id, enabled);
                    _service.SetOutputVolume(output.Id, output.Volume);
                    _service.SetOutputDelayMs(output.Id, output.DelayMs);
                }

                _service.ApplyConfiguration();
            }

            IsConfigurationApplied = true;
            HasPendingRoutingUpdate = IsActive && IsRoutingActive;
            StatusText = HasPendingRoutingUpdate
                ? $"{Name} template metadata checked. Click Update to continue in Calibration."
                : $"{Name} configuration checked and staged. Select is now enabled.";
        }
        catch (Exception ex)
        {
            IsConfigurationApplied = false;
            StatusText = $"Configuration check failed: {ex.Message}";
        }
    }

    private void InvalidateAppliedConfiguration()
    {
        IsConfigurationApplied = false;
        if (IsActive && IsRoutingActive)
        {
            HasPendingRoutingUpdate = true;
            StatusText = "Routing changes pending. Apply configuration, then update from Calibration.";
            return;
        }

        if (IsActive)
        {
            IsActive = false;
        }
    }
}
