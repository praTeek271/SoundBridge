using System.IO;
using System.Text.Json;

namespace SoundBridge.Gui.ViewModels;

public sealed class AboutViewModel : ViewModelBase
{
    private readonly ProductMetadata _metadata = ProductMetadata.Load();

    public string AppName => _metadata.Name;

    public string Version => _metadata.DisplayVersion;

    public string Description => "Multi-output audio router for Windows.\nPlay audio on multiple devices simultaneously.";

    public string Developer => "Prateek Kumar Singh / prateek271";

    public string VirtualDriverCredit => "Virtual Audio Driver by MikeTheTech — MIT License";

    public string RuntimeCredit => ".NET 8 Runtime / WPF — Microsoft — MIT License";

    public string Copyright => "© 2026 Prateek Kumar Singh";

    private sealed class ProductMetadata
    {
        public string Name { get; private init; } = "SoundBridge";

        public string DisplayVersion { get; private init; } = "v X.X.X";

        public static ProductMetadata Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "app.meta.json");
            if (!File.Exists(path))
            {
                return new ProductMetadata();
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var product = document.RootElement.GetProperty("product");
                var name = product.GetProperty("name").GetString();
                var displayVersion = product.GetProperty("displayVersion").GetString();
                var version = product.GetProperty("version").GetString();

                return new ProductMetadata
                {
                    Name = string.IsNullOrWhiteSpace(name) ? "SoundBridge" : name,
                    DisplayVersion = !string.IsNullOrWhiteSpace(displayVersion)
                        ? displayVersion
                        : !string.IsNullOrWhiteSpace(version)
                            ? $"v {version}"
                            : "v X.X.X"
                };
            }
            catch
            {
                return new ProductMetadata();
            }
        }
    }
}
