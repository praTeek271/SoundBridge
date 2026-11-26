using System.Runtime.InteropServices;
using System.Text;

namespace SoundBridge.Gui.Services;

/// <summary>
/// Raw P/Invoke declarations for SoundBridgeBridge.dll.
/// Do not call these directly — use NativeAudioRouterService instead.
/// </summary>
internal static class NativeAudioRouterInterop
{
    private const string DllName = "SoundBridgeBridge.dll";

    // ---- Delegate for device enumeration callback --------------------------

    /// <summary>
    /// Called once per device during SoundBridge_EnumerateRenderDevices.
    /// The delegate must be kept alive (stored as a field) for the duration
    /// of the native call — the GC must not collect it mid-call.
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    internal delegate void DeviceCallback(
        [MarshalAs(UnmanagedType.LPWStr)] string id,
        [MarshalAs(UnmanagedType.LPWStr)] string name,
        int isDefault,
        int sampleRateHz,
        int channelCount,
        int bitsPerSample,
        int volumePercent,
        int formFactor,
        [MarshalAs(UnmanagedType.LPWStr)] string category,
        [MarshalAs(UnmanagedType.LPWStr)] string iconPath,
        IntPtr userData);

    // ---- Engine lifecycle --------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern IntPtr SoundBridge_CreateEngine();

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern void SoundBridge_DestroyEngine(IntPtr handle);

    // ---- Device enumeration ------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SoundBridge_EnumerateRenderDevices(
        IntPtr handle,
        DeviceCallback callback,
        IntPtr userData);

    // ---- Source / output configuration -------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall,
               CharSet = CharSet.Unicode)]
    internal static extern int SoundBridge_SetSourceDevice(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SoundBridge_ClearOutputDevices(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall,
               CharSet = CharSet.Unicode)]
    internal static extern int SoundBridge_AddOutputDevice(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    // ---- Per-output settings (NEW) ----------------------------------------

    /// <summary>gain — linear multiplier [0.0, 1.0]</summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall,
               CharSet = CharSet.Unicode)]
    internal static extern int SoundBridge_SetOutputVolume(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        float gain);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall,
               CharSet = CharSet.Unicode)]
    internal static extern int SoundBridge_SetOutputDelayMs(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        int delayMs);

    // ---- Routing lifecycle -------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SoundBridge_StartRouting(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern void SoundBridge_StopRouting(IntPtr handle);

    /// <summary>
    /// Stop + reconfigure + restart in one native call.
    /// outputIds is an array of wchar_t* pointers.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall,
               CharSet = CharSet.Unicode)]
    internal static extern int SoundBridge_UpdateRouting(
        IntPtr handle,
        [MarshalAs(UnmanagedType.LPWStr)] string sourceId,
        [MarshalAs(UnmanagedType.LPArray,
                   ArraySubType = UnmanagedType.LPWStr)] string[] outputIds,
        int outputCount);

    // ---- Calibration (NEW) -------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SoundBridge_PlayCalibrationTone(IntPtr handle);

    // ---- Status ------------------------------------------------------------

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SoundBridge_IsRoutingActive(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern float SoundBridge_GetPeakLevel(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SoundBridge_GetActiveOutputCount(IntPtr handle);

    // ---- Error reporting (NEW) ---------------------------------------------

    /// <summary>
    /// Copies the last error message (UTF-8) into buf.
    /// Returns byte count written; 0 = no error.
    /// </summary>
    [DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
    internal static extern int SoundBridge_GetLastError(
        IntPtr handle,
        StringBuilder buf,
        int bufSize);
}
