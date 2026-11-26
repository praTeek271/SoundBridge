using System.Collections.ObjectModel;
using System.Windows;
using SoundBridge.Gui.Models;
using SoundBridge.Gui.Services;

namespace SoundBridge.Gui.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly ITemplateStore _templateStore;
    private readonly IAppSettingsStore _settingsStore;
    private bool _isRestoringTemplates;
    private NavigationItem? _selectedNavigationItem;
    private NavigationItem? _aboutNavigationItem;
    private object? _selectedPage;
    private bool _isSidebarCollapsed;
    private bool _isDevicesExpanded = true;
    private bool _isDeviceCreateMenuOpen;
    private int _multiOutputDeviceCount;
    private int _aggregateDeviceCount;

    public MainViewModel()
        : this(new MockAudioRouterService(), new JsonTemplateStore(), new JsonAppSettingsStore())
    {
    }

    public MainViewModel(IAudioRouterService routerService)
        : this(routerService, new JsonTemplateStore(), new JsonAppSettingsStore())
    {
    }

    public MainViewModel(IAudioRouterService routerService, ITemplateStore templateStore)
        : this(routerService, templateStore, new JsonAppSettingsStore())
    {
    }

    public MainViewModel(
        IAudioRouterService routerService,
        ITemplateStore templateStore,
        IAppSettingsStore settingsStore)
    {
        RouterService = routerService;
        _templateStore = templateStore;
        _settingsStore = settingsStore;

        Devices = new DevicesViewModel(RouterService);
        Calibration = new CalibrationViewModel(RouterService);
        Calibration.RoutingStateChanged += isActive =>
        {
            Devices.SetActiveTemplate(isActive ? Calibration.ActiveDevice : null);
            Devices.SetRoutingActive(isActive);
        };
        Settings = new SettingsViewModel(RouterService, _settingsStore);
        About = new AboutViewModel();

        DevicesNavigationItem = new NavigationItem
        {
            Title = "Devices",
            Description = "Live routing status and output overview.",
            IconGlyph = "\uE7F5",
            Page = Devices
        };

        CalibrationNavigationItem = new NavigationItem
        {
            Title = "Calibration",
            Description = "Latency sync and beep verification.",
            IconGlyph = "\uE9CA",
            Page = Calibration,
            IsEnabled = false
        };
        Devices.OpenCalibrationRequested += () =>
        {
            if (CalibrationNavigationItem.IsEnabled)
            {
                SelectedNavigationItem = CalibrationNavigationItem;
            }
        };

            SettingsNavigationItem = new NavigationItem
        {
            Title = "Settings",
            Description = "Startup, tray, and config.",
            IconGlyph = "\uE713",
            Page = Settings
        };

        NavigationItems = new ObservableCollection<NavigationItem>
        {
            DevicesNavigationItem,
            CalibrationNavigationItem,
            SettingsNavigationItem
        };

        CreatedDeviceItems = new ObservableCollection<NavigationItem>();

        AboutNavigationItem = new NavigationItem
        {
            Title = "About",
            Description = "Version, credits, and references.",
            IconGlyph = "\uE946",
            Page = About
        };

        SelectNavigationCommand = new RelayCommand(parameter =>
        {
            if (parameter is NavigationItem { IsEnabled: true } item)
            {
                SelectedNavigationItem = item;
            }
        });
        SelectAboutCommand = new RelayCommand(() => SelectedNavigationItem = AboutNavigationItem);
        ToggleDevicesExpandedCommand = new RelayCommand(() =>
        {
            SelectedNavigationItem = DevicesNavigationItem;

            if (CreatedDeviceItems.Count > 0)
            {
                IsDevicesExpanded = true;
                return;
            }

            IsDevicesExpanded = !IsDevicesExpanded;
        });
        ToggleDeviceCreateMenuCommand = new RelayCommand(() => IsDeviceCreateMenuOpen = !IsDeviceCreateMenuOpen);
        CreateMultiOutputDeviceCommand = new RelayCommand(() => CreateDevicePage("multi-output device", "Multi-output"));
        CreateAggregateDeviceCommand = new RelayCommand(
            _ => { },
            _ => false);

        RestorePersistedTemplates();
        SelectedNavigationItem ??= DevicesNavigationItem;
    }

    public IAudioRouterService RouterService { get; }

    public ObservableCollection<NavigationItem> NavigationItems { get; }

    public ObservableCollection<NavigationItem> CreatedDeviceItems { get; }

    public NavigationItem DevicesNavigationItem { get; }

    public NavigationItem CalibrationNavigationItem { get; }

    public NavigationItem SettingsNavigationItem { get; }

    public DevicesViewModel Devices { get; }

    public CalibrationViewModel Calibration { get; }

    public SettingsViewModel Settings { get; }

    public AboutViewModel About { get; }

    public RelayCommand SelectAboutCommand { get; }

    public RelayCommand SelectNavigationCommand { get; }

    public RelayCommand ToggleDevicesExpandedCommand { get; }

    public RelayCommand ToggleDeviceCreateMenuCommand { get; }

    public RelayCommand CreateMultiOutputDeviceCommand { get; }

    public RelayCommand CreateAggregateDeviceCommand { get; }

    public NavigationItem? AboutNavigationItem
    {
        get => _aboutNavigationItem;
        private set => SetProperty(ref _aboutNavigationItem, value);
    }

    public bool IsSidebarCollapsed
    {
        get => _isSidebarCollapsed;
        set => SetProperty(ref _isSidebarCollapsed, value);
    }

    public bool IsDevicesExpanded
    {
        get => _isDevicesExpanded;
        set
        {
            var nextValue = value || CreatedDeviceItems.Count > 0;
            SetProperty(ref _isDevicesExpanded, nextValue);
        }
    }

    public bool IsDeviceCreateMenuOpen
    {
        get => _isDeviceCreateMenuOpen;
        set => SetProperty(ref _isDeviceCreateMenuOpen, value);
    }

    public object? SelectedPage
    {
        get => _selectedPage;
        private set => SetProperty(ref _selectedPage, value);
    }

    public NavigationItem? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (SetProperty(ref _selectedNavigationItem, value))
            {
                foreach (var item in NavigationItems)
                {
                    item.IsSelected = ReferenceEquals(item, value);
                }

                foreach (var item in CreatedDeviceItems)
                {
                    item.IsSelected = ReferenceEquals(item, value);
                }

                if (AboutNavigationItem is not null)
                {
                    AboutNavigationItem.IsSelected = ReferenceEquals(AboutNavigationItem, value);
                }

                RaisePropertyChanged(nameof(NavigationItems));

                SelectedPage = value?.Page;

                Devices.RefreshLiveStatus();

                if (ReferenceEquals(value, DevicesNavigationItem))
                {
                    Devices.ResetConsoleExpansionForNavigation();
                }
            }
        }
    }

    private void CreateDevicePage(string titlePrefix, string deviceType)
    {
        var index = deviceType == "Multi-output" ? ++_multiOutputDeviceCount : ++_aggregateDeviceCount;
        var title = $"{titlePrefix} {index}";
        var item = AddDevicePage(title, deviceType);

        CreatedDeviceItems.Add(item);
        IsDevicesExpanded = true;
        IsDeviceCreateMenuOpen = false;
        SelectedNavigationItem = item;
        SaveTemplates($"created {title}");
    }

    private NavigationItem AddDevicePage(string title, string deviceType, string? templateId = null)
    {
        var page = new DeviceWorkspaceViewModel(RouterService, title, deviceType, templateId, ActivateDevicePage);

        var item = new NavigationItem
        {
            Title = title,
            Description = $"{deviceType} device routing workspace.",
            IconGlyph = deviceType == "Multi-output" ? "\uE7F6" : "\uE9D2",
            Page = page,
            SubInfo = page.RoutingSummary
        };

        page.RoutingChanged += updatedPage =>
        {
            item.SubInfo = updatedPage.RoutingSummary;

            if (updatedPage.IsActive && string.Equals(updatedPage.DeviceType, "Multi-output", StringComparison.Ordinal))
            {
                Calibration.SetActiveDevice(updatedPage);
            }

            SaveTemplates($"updated {updatedPage.Name}");
        };
        page.RoutingUpdateRequested += updatedPage =>
        {
            if (ReferenceEquals(Calibration.ActiveDevice, updatedPage))
            {
                Calibration.MarkRoutingUpdatePending();
                SelectedNavigationItem = CalibrationNavigationItem;
            }
        };

        page.DeleteRequested += deletePage =>
        {
            DeleteDevicePage(deletePage);
        };

        return item;
    }

    private void ActivateDevicePage(DeviceWorkspaceViewModel activePage)
    {
        var shouldActivate = !activePage.IsActive;

        foreach (var item in CreatedDeviceItems)
        {
            if (item.Page is DeviceWorkspaceViewModel page)
            {
                var isActive = shouldActivate && ReferenceEquals(page, activePage);
                page.IsActive = isActive;
                item.IsActiveDevice = isActive;
            }
        }

        var isActiveMultiOutput = shouldActivate && string.Equals(activePage.DeviceType, "Multi-output", StringComparison.Ordinal);
        CalibrationNavigationItem.IsEnabled = isActiveMultiOutput;

        if (isActiveMultiOutput)
        {
            Calibration.SetActiveDevice(activePage);
            Devices.SetActiveTemplate(activePage);
            SaveTemplates($"selected {activePage.Name}");
            SelectedNavigationItem = CalibrationNavigationItem;
            return;
        }

        activePage.StopRouting();
        Calibration.ClearActiveDevice();
        Devices.SetActiveTemplate(null);

        if (ReferenceEquals(SelectedNavigationItem, CalibrationNavigationItem))
        {
            SelectedNavigationItem = DevicesNavigationItem;
        }

        SaveTemplates($"deselected {activePage.Name}");
    }

    public void DeleteDevicePage(DeviceWorkspaceViewModel page)
    {
        var confirmMessage = page.IsRoutingActive
            ? $"'{page.Name}' is currently active and routing audio. Deleting it will stop routing.\n\nAre you sure you want to delete this template?"
            : $"Are you sure you want to delete '{page.Name}'?";

        var result = MessageBox.Show(
            confirmMessage,
            "Delete Template - SoundBridge",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (page.IsActive)
        {
            page.StopRouting();
            Calibration.ClearActiveDevice();
            Devices.SetActiveTemplate(null);
            CalibrationNavigationItem.IsEnabled = false;
        }

        var navItem = CreatedDeviceItems.FirstOrDefault(item => ReferenceEquals(item.Page, page));
        if (navItem is not null)
        {
            CreatedDeviceItems.Remove(navItem);
        }

        if (ReferenceEquals(SelectedPage, page) || ReferenceEquals(SelectedNavigationItem, navItem))
        {
            SelectedNavigationItem = DevicesNavigationItem;
        }

        if (CreatedDeviceItems.Count == 0)
        {
            IsDevicesExpanded = false;
        }

        SaveTemplates($"deleted {page.Name}");
    }

    private void RestorePersistedTemplates()
    {
        _isRestoringTemplates = true;
        try
        {
            var document = _templateStore.Load();
            NavigationItem? selectedItem = null;

            foreach (var template in document.Templates)
            {
                TrackDeviceCounter(template.Name, template.Type);

                var item = AddDevicePage(template.Name, template.Type, template.Id);
                if (item.Page is DeviceWorkspaceViewModel page)
                {
                    page.RestoreFrom(template);
                    page.IsActive = template.IsSelected;
                    item.IsActiveDevice = template.IsSelected;

                    if (template.IsSelected
                        && string.Equals(template.Type, "Multi-output", StringComparison.Ordinal))
                    {
                        CalibrationNavigationItem.IsEnabled = true;
                        Calibration.SetActiveDevice(page);
                        Devices.SetActiveTemplate(page);
                        selectedItem = item;
                    }
                }

                CreatedDeviceItems.Add(item);
            }

            if (CreatedDeviceItems.Count > 0)
            {
                IsDevicesExpanded = true;
            }

            if (selectedItem is not null)
            {
                SelectedNavigationItem = selectedItem;
            }
        }
        finally
        {
            _isRestoringTemplates = false;
        }
    }

    private void SaveTemplates(string reason)
    {
        if (_isRestoringTemplates)
        {
            return;
        }

        var document = new PersistedTemplateDocument
        {
            SchemaVersion = 1,
            Templates = CreatedDeviceItems
                .Select(item => item.Page as DeviceWorkspaceViewModel)
                .Where(page => page is not null)
                .Select(page => page!.ToPersistedTemplate(page.IsActive))
                .ToList()
        };

        try
        {
            _templateStore.Save(document, reason);
        }
        catch
        {
            // Persistence failures should not crash the visual layer.
            // JsonTemplateStore records the detailed failure in logs/app.log when possible.
        }
    }

    private void TrackDeviceCounter(string name, string deviceType)
    {
        var number = ExtractTrailingNumber(name);
        if (number <= 0)
        {
            return;
        }

        if (string.Equals(deviceType, "Multi-output", StringComparison.Ordinal))
        {
            _multiOutputDeviceCount = Math.Max(_multiOutputDeviceCount, number);
            return;
        }

        if (string.Equals(deviceType, "Aggregate", StringComparison.Ordinal))
        {
            _aggregateDeviceCount = Math.Max(_aggregateDeviceCount, number);
        }
    }

    private static int ExtractTrailingNumber(string value)
    {
        var index = value.Length - 1;
        while (index >= 0 && char.IsDigit(value[index]))
        {
            index--;
        }

        return int.TryParse(value[(index + 1)..], out var number) ? number : 0;
    }
}
