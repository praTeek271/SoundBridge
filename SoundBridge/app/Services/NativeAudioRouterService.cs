using SoundBridge.Gui.Models;
using System.Text;

namespace SoundBridge.Gui.Services;

/// <summary>
/// Production implementation of IAudioRouterService.
/// Delegates to SoundBridgeBridge.dll via P/Invoke.
/// </summary>
public sealed class NativeAudioRouterService : IAudioRouterService, IDisposable
{
    // ---- Private state -----------------------------------------------------

    private readonly IntPtr _handle;
    private bool _disposed;

    // Device list cache (refreshed on ScanDevices / GetRenderDevices)
    private List<AudioDeviceDescriptor> _deviceCache = new();

    // Source device selected by the UI
    private string? _sourceDeviceId;

    // Per-output config: enabled flag, volume (0–100), delay (ms)
    private record struct OutputConfig(bool Enabled, int Volume, int DelayMs);
    private readonly Dictionary<string, OutputConfig> _outputConfig = new();

    // Keep delegate alive — the GC must not collect it between CreateDelegate
    // and the end of the native call.
    private NativeAudioRouterInterop.DeviceCallback? _enumCallback;

    // ---- Construction / disposal -------------------------------------------

    public NativeAudioRouterService()
    {
        _handle = NativeAudioRouterInterop.SoundBridge_CreateEngine();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException(
                "SoundBridge_CreateEngine returned null. " +
                "Ensure SoundBridgeBridge.dll is present and all VC++ runtimes are installed.");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        NativeAudioRouterInterop.SoundBridge_StopRouting(_handle);
        NativeAudioRouterInterop.SoundBridge_DestroyEngine(_handle);
    }

    // ---- Helpers -----------------------------------------------------------

    private string GetNativeLastError()
    {
        var buf = new StringBuilder(1024);
        NativeAudioRouterInterop.SoundBridge_GetLastError(_handle, buf, buf.Capacity);
        return buf.ToString();
    }

    private void ThrowIfFailed(int result, string operation)
    {
        if (result == 0)
        {
            string err = GetNativeLastError();
            throw new InvalidOperationException(
                string.IsNullOrEmpty(err)
                    ? $"{operation} failed (no details from native layer)."
                    : $"{operation} failed: {err}");
        }
    }

    // ---- IAudioRouterService — Device enumeration -------------------------

    public IReadOnlyList<AudioDeviceDescriptor> GetRenderDevices()
    {
        var results = new List<AudioDeviceDescriptor>();

        // Store delegate in field to prevent GC collection during native call.
        _enumCallback = (
            id,
            name,
            isDefault,
            sampleRateHz,
            channelCount,
            bitsPerSample,
            volumePercent,
            formFactor,
            category,
            iconPath,
            _) =>
        {
            results.Add(new AudioDeviceDescriptor
            {
                Id = id,
                FriendlyName = name,
                IsDefault = isDefault != 0,
                SampleRateHz = sampleRateHz,
                ChannelCount = channelCount,
                BitsPerSample = bitsPerSample,
                VolumePercent = volumePercent,
                FormFactor = formFactor,
                Category = category,
                IconPath = iconPath
            });
        };

        NativeAudioRouterInterop.SoundBridge_EnumerateRenderDevices(
            _handle, _enumCallback, IntPtr.Zero);

        _deviceCache = results;

        // Seed output config for any newly discovered device (preserve existing).
        foreach (var device in _deviceCache)
        {
            if (!_outputConfig.ContainsKey(device.Id))
                _outputConfig[device.Id] = new OutputConfig(false, 100, 0);
        }

        return results;
    }

    public void ScanDevices() => GetRenderDevices();

    // ---- IAudioRouterService — Source / output selection ------------------

    public AudioDeviceDescriptor? GetSourceDevice()
        => _deviceCache.FirstOrDefault(d => d.Id == _sourceDeviceId);

    public IReadOnlyList<AudioDeviceDescriptor> GetSelectedOutputs()
        => _deviceCache
            .Where(d => _outputConfig.TryGetValue(d.Id, out var cfg) && cfg.Enabled)
            .ToList();

    public void SetSourceDevice(string deviceId)
    {
        _sourceDeviceId = deviceId;
        // Deferred to ApplyConfiguration — engine must not be running here.
    }

    public void SetOutputEnabled(string deviceId, bool enabled)
    {
        if (_outputConfig.TryGetValue(deviceId, out var cfg))
            _outputConfig[deviceId] = cfg with { Enabled = enabled };
        else
            _outputConfig[deviceId] = new OutputConfig(enabled, 100, 0);
    }

