using System;
using System.Runtime.InteropServices;
using Kil0bitSystemMonitor.Helpers;
using Kil0bitSystemMonitor.Services;
using Kil0bitSystemMonitor.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace Kil0bitSystemMonitor
{
    public partial class App : Application
    {
        public App()
        {
            _instance = this;

            // Must be set before resources/windows are initialized.
            // OnExplicitShutdown replicates the WPF dummy-window trick: closing the
            // settings window must NOT terminate the process — the taskbar overlay
            // lives in this process and keeps running with zero open windows.
            try { this.DispatcherShutdownMode = DispatcherShutdownMode.OnExplicitShutdown; }
            catch (Exception ex) { Helpers.Diag.Log("DispatcherShutdownMode", ex); }

            try { this.RequestedTheme = ApplicationTheme.Dark; } catch { }
            InitializeComponent();
            Kil0bitSystemMonitor.Helpers.Win32Helper.SetCurrentProcessExplicitAppUserModelID("Kil0bit.SystemMonitor.Main.v3");
        }

        private static App? _instance;

        private System.Threading.Mutex? _mutex;
        private OverlayWindow? _overlay;
        private TelemetryService? _telemetry;
        private readonly MainViewModel _viewModel = new();
        private readonly ConfigService _config = new();

        public static MainWindow? SettingsWindow { get; private set; }
        public static DispatcherQueue? UiDispatcher { get; private set; }

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Helpers.Diag.Log("OnLaunched begin");
            UnhandledException += (s, e) =>
            {
                Helpers.Diag.Log("App.UnhandledException", e.Exception);
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Helpers.Diag.Log("Domain.UnhandledException", e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString()));

            try
            {
                Helpers.Diag.Log("theme check (set in ctor)");
            }
            catch (Exception ex) { Helpers.Diag.Log("RequestedTheme", ex); }

            // Robust single-instance check using Mutex
            bool createdNew;
            _mutex = new System.Threading.Mutex(true, "Local\\Kil0bitSystemMonitor_SingleInstance_Mutex", out createdNew);
            Helpers.Diag.Log($"mutex createdNew={createdNew}");

            if (!createdNew)
            {
                // Signal the running instance to show its settings window
                // (named event — more reliable than FindWindow across processes)
                try
                {
                    using var evt = System.Threading.EventWaitHandle.OpenExisting("Local\\Kil0bitSystemMonitor_ShowSettings");
                    evt.Set();
                }
                catch (Exception ex) { Helpers.Diag.Log("signal existing instance", ex); }
                _mutex.Dispose();
                Environment.Exit(0);
                return;
            }

            UiDispatcher = DispatcherQueue.GetForCurrentThread();
            Helpers.Diag.Log("dispatcher ok");

            // Listen for "show settings" requests from second instances
            var showSettingsEvent = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.AutoReset, "Local\\Kil0bitSystemMonitor_ShowSettings");
            var listener = new System.Threading.Thread(() =>
            {
                while (showSettingsEvent.WaitOne())
                {
                    UiDispatcher?.TryEnqueue(() => OpenSettings(_viewModel, _config));
                }
            });
            listener.IsBackground = true;
            listener.Start();

            // Shared converters (registered in code so the XAML markup compiler
            // doesn't need to resolve local project types on first build)
            Resources["BoolToVis"] = new Helpers.BoolToVisibilityConverter();
            Resources["HexToBrush"] = new Helpers.HexToBrushConverter();
            Helpers.Diag.Log("converters registered");

            _viewModel.Config = _config.Config;
            Helpers.Diag.Log("config loaded");

            // Localization sources (UI language / overlay language)
            Loc.Init(() => _config.Config.Language, () => _config.Config.OverlayLanguage);

            try
            {
                _telemetry = new TelemetryService(_config);
                Helpers.Diag.Log("telemetry created");
            }
            catch (Exception ex)
            {
                Helpers.Diag.Log("TelemetryService ctor", ex);
                throw;
            }

            try
            {
                _overlay = new OverlayWindow(_viewModel, _config, _telemetry);
                Helpers.Diag.Log("overlay created");
            }
            catch (Exception ex)
            {
                Helpers.Diag.Log("OverlayWindow ctor", ex);
                throw;
            }

            string[] cmdArgs = Environment.GetCommandLineArgs();
            bool isStartup = Array.IndexOf(cmdArgs, "--startup") >= 0;
            if (!isStartup)
            {
                OpenSettings(_viewModel, _config);
            }
            Helpers.Diag.Log("OnLaunched end");
        }

        public static void OpenSettings(MainViewModel viewModel, ConfigService config)
        {
            if (SettingsWindow != null)
            {
                SettingsWindow.Activate();
                try { SettingsWindow.AppWindow.MoveInZOrderAtTop(); } catch { }
                try { SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(SettingsWindow)); } catch { }
                return;
            }

            SettingsWindow = new MainWindow(viewModel, config);
            SettingsWindow.Closed += (s, e) => { SettingsWindow = null; };
            SettingsWindow.Activate();
        }

        public static void Quit()
        {
            void Shutdown()
            {
                try { SettingsWindow?.Close(); } catch { }
                try { _instance?._overlay?.Dispose(); } catch { }
                try { _instance?._telemetry?.Dispose(); } catch { }
                Environment.Exit(0);
            }

            var d = UiDispatcher;
            if (d == null) { Environment.Exit(0); return; }
            if (d.HasThreadAccess) { Shutdown(); }
            else if (!d.TryEnqueue(() => Shutdown())) { Environment.Exit(0); }
        }
    }
}
