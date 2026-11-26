namespace SoundBridge.Gui.ViewModels;

public sealed class DeviceViewModel : ViewModelBase
{
    private bool _isEnabled;
    private int _volume;
    private int _delayMs;
    private bool _isDefault;
    private bool _isActive;
    private string _name = string.Empty;
    private string _id = string.Empty;
    private int _sampleRateHz;
    private int _channelCount;
    private int _bitsPerSample;
    private int _volumePercent = -1;
    private int _formFactor = -1;
    private string _category = "Unknown";
    private string _iconPath = string.Empty;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public int Volume
    {
        get => _volume;
        set => SetProperty(ref _volume, value);
    }

    public int DelayMs
    {
        get => _delayMs;
        set => SetProperty(ref _delayMs, value);
    }

    public bool IsDefault
    {
        get => _isDefault;
        set
        {
            if (SetProperty(ref _isDefault, value))
            {
                RaisePropertyChanged(nameof(MetadataSummary));
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public int SampleRateHz
    {
        get => _sampleRateHz;
        set
        {
            if (SetProperty(ref _sampleRateHz, value))
            {
                RaisePropertyChanged(nameof(MetadataSummary));
            }
        }
    }

    public int ChannelCount
    {
        get => _channelCount;
        set
        {
            if (SetProperty(ref _channelCount, value))
            {
                RaisePropertyChanged(nameof(MetadataSummary));
            }
        }
    }

    public int BitsPerSample
    {
        get => _bitsPerSample;
        set
        {
            if (SetProperty(ref _bitsPerSample, value))
            {
                RaisePropertyChanged(nameof(MetadataSummary));
            }
        }
    }

    public int VolumePercent
    {
        get => _volumePercent;
        set
        {
            if (SetProperty(ref _volumePercent, value))
            {
                RaisePropertyChanged(nameof(MetadataSummary));
            }
        }
    }

    public int FormFactor
    {
        get => _formFactor;
        set => SetProperty(ref _formFactor, value);
    }

    public string Category
    {
        get => _category;
        set
        {
            if (SetProperty(ref _category, value))
            {
                RaisePropertyChanged(nameof(MetadataSummary));
            }
        }
    }

    public string IconPath
    {
        get => _iconPath;
        set => SetProperty(ref _iconPath, value);
    }

    public string MetadataSummary
    {
        get
        {
            var sampleRate = SampleRateHz > 0 ? $"{SampleRateHz / 1000.0:0.#} kHz" : "Unknown rate";
            var channels = ChannelCount > 0 ? $"{ChannelCount} ch" : "Unknown ch";
            var bitDepth = BitsPerSample > 0 ? $"{BitsPerSample}-bit" : "Unknown bit";
            var volume = VolumePercent >= 0 ? $"{VolumePercent}% vol" : "Unknown vol";
            var defaultMarker = IsDefault ? "default" : "available";

            return $"{Category} / {sampleRate} / {channels} / {bitDepth} / {volume} / {defaultMarker}";
        }
    }
}
