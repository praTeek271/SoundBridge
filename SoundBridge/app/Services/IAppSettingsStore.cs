namespace SoundBridge.Gui.Services;

public interface IAppSettingsStore
{
    string SettingsPath { get; }

    AppSettings Load();

    void Save(AppSettings settings);
}
