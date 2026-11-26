using System.Diagnostics;
using System.IO;
using SoundBridge.Gui.Services;

namespace SoundBridge.Gui.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsStore _settingsStore;
    private readonly StartupRegistrationService _startupRegistration;
    private bool _startWithWindows;
    private bool _minimizeToTray = true;
    private string _configPath;

    public SettingsViewModel(IAudioRouterService _, IAppSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _startupRegistration = new StartupRegistrationService();
        _configPath = settingsStore.SettingsPath;

        var settings = settingsStore.Load();
        _startWithWindows = settings.StartWithWindows || _startupRegistration.IsRegistered();
        _minimizeToTray = settings.MinimizeToTray;
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetProperty(ref _startWithWindows, value))
            {
                _startupRegistration.SetRegistered(value);
                Save();
            }
        }
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set
        {
            if (SetProperty(ref _minimizeToTray, value))
            {
                Save();
            }
        }
    }

    public string ConfigPath
    {
        get => _configPath;
        private set => SetProperty(ref _configPath, value);
    }

    public RelayCommand ResetCommand => new(() =>
    {
        StartWithWindows = false;
        MinimizeToTray = true;
    });

    public RelayCommand OpenFolderCommand => new(() =>
    {
        var directory = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo
            {
                FileName = directory,
                UseShellExecute = true
            });
        }
    });

    private void Save()
    {
        _settingsStore.Save(new AppSettings
        {
            StartWithWindows = StartWithWindows,
            MinimizeToTray = MinimizeToTray
        });
    }
}
