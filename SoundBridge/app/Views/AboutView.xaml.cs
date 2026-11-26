using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace SoundBridge.Gui.Views;

public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
        LoadAppIcon();
    }

    private void LoadAppIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "assets", "icon.png");
        if (!File.Exists(iconPath))
        {
            return;
        }

        AppIconImage.Source = new BitmapImage(new Uri(iconPath, UriKind.Absolute));
    }
}
