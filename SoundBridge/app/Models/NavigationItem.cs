using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SoundBridge.Gui.Models;

public sealed class NavigationItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private string _description = string.Empty;
    private string _iconGlyph = string.Empty;
    private string _subInfo = string.Empty;
    private object? _page;
    private bool _isSelected;
    private bool _isActiveDevice;
    private bool _isEnabled = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string IconGlyph
    {
        get => _iconGlyph;
        set => SetProperty(ref _iconGlyph, value);
    }

    public string SubInfo
    {
        get => _subInfo;
        set => SetProperty(ref _subInfo, value);
    }

    public object? Page
    {
        get => _page;
        set => SetProperty(ref _page, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public bool IsActiveDevice
    {
        get => _isActiveDevice;
        set => SetProperty(ref _isActiveDevice, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
