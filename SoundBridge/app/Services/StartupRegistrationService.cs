using Microsoft.Win32;

namespace SoundBridge.Gui.Services;

public sealed class StartupRegistrationService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SoundBridge";

    public bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) is string value
            && value.Contains("SoundBridge", StringComparison.OrdinalIgnoreCase);
    }

    public void SetRegistered(bool isRegistered)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (isRegistered)
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                key.SetValue(ValueName, $"\"{executablePath}\"");
            }

            return;
        }

        key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
