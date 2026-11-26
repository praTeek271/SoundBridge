using System.IO;
using System.Text.Json;

namespace SoundBridge.Gui.Services;

public sealed class JsonTemplateStore : ITemplateStore
{
    private const int CurrentSchemaVersion = 1;
    private const int MaxLoggedRawChars = 4000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _rootDirectory;
    private readonly string _templatesPath;
    private readonly string _backupPath;
    private readonly string _logDirectory;
    private readonly string _logPath;
    private readonly string _corruptedDirectory;

    public JsonTemplateStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SoundBridge"))
    {
    }

    public JsonTemplateStore(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
        _templatesPath = Path.Combine(_rootDirectory, "templates.json");
        _backupPath = Path.Combine(_rootDirectory, "templates.backup.json");
        _logDirectory = Path.Combine(_rootDirectory, "logs");
        _logPath = Path.Combine(_logDirectory, "app.log");
        _corruptedDirectory = Path.Combine(_rootDirectory, "corrupted");

        Directory.CreateDirectory(_rootDirectory);
        Directory.CreateDirectory(_logDirectory);
        Directory.CreateDirectory(_corruptedDirectory);
    }

    public PersistedTemplateDocument Load()
    {
        var primary = TryLoadFile(_templatesPath, "primary");
        if (primary.Document is not null)
        {
            return primary.Document;
        }

        var backup = TryLoadFile(_backupPath, "backup");
        if (backup.Document is not null)
        {
            AppendLog("template-load-restored-backup", "Primary template file failed; backup loaded and restored.");
            Save(backup.Document, "restore backup after primary load failure");
            QuarantineIfExists(_templatesPath, "templates", primary.Error);
            return backup.Document;
        }

        if (primary.Error is not null || backup.Error is not null)
        {
            AppendLog(
                "template-load-failed",
                $"Primary and backup template files failed. Starting with empty templates. Backup error: {backup.Error?.Message ?? "none"}",
                primary.RawText,
                backup.RawText,
                primary.Error);

            QuarantineIfExists(_templatesPath, "templates", primary.Error);
            QuarantineIfExists(_backupPath, "templates.backup", backup.Error);
        }

        return new PersistedTemplateDocument { SchemaVersion = CurrentSchemaVersion };
    }

    public void Save(PersistedTemplateDocument document, string reason)
    {
        document.SchemaVersion = CurrentSchemaVersion;
        AppendLog("template-save-start", reason);

        Directory.CreateDirectory(_rootDirectory);
        Directory.CreateDirectory(_logDirectory);

        Validate(document);

        if (File.Exists(_templatesPath))
        {
            File.Copy(_templatesPath, _backupPath, overwrite: true);
        }

        var temporaryPath = Path.Combine(_rootDirectory, "templates.tmp");
        try
        {
            var json = JsonSerializer.Serialize(document, JsonOptions);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(_templatesPath))
            {
                File.Replace(temporaryPath, _templatesPath, null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, _templatesPath);
            }

            AppendLog("template-save-complete", $"{reason}; templates={document.Templates.Count}");
        }
        catch (Exception ex)
        {
            AppendLog("template-save-failed", reason, error: ex);
            throw;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private LoadResult TryLoadFile(string path, string label)
    {
        if (!File.Exists(path))
        {
            return new LoadResult(null, null, null);
        }

        string? rawText = null;
        try
        {
            rawText = File.ReadAllText(path);
            var document = JsonSerializer.Deserialize<PersistedTemplateDocument>(rawText, JsonOptions);
            if (document is null)
            {
                throw new InvalidDataException($"{label} template file deserialized as null.");
            }

            Validate(document);
            AppendLog("template-load-complete", $"{label}; templates={document.Templates.Count}");
            return new LoadResult(document, rawText, null);
        }
        catch (Exception ex)
        {
            AppendLog("template-load-file-failed", $"{label}: {path}", rawText, error: ex);
            return new LoadResult(null, rawText, ex);
        }
    }

    private void Validate(PersistedTemplateDocument document)
    {
        if (document.SchemaVersion <= 0 || document.SchemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported template schema version: {document.SchemaVersion}.");
        }

        document.Templates ??= new List<PersistedTemplate>();

        foreach (var template in document.Templates)
        {
            template.Id = CleanRequired(template.Id, Guid.NewGuid().ToString("N"));
            template.Name = CleanRequired(template.Name, "Untitled template");
            template.Type = CleanRequired(template.Type, "Multi-output");
            template.Outputs ??= new List<PersistedTemplateOutput>();

            foreach (var output in template.Outputs)
            {
                output.DeviceId = CleanRequired(output.DeviceId, string.Empty);
                output.DeviceName = CleanRequired(output.DeviceName, "Unknown device");
                output.Volume = Math.Clamp(output.Volume, 0, 100);
                output.DelayMs = Math.Clamp(output.DelayMs, 0, 2000);
            }
        }
    }

    private static string CleanRequired(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private void QuarantineIfExists(string path, string label, Exception? error)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var destination = Path.Combine(_corruptedDirectory, $"{label}.{timestamp}.bad.json");

        try
        {
            File.Move(path, destination, overwrite: true);
            AppendLog("template-file-quarantined", $"{path} -> {destination}", error: error);
        }
        catch (Exception ex)
        {
            AppendLog("template-file-quarantine-failed", path, error: ex);
        }
    }

    private void AppendLog(
        string eventName,
        string message,
        string? primaryRawText = null,
        string? backupRawText = null,
        Exception? error = null)
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);

            using var writer = File.AppendText(_logPath);
            writer.WriteLine($"[{DateTime.UtcNow:O}] {eventName}: {message}");

            if (error is not null)
            {
                writer.WriteLine($"error: {error.GetType().Name}: {error.Message}");
            }

            WriteRawSnippet(writer, "primary-raw", primaryRawText);
            WriteRawSnippet(writer, "backup-raw", backupRawText);
        }
        catch
        {
            // Logging must never break startup or template saving.
        }
    }

    private static void WriteRawSnippet(TextWriter writer, string label, string? rawText)
    {
        if (string.IsNullOrEmpty(rawText))
        {
            return;
        }

        var snippet = rawText.Length > MaxLoggedRawChars
            ? rawText[..MaxLoggedRawChars] + "...[truncated]"
            : rawText;

        writer.WriteLine($"{label}:");
        writer.WriteLine(snippet);
    }

    private sealed record LoadResult(
        PersistedTemplateDocument? Document,
        string? RawText,
        Exception? Error);
}
