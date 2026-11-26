using System.Windows;
using System.Windows.Threading;
using System.IO;
using SoundBridge.Gui.Services;

namespace SoundBridge.Gui;

public partial class App : Application
{
    private const string InstanceMutexName = "Local\\SoundBridge.App.SingleInstance";
    private const string ActivateEventName = "Local\\SoundBridge.App.Activate";
    private Mutex? _instanceMutex;
    private EventWaitHandle? _activateEvent;
    private Thread? _activationThread;
    private bool _ownsInstance;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            LogCrash("startup-begin", null);
            _instanceMutex = new Mutex(initiallyOwned: true, InstanceMutexName, out _ownsInstance);
            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);

            if (!_ownsInstance)
            {
                _activateEvent.Set();
                Shutdown();
                return;
            }

            base.OnStartup(e);

            var routerService = CreateRouterService();
            var settingsStore = new JsonAppSettingsStore();
            var window = new MainWindow
            {
                DataContext = new ViewModels.MainViewModel(routerService, new JsonTemplateStore(), settingsStore)
            };

            MainWindow = window;
            window.Show();
            StartActivationListener();
            LogCrash("startup-complete", null);
        }
        catch (Exception ex)
        {
            LogCrash("startup-failed", ex);
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activateEvent?.Set();
        _activateEvent?.Dispose();

        if (_ownsInstance)
        {
            _instanceMutex?.ReleaseMutex();
        }

        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void StartActivationListener()
    {
        if (_activateEvent is null)
        {
            return;
        }

        _activationThread = new Thread(() =>
        {
            while (_activateEvent.WaitOne())
            {
                if (Dispatcher.HasShutdownStarted)
                {
                    return;
                }

                Dispatcher.Invoke(() =>
                {
                    if (MainWindow is null)
                    {
                        return;
                    }

                    MainWindow.Show();
                    MainWindow.WindowState = WindowState.Normal;
                    MainWindow.Activate();
                });
            }
        })
        {
            IsBackground = true,
            Name = "SoundBridge activation listener"
        };
        _activationThread.Start();
    }

    private static IAudioRouterService CreateRouterService()
    {
        if (string.Equals(
            Environment.GetEnvironmentVariable("SOUNDBRIDGE_USE_MOCK"),
            "1",
            StringComparison.Ordinal))
        {
            return new MockAudioRouterService();
        }

        try
        {
            return new NativeAudioRouterService();
        }
        catch
        {
            return new MockAudioRouterService();
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash("dispatcher-unhandled", e.Exception);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogCrash("domain-unhandled", e.ExceptionObject as Exception);
    }

    private static void LogCrash(string marker, Exception? exception)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SoundBridge",
                "logs");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "crash.log");
            var message = exception is null ? string.Empty : Environment.NewLine + exception;
            File.AppendAllText(path, $"[{DateTimeOffset.Now:O}] {marker}{message}{Environment.NewLine}");
        }
        catch
        {
            // Last-resort crash logging must never create a second crash.
        }
    }
}
