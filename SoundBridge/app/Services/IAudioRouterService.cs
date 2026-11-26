using SoundBridge.Gui.Models;

namespace SoundBridge.Gui.Services;

public interface IAudioRouterService
{
    IReadOnlyList<AudioDeviceDescriptor> GetRenderDevices();

    IReadOnlyList<AudioDeviceDescriptor> GetSelectedOutputs();

    AudioDeviceDescriptor? GetSourceDevice();

    bool IsRoutingActive { get; }

    double PeakLevel { get; }

    int ActiveOutputCount { get; }

    void SetSourceDevice(string deviceId);

    void SetOutputEnabled(string deviceId, bool enabled);

    void SetOutputVolume(string deviceId, int volume);

    void SetOutputDelayMs(string deviceId, int delayMs);

    void SetSystemDefaultOutputDevice(string deviceId);

    void ScanDevices();

    void ApplyConfiguration();

    void StartRouting();

    void StopRouting();

    void UpdateRouting();

    void PlayCalibrationBeeps();
}
