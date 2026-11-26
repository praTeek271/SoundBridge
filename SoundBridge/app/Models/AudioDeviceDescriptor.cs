namespace SoundBridge.Gui.Models;

public sealed class AudioDeviceDescriptor
{
    public required string Id { get; init; }

    public required string FriendlyName { get; init; }

    public bool IsDefault { get; set; }

    public int SampleRateHz { get; init; }

    public int ChannelCount { get; init; }

    public int BitsPerSample { get; init; }

    public int VolumePercent { get; init; } = -1;

    public int FormFactor { get; init; } = -1;

    public string Category { get; init; } = "Unknown";

    public string IconPath { get; init; } = string.Empty;

    public bool IsSystemDefaultSpeaker =>
        FriendlyName.Contains("speaker", StringComparison.OrdinalIgnoreCase);

    public string DisplayName => FriendlyName;

    public string SampleRateDisplay => SampleRateHz > 0
        ? $"{SampleRateHz / 1000.0:0.#} kHz"
        : "Unknown";

    public string ChannelDisplay => ChannelCount > 0
        ? $"{ChannelCount} ch"
        : "Unknown";

    public string BitDepthDisplay => BitsPerSample > 0
        ? $"{BitsPerSample}-bit"
        : "Unknown";

    public string VolumeDisplay => VolumePercent >= 0
        ? $"{VolumePercent}%"
        : "Unknown";

    public string DeviceKindDisplay => string.IsNullOrWhiteSpace(Category)
        ? "Unknown"
        : Category;
}