    public void SetOutputVolume(string deviceId, int volume)
    {
        int clamped = Math.Clamp(volume, 0, 100);

        if (_outputConfig.TryGetValue(deviceId, out var cfg))
            _outputConfig[deviceId] = cfg with { Volume = clamped };
        else
            _outputConfig[deviceId] = new OutputConfig(false, clamped, 0);

        // Apply immediately if routing is already active (live volume change).
        if (IsRoutingActive)
        {
            float gain = clamped / 100.0f;
            NativeAudioRouterInterop.SoundBridge_SetOutputVolume(_handle, deviceId, gain);
        }
    }

    public void SetOutputDelayMs(string deviceId, int delayMs)
    {
        int clamped = Math.Clamp(delayMs, 0, 2000);

        if (_outputConfig.TryGetValue(deviceId, out var cfg))
            _outputConfig[deviceId] = cfg with { DelayMs = clamped };
        else
            _outputConfig[deviceId] = new OutputConfig(false, 100, clamped);

        // Delay takes effect on next Start() / UpdateRouting() —
        // it cannot be hot-reloaded on a live stream (requires queue rebuild).
        if (IsRoutingActive)
        {
            NativeAudioRouterInterop.SoundBridge_SetOutputDelayMs(_handle, deviceId, clamped);
        }
    }

    public void SetSystemDefaultOutputDevice(string deviceId)
        => SystemDefaultAudioEndpointService.SetDefaultRenderEndpoint(deviceId);

    // ---- IAudioRouterService — Configuration / lifecycle ------------------

    /// <summary>
    /// Validates the current template and stages it into the native engine.
    /// Does NOT start routing. Call StartRouting() after this.
    /// </summary>
    public void ApplyConfiguration()
    {
        if (string.IsNullOrEmpty(_sourceDeviceId))
            throw new InvalidOperationException("No source device selected.");

        var enabled = _outputConfig
            .Where(kv => kv.Value.Enabled)
            .ToList();

        if (enabled.Count == 0)
            throw new InvalidOperationException("No output devices are enabled.");

        // If routing is already active, stop before reconfiguring.
        if (IsRoutingActive)
            NativeAudioRouterInterop.SoundBridge_StopRouting(_handle);

        ThrowIfFailed(
            NativeAudioRouterInterop.SoundBridge_SetSourceDevice(_handle, _sourceDeviceId),
            "SetSourceDevice");

        NativeAudioRouterInterop.SoundBridge_ClearOutputDevices(_handle);

        foreach (var (id, cfg) in enabled)
        {
            ThrowIfFailed(
                NativeAudioRouterInterop.SoundBridge_AddOutputDevice(_handle, id),
                $"AddOutputDevice ({id})");

            NativeAudioRouterInterop.SoundBridge_SetOutputVolume(
                _handle, id, cfg.Volume / 100.0f);

            NativeAudioRouterInterop.SoundBridge_SetOutputDelayMs(
                _handle, id, cfg.DelayMs);
        }
    }

    public void StartRouting()
    {
        if (!string.IsNullOrEmpty(_sourceDeviceId))
        {
            SetSystemDefaultOutputDevice(_sourceDeviceId);
        }

        ThrowIfFailed(
            NativeAudioRouterInterop.SoundBridge_StartRouting(_handle),
            "StartRouting");
    }

    public void StopRouting()
        => NativeAudioRouterInterop.SoundBridge_StopRouting(_handle);

    /// <summary>
    /// Applies pending configuration changes to an active routing session.
    /// Internally: StopRouting → ApplyConfiguration → StartRouting.
    /// Safe to call even if routing is not currently active.
    /// </summary>
    public void UpdateRouting()
    {
        NativeAudioRouterInterop.SoundBridge_StopRouting(_handle);
        ApplyConfiguration();
        StartRouting();
    }

    // ---- IAudioRouterService — Calibration --------------------------------

    public void PlayCalibrationBeeps()
    {
        ThrowIfFailed(
            NativeAudioRouterInterop.SoundBridge_PlayCalibrationTone(_handle),
            "PlayCalibrationTone");
    }

    // ---- IAudioRouterService — Status -------------------------------------

    public bool IsRoutingActive
        => NativeAudioRouterInterop.SoundBridge_IsRoutingActive(_handle) != 0;

    public double PeakLevel
        => NativeAudioRouterInterop.SoundBridge_GetPeakLevel(_handle);

    public int ActiveOutputCount
        => NativeAudioRouterInterop.SoundBridge_GetActiveOutputCount(_handle);
}
