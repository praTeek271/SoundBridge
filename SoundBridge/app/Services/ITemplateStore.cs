namespace SoundBridge.Gui.Services;

public interface ITemplateStore
{
    PersistedTemplateDocument Load();

    void Save(PersistedTemplateDocument document, string reason);
}
