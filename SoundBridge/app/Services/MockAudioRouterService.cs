using SoundBridge.Gui.Models;

namespace SoundBridge.Gui.Services;

public sealed class MockAudioRouterService : IAudioRouterService
{
    private readonly List<AudioDeviceDescriptor> _devices = new()
    {
        new AudioDeviceDescriptor
        {
            Id = "sb-virtual",
            FriendlyName = "SoundBridge Virtual Device",
            IsDefault = true,
            SampleRateHz = 48000,
            ChannelCount = 2,
            BitsPerSample = 32,
            VolumePercent = 100,
            FormFactor = 1,
            Category = "Speakers",
            IconPath = "%SystemRoot%\\System32\\mmres.dll,-3010"
        },
        new AudioDeviceDescriptor
        {
            Id = "bt-headset",
            FriendlyName = "Sony WH-1000XM5 (Bluetooth)",
            SampleRateHz = 48000,
            ChannelCount = 2,
            BitsPerSample = 16,
            VolumePercent = 72,
            FormFactor = 5,
            Category = "Headset",
            IconPath = "%SystemRoot%\\System32\\mmres.dll,-3012"
        },
        new AudioDeviceDescriptor
        {
            Id = "usb-headset",
            FriendlyName = "USB Headset Pro",
            SampleRateHz = 48000,
            ChannelCount = 2,
            BitsPerSample = 24,
            VolumePercent = 80,
            FormFactor = 3,
            Category = "Headphones",
            IconPath = "%SystemRoot%\\System32\\mmres.dll,-3011"
        },
        new AudioDeviceDescriptor
        {
            Id = "realtek",
            FriendlyName = "Realtek HD Audio",
            SampleRateHz = 44100,
            ChannelCount = 2,
            BitsPerSample = 16,
            VolumePercent = 65,
            FormFactor = 1,
            Category = "Speakers",
            IconPath = "%SystemRoot%\\System32\\mmres.dll,-3010"
        }
    };

    private readonly Dictionary<string, OutputDeviceState> _outputStates;
    private string _sourceDeviceId = "sb-virtual";
    private bool _isRoutingActive;
    private double _peakLevel = 0.18;

    public MockAudioRouterService()
    {
        _outputStates = _devices.ToDictionary(
            d => d.Id,
            d => new OutputDeviceState
            {
                Descriptor = d,
                Enabled = d.Id != "sb-virtual",
                Volume = d.Id == "sb-virtual" ? 100 : 80,
                DelayMs = d.Id == "sb-virtual" ? 0 : 80
            });
    }

    public IReadOnlyList<AudioDeviceDescriptor> GetRenderDevices() => _devices;

    public IReadOnlyList<AudioDeviceDescriptor> GetSelectedOutputs() => _outputStates.Values
        .Where(s => s.Enabled && s.Descriptor.Id != _sourceDeviceId)
        .Select(s => s.Descriptor)
        .ToList();

    public AudioDeviceDescriptor? GetSourceDevice() =>
        _devices.FirstOrDefault(d => d.Id == _sourceDeviceId);

    public bool IsRoutingActive => _isRoutingActive;

    public double PeakLevel => _peakLevel;

    public int ActiveOutputCount => GetSelectedOutputs().Count;

    public void SetSourceDevice(string deviceId)
    {
        if (_devices.Any(d => d.Id == deviceId))
        {
            _sourceDeviceId = deviceId;
            if (_sourceDeviceId == "sb-virtual")
            {
                _outputStates["sb-virtual"].Enabled = false;
            }
        }
    }

    public void SetOutputEnabled(string deviceId, bool enabled)
    {
        if (_outputStates.TryGetValue(deviceId, out var state))
        {
            state.Enabled = enabled && deviceId != _sourceDeviceId;
        }
    }

    public void SetOutputVolume(string deviceId, int volume)
    {
        if (_outputStates.TryGetValue(deviceId, out var state))
        {
            state.Volume = Math.Clamp(volume, 0, 100);
        }
    }

    public void SetOutputDelayMs(string deviceId, int delayMs)
    {
        if (_outputStates.TryGetValue(deviceId, out var state))
        {
            state.DelayMs = Math.Clamp(delayMs, 0, 500);
        }
    }

    public void SetSystemDefaultOutputDevice(string deviceId)
    {
        SetSourceDevice(deviceId);
        foreach (var dev in _devices)
        {
            dev.IsDefault = string.Equals(dev.Id, deviceId, StringComparison.Ordinal);
        }
    }

    public void ScanDevices()
    {
        _peakLevel = 0.12 + Random.Shared.NextDouble() * 0.15;
    }

    public void ApplyConfiguration()
    {
        _peakLevel = 0.20 + Random.Shared.NextDouble() * 0.20;
    }

    public void StartRouting()
    {
        _isRoutingActive = true;
        _peakLevel = 0.35;
        if (!string.IsNullOrEmpty(_sourceDeviceId))
        {
            SetSystemDefaultOutputDevice(_sourceDeviceId);
        }
    }

    public void StopRouting()
    {
        _isRoutingActive = false;
        _peakLevel = 0.0;
    }

    public void UpdateRouting()
    {
        _isRoutingActive = true;
        _peakLevel = 0.42;
    }

    public void PlayCalibrationBeeps()
    {
        _peakLevel = 0.95;
    }

    private sealed class OutputDeviceState
    {
        public required AudioDeviceDescriptor Descriptor { get; init; }

        public bool Enabled { get; set; }

        public int Volume { get; set; }

        public int DelayMs { get; set; }
    }
}
