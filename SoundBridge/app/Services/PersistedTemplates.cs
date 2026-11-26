namespace SoundBridge.Gui.Services;

public sealed class PersistedTemplateDocument
{
    public int SchemaVersion { get; set; } = 1;

    public List<PersistedTemplate> Templates { get; set; } = new();
}

public sealed class PersistedTemplate
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "Multi-output";

    public bool IsSelected { get; set; }

    public bool IsRoutingActiveOnLastExit { get; set; }

    public string? SourceDeviceId { get; set; }

    public string? SourceDeviceName { get; set; }

    public int SampleRateHz { get; set; }

    public List<PersistedTemplateOutput> Outputs { get; set; } = new();

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PersistedTemplateOutput
{
    public string DeviceId { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public int Volume { get; set; }

    public int DelayMs { get; set; }
}
