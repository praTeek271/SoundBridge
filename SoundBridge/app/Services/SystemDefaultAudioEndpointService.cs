using System.Runtime.InteropServices;

namespace SoundBridge.Gui.Services;

internal static class SystemDefaultAudioEndpointService
{
    // ── Read the current Windows default playback device name ────────────────

    /// <summary>
    /// Returns the friendly name of the current Windows default render
    /// (playback) endpoint, or <c>null</c> if it cannot be determined.
    /// </summary>
    public static string? GetDefaultRenderEndpointName()
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IPropertyStore? store = null;
        object? enumeratorObj = null;

        try
        {
            enumeratorObj = new MmDeviceEnumeratorCoClass();
            enumerator = (IMMDeviceEnumerator)enumeratorObj;

            // eRender = 0, eConsole = 0
            enumerator.GetDefaultAudioEndpoint(0, 0, out device);
            if (device is null) return null;

            device.OpenPropertyStore(0 /* STGM_READ */, out store);
            if (store is null) return null;

            // PKEY_Device_FriendlyName
            var key = new PropVariantKey
            {
                FormatId = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
                PropertyId = 14
            };

            store.GetValue(ref key, out var pv);
            try
            {
                return pv.VarType == 31 /* VT_LPWSTR */ ? pv.StringVal : null;
            }
            finally
            {
                PropVariant.Clear(ref pv);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            if (store is not null) Marshal.ReleaseComObject(store);
            if (device is not null) Marshal.ReleaseComObject(device);
            if (enumeratorObj is not null) Marshal.FinalReleaseComObject(enumeratorObj);
        }
    }

    public static void SetDefaultRenderEndpoint(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("A valid audio device id is required.", nameof(deviceId));
        }

        object? policyConfigObject = null;

        try
        {
            policyConfigObject = new PolicyConfigClient();
            var policyConfig = (IPolicyConfig)policyConfigObject;

            SetDefaultEndpoint(policyConfig, deviceId, AudioRole.Console);
            SetDefaultEndpoint(policyConfig, deviceId, AudioRole.Multimedia);
            SetDefaultEndpoint(policyConfig, deviceId, AudioRole.Communications);
        }
        finally
        {
            if (policyConfigObject is not null)
            {
                Marshal.FinalReleaseComObject(policyConfigObject);
            }
        }
    }

    private static void SetDefaultEndpoint(IPolicyConfig policyConfig, string deviceId, AudioRole role)
    {
        var hr = policyConfig.SetDefaultEndpoint(deviceId, role);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    private enum AudioRole
    {
        Console = 0,
        Multimedia = 1,
        Communications = 2
    }

    [ComImport]
    [Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private sealed class PolicyConfigClient
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
    private interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceName, IntPtr format);

        [PreserveSig]
        int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceName, int defaultFormat, IntPtr format);

        [PreserveSig]
        int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceName);

        [PreserveSig]
        int SetDeviceFormat(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr endpointFormat,
            IntPtr mixFormat);

        [PreserveSig]
        int GetProcessingPeriod(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            int defaultPeriod,
            IntPtr defaultDevicePeriod,
            IntPtr minimumDevicePeriod);

        [PreserveSig]
        int SetProcessingPeriod(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr devicePeriod);

        [PreserveSig]
        int GetShareMode(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr mode);

        [PreserveSig]
        int SetShareMode(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            IntPtr mode);

        [PreserveSig]
        int GetPropertyValue(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            ref PropertyKey key,
            IntPtr propertyValue);

        [PreserveSig]
        int SetPropertyValue(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            ref PropertyKey key,
            IntPtr propertyValue);

        [PreserveSig]
        int SetDefaultEndpoint(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            AudioRole role);

        [PreserveSig]
        int SetEndpointVisibility(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceName,
            int visible);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid FormatId;
        public int PropertyId;
    }

    // ── COM interop for IMMDeviceEnumerator ──────────────────────────────────

    [ComImport]
    [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MmDeviceEnumeratorCoClass { }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, uint stateMask, out IntPtr devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? ppDevice);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice? ppDevice);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr pClient);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        [PreserveSig] int OpenPropertyStore(uint stgmAccess, out IPropertyStore? ppProperties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string? ppstrId);
        [PreserveSig] int GetState(out uint pdwState);
    }

    [ComImport]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);
        [PreserveSig] int GetAt(uint iProp, out PropVariantKey pkey);
        [PreserveSig] int GetValue(ref PropVariantKey key, out PropVariant pv);
        [PreserveSig] int SetValue(ref PropVariantKey key, ref PropVariant pv);
        [PreserveSig] int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariantKey
    {
        public Guid FormatId;
        public int PropertyId;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public ushort VarType;
        [FieldOffset(2)] public ushort Reserved1;
        [FieldOffset(4)] public ushort Reserved2;
        [FieldOffset(6)] public ushort Reserved3;
        [FieldOffset(8)] public IntPtr Data;

        public string? StringVal =>
            VarType == 31 /* VT_LPWSTR */ && Data != IntPtr.Zero
                ? Marshal.PtrToStringUni(Data)
                : null;

        public static void Clear(ref PropVariant pv)
        {
            PropVariantClear(ref pv);
        }

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant pv);
    }
}
