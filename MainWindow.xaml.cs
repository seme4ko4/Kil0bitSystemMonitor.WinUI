using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Kil0bitSystemMonitor.Helpers;
using Kil0bitSystemMonitor.Models;
using Kil0bitSystemMonitor.Services;
using Kil0bitSystemMonitor.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Kil0bitSystemMonitor
{
    public sealed partial class MainWindow : Window
    {
        private readonly ConfigService _config = null!;
        private readonly DispatcherQueue _dispatcherQueue;

        public MainViewModel ViewModel { get; }

        /// <summary>Localization source for x:Bind (Mode=OneWay) — see Helpers/Loc.cs.</summary>
        public Loc L => Loc.I;

        public ObservableCollection<DiskSelectionItem> DiskItems { get; } = new();
        private readonly List<string> _diskSelectionOrder = new();
        private bool _isNavigating = false;

        public MainWindow(MainViewModel viewModel, ConfigService config)
        {
            _config = config;
            ViewModel = viewModel;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            InitializeComponent();

            // Populate static combo data before the binding engine reads the DataContext
            RefreshStaticCombos();

            // Language switcher combos (labels are the same in every language)
            var languages = new List<LabelValueOption> { new("Русский", "ru"), new("English", "en") };
            UiLanguageCombo.ItemsSource = languages;
            OverlayLanguageCombo.ItemsSource = new List<LabelValueOption>(languages);

            SettingsRoot.DataContext = viewModel;

            // Live UI language switch
            viewModel.Config.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Models.AppConfig.Language))
                {
                    Loc.I.Refresh();
                    Title = Loc.T("window.title");
                    RefreshStaticCombos();
                    ViewModel.RefreshLocalized();
                }
            };

            ConfigureWindowChrome();

            // Load heavy hardware lists in background to keep UI snappy
            _ = LoadHardwareDataAsync();
        }

        private void RefreshStaticCombos()
        {
            RefreshRateCombo.ItemsSource = new List<RefreshIntervalOption>
            {
                new(Loc.T("refresh.500"), 500),
                new(Loc.T("refresh.1000"), 1000),
                new(Loc.T("refresh.2000"), 2000),
                new(Loc.T("refresh.5000"), 5000),
            };
            RefreshRateCombo.SelectedValue = _config.Config.UpdateInterval;

            DisplayModeCombo.ItemsSource = new List<LabelValueOption>
            {
                new(Loc.T("mode.text"), "Text"),
                new(Loc.T("mode.compact"), "Compact"),
            };
            DisplayModeCombo.SelectedValue = _config.Config.DisplayStyle;
        }

        private void ConfigureWindowChrome()
        {
            Title = Loc.T("window.title");

            try
            {
                ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);
            }
            catch { }

            try
            {
                SystemBackdrop = new MicaBackdrop();
            }
            catch { }

            try
            {
                string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "icon.ico");
                if (!System.IO.File.Exists(iconPath))
                    iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "icon.png");
                if (System.IO.File.Exists(iconPath)) AppWindow.SetIcon(iconPath);

                AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000, 720));
            }
            catch { }
        }

        private async Task LoadHardwareDataAsync()
        {
            try
            {
                // Defer heavy WMI/PerfCounter calls to background thread
                var gpus = await Task.Run(() => TelemetryService.GetAvailableGpus());
                var disks = await Task.Run(() => TelemetryService.GetAvailableDisks());
                var adapters = await Task.Run(() => TelemetryService.GetAvailableNetworkAdapters());

                _dispatcherQueue.TryEnqueue(() =>
                {
                    PopulateGpuList(gpus);
                    PopulateDiskList(disks);
                    PopulateNetworkList(adapters);
                    EnsureValidSelections();
                });
            }
            catch { }
        }

        private void PopulateNetworkList(List<string> adapters)
        {
            try
            {
                var items = new List<LabelValueOption> { new(Loc.T("default.item"), "Default") };
                foreach (var adapter in adapters)
                {
                    items.Add(new LabelValueOption(adapter, adapter));
                }
                NetAdapterCombo.ItemsSource = items;
                SyncCombo(NetAdapterCombo, _config.Config.NetworkAdapter);
            }
            catch { }
        }

        private void PopulateGpuList(List<string> gpus)
        {
            try
            {
                var items = new List<LabelValueOption> { new(Loc.T("default.item"), "Default") };
                foreach (var gpu in gpus)
                {
                    items.Add(new LabelValueOption(gpu, gpu));
                }
                GpuAdapterCombo.ItemsSource = items;
                SyncCombo(GpuAdapterCombo, _config.Config.GpuAdapter);
            }
            catch { }
        }

        private static void SyncCombo(ComboBox comboBox, string? value)
        {
            int best = 0;
            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i] is LabelValueOption opt && string.Equals(opt.Value, value, StringComparison.OrdinalIgnoreCase))
                {
                    best = i;
                    break;
                }
            }
            comboBox.SelectedIndex = best;
        }

        private void PopulateDiskList(List<string> disks)
        {
            try
            {
                DiskItems.Clear();
                var selectedArray = (_config.Config.SelectedDisks ?? "All")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries);

                _diskSelectionOrder.Clear();
                if (!selectedArray.Contains("All") && !selectedArray.Contains("None"))
                    _diskSelectionOrder.AddRange(selectedArray);

                foreach (var disk in disks)
                {
                    if (disk == "_Total") continue;

                    var item = new DiskSelectionItem
                    {
                        Name = disk,
                        IsSelected = selectedArray.Contains(disk) || selectedArray.Contains("All")
                    };
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(DiskSelectionItem.IsSelected)) UpdateSelectedDisks(item);
                    };
                    DiskItems.Add(item);
                }
            }
            catch { }
        }

        private void UpdateSelectedDisks(DiskSelectionItem item)
        {
            if (item.IsSelected)
            {
                if (!_diskSelectionOrder.Contains(item.Name)) _diskSelectionOrder.Add(item.Name);
            }
            else
            {
                _diskSelectionOrder.Remove(item.Name);
            }

            if (_diskSelectionOrder.Count == 0) _config.Config.SelectedDisks = "None";
            else _config.Config.SelectedDisks = string.Join(";", _diskSelectionOrder);
        }

        private void EnsureValidSelections()
        {
            try
            {
                if (_config.Config.NetworkAdapter == "All") _config.Config.NetworkAdapter = "Default";
                if (_config.Config.GpuAdapter == "All") _config.Config.GpuAdapter = "Default";
                if (_config.Config.SelectedDisks == "Default") _config.Config.SelectedDisks = "All";
            }
            catch { }
        }

        private void SettingsRoot_Loaded(object sender, RoutedEventArgs e)
        {
            // Window chrome is configured in the constructor.
        }

        private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
        {
            var c = _config.Config;
            c.DisplayStyle = "Text";
            c.FontFamily = "Segoe UI";
            c.AccentColorHex = "#FFFFFF";
            c.LabelColorHex = "#00CCFF";
            c.BackgroundColorHex = "#B4141414";
            c.PodColorHex = "#0FFFFFFF";
            c.ScaleFactor = 1.0;
            c.ColumnSpacing = 6;
            c.IsTextBold = true;
            c.ShowPods = true;
            c.ShowBackground = false;
            c.NetLabelColorHex = null; c.CpuRamLabelColorHex = null; c.GpuLabelColorHex = null; c.DiskLabelColorHex = null;
            c.NetAccentColorHex = null; c.CpuRamAccentColorHex = null; c.GpuAccentColorHex = null; c.DiskAccentColorHex = null;
            _config.SaveConfig();
        }

        private async void ResetApp_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog resetDialog = new ContentDialog
            {
                Title = Loc.T("reset.title"),
                Content = Loc.T("reset.text"),
                PrimaryButtonText = Loc.T("reset.primary"),
                CloseButtonText = Loc.T("cancel"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await resetDialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            var c = _config.Config;
            c.ShowOverlay = true;
            c.LockPosition = false;
            c.LaunchOnStartup = false;
            c.HideOnFullscreen = true;
            c.StickToTaskbar = true;
            c.AlwaysOnTop = true;

            c.ShowCpu = true;
            c.ShowRam = true;
            c.ShowGpu = true;
            c.ShowTemp = true;
            c.ShowDisk = true;
            c.ShowDiskSpeed = true;
            c.ShowNetUp = true;
            c.ShowNetDown = true;

            c.NetworkAdapter = "Default";
            c.GpuAdapter = "Default";
            c.SelectedDisks = "All";

            c.DisplayStyle = "Text";
            c.FontFamily = "Segoe UI";
            c.AccentColorHex = "#FFFFFF";
            c.LabelColorHex = "#00CCFF";
            c.BackgroundColorHex = "#B4141414";
            c.PodColorHex = "#0FFFFFFF";
            c.ScaleFactor = 1.0;
            c.ColumnSpacing = 6;
            c.IsTextBold = true;
            c.ShowPods = true;
            c.ShowBackground = false;
            c.NetLabelColorHex = null; c.CpuRamLabelColorHex = null; c.GpuLabelColorHex = null; c.DiskLabelColorHex = null;
            c.NetAccentColorHex = null; c.CpuRamAccentColorHex = null; c.GpuAccentColorHex = null; c.DiskAccentColorHex = null;

            StartupService.SetStartup(false);
            _config.SaveConfig();
        }

        private async void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tag)
            {
                string currentHex = tag switch
                {
                    "Accent"       => _config.Config.AccentColorHex,
                    "Label"        => _config.Config.LabelColorHex,
                    "Background"   => _config.Config.BackgroundColorHex,
                    "Pod"          => _config.Config.PodColorHex,
                    "NetLabel"     => _config.Config.NetLabelColorHex    ?? _config.Config.LabelColorHex,
                    "CpuRamLabel"  => _config.Config.CpuRamLabelColorHex ?? _config.Config.LabelColorHex,
                    "GpuLabel"     => _config.Config.GpuLabelColorHex    ?? _config.Config.LabelColorHex,
                    "DiskLabel"    => _config.Config.DiskLabelColorHex   ?? _config.Config.LabelColorHex,
                    "NetAccent"    => _config.Config.NetAccentColorHex    ?? _config.Config.AccentColorHex,
                    "CpuRamAccent" => _config.Config.CpuRamAccentColorHex ?? _config.Config.AccentColorHex,
                    "GpuAccent"    => _config.Config.GpuAccentColorHex    ?? _config.Config.AccentColorHex,
                    "DiskAccent"   => _config.Config.DiskAccentColorHex   ?? _config.Config.AccentColorHex,
                    _              => "#FFFFFF"
                };

                bool allowAlpha = tag is "Background" or "Pod";
                string title = Loc.F("picker.title", ColorTagName(tag));
                string? hex = await PickColorAsync(title, currentHex, allowAlpha);
                if (hex == null) return;

                switch (tag)
                {
                    case "Accent":       _config.Config.AccentColorHex = hex; break;
                    case "Label":        _config.Config.LabelColorHex = hex; break;
                    case "Background":   _config.Config.BackgroundColorHex = hex; break;
                    case "Pod":          _config.Config.PodColorHex = hex; break;
                    case "NetLabel":     _config.Config.NetLabelColorHex = hex; break;
                    case "CpuRamLabel":  _config.Config.CpuRamLabelColorHex = hex; break;
                    case "GpuLabel":     _config.Config.GpuLabelColorHex = hex; break;
                    case "DiskLabel":    _config.Config.DiskLabelColorHex = hex; break;
                    case "NetAccent":    _config.Config.NetAccentColorHex = hex; break;
                    case "CpuRamAccent": _config.Config.CpuRamAccentColorHex = hex; break;
                    case "GpuAccent":    _config.Config.GpuAccentColorHex = hex; break;
                    case "DiskAccent":   _config.Config.DiskAccentColorHex = hex; break;
                }
            }
        }

        private static string ColorTagName(string tag) => tag switch
        {
            "Accent"       => Loc.T("tag.accent"),
            "Label"        => Loc.T("tag.label"),
            "Background"   => Loc.T("tag.background"),
            "Pod"          => Loc.T("tag.pod"),
            "NetLabel"     => Loc.T("tag.netlabel"),
            "NetAccent"    => Loc.T("tag.netaccent"),
            "CpuRamLabel"  => Loc.T("tag.cpulabel"),
            "CpuRamAccent" => Loc.T("tag.cpuaccent"),
            "GpuLabel"     => Loc.T("tag.gpulabel"),
            "GpuAccent"    => Loc.T("tag.gpuaccent"),
            "DiskLabel"    => Loc.T("tag.disklabel"),
            "DiskAccent"   => Loc.T("tag.diskaccent"),
            _              => tag
        };

        /// <summary>
        /// Lightweight in-app color picker dialog (RGB + optional alpha + hex input).
        /// Returns the selected "#AARRGGBB"/"#RRGGBB" string, or null when cancelled.
        /// </summary>
        private async Task<string?> PickColorAsync(string title, string currentHex, bool allowAlpha)
        {
            Color start = HexToBrushConverter.ParseHex(currentHex);
            if (!allowAlpha) start.A = 255;

            bool updating = false;

            var preview = new Border
            {
                Height = 44,
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255))
            };

            var alphaSlider = new Slider { Header = Loc.T("picker.alpha"), Minimum = 0, Maximum = 255, StepFrequency = 1, Value = start.A };
            var redSlider   = new Slider { Header = Loc.T("picker.red"), Minimum = 0, Maximum = 255, StepFrequency = 1, Value = start.R };
            var greenSlider = new Slider { Header = Loc.T("picker.green"), Minimum = 0, Maximum = 255, StepFrequency = 1, Value = start.G };
            var blueSlider  = new Slider { Header = Loc.T("picker.blue"), Minimum = 0, Maximum = 255, StepFrequency = 1, Value = start.B };

            var hexBox = new TextBox { Header = Loc.T("picker.hex"), Text = FormatHex(start, allowAlpha) };

            void SyncFromSliders()
            {
                if (updating) return;
                updating = true;
                try
                {
                    var c = Color.FromArgb(
                        allowAlpha ? (byte)Math.Clamp((int)alphaSlider.Value, 0, 255) : (byte)255,
                        (byte)Math.Clamp((int)redSlider.Value, 0, 255),
                        (byte)Math.Clamp((int)greenSlider.Value, 0, 255),
                        (byte)Math.Clamp((int)blueSlider.Value, 0, 255));
                    preview.Background = new SolidColorBrush(c);
                    hexBox.Text = FormatHex(c, allowAlpha);
                }
                finally { updating = false; }
            }

            alphaSlider.ValueChanged += (s, e) => SyncFromSliders();
            redSlider.ValueChanged += (s, e) => SyncFromSliders();
            greenSlider.ValueChanged += (s, e) => SyncFromSliders();
            blueSlider.ValueChanged += (s, e) => SyncFromSliders();

            hexBox.TextChanged += (s, e) =>
            {
                if (updating) return;
                try
                {
                    var c = HexToBrushConverter.ParseHex(hexBox.Text);
                    updating = true;
                    try
                    {
                        alphaSlider.Value = c.A; redSlider.Value = c.R; greenSlider.Value = c.G; blueSlider.Value = c.B;
                        preview.Background = new SolidColorBrush(c);
                    }
                    finally { updating = false; }
                }
                catch { }
            };

            var panel = new StackPanel { Spacing = 12, MinWidth = 360 };
            panel.Children.Add(preview);
            if (allowAlpha) panel.Children.Add(alphaSlider);
            panel.Children.Add(redSlider);
            panel.Children.Add(greenSlider);
            panel.Children.Add(blueSlider);
            panel.Children.Add(hexBox);

            var dialog = new ContentDialog
            {
                Title = title,
                Content = panel,
                PrimaryButtonText = Loc.T("ok"),
                CloseButtonText = Loc.T("cancel"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            SyncFromSliders();
            ContentDialogResult result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? hexBox.Text : null;
        }

        private static string FormatHex(Color c, bool withAlpha)
            => withAlpha ? $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private void ClearSectionColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tag)
            {
                switch (tag)
                {
                    case "NetLabel":     _config.Config.NetLabelColorHex = null; break;
                    case "CpuRamLabel":  _config.Config.CpuRamLabelColorHex = null; break;
                    case "GpuLabel":     _config.Config.GpuLabelColorHex = null; break;
                    case "DiskLabel":    _config.Config.DiskLabelColorHex = null; break;
                    case "NetAccent":    _config.Config.NetAccentColorHex = null; break;
                    case "CpuRamAccent": _config.Config.CpuRamAccentColorHex = null; break;
                    case "GpuAccent":    _config.Config.GpuAccentColorHex = null; break;
                    case "DiskAccent":   _config.Config.DiskAccentColorHex = null; break;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            StartupService.SetStartup(_config.Config.LaunchOnStartup);
            _config.SaveConfig();
            this.Close();
        }

        private void HomeCard_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string tag)
            {
                SelectSection(tag);
            }
        }

        private void SettingsNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                SelectSection(item.Tag?.ToString() ?? string.Empty);
            }
        }

        public void SelectSection(string sectionName)
        {
            Kil0bitSystemMonitor.Helpers.Diag.Log($"SelectSection('{sectionName}')");
            if (string.IsNullOrEmpty(sectionName) || _isNavigating) return;

            _isNavigating = true;
            try
            {
                HomeSection.Visibility = Visibility.Collapsed;
                GeneralSection.Visibility = Visibility.Collapsed;
                MonitoringSection.Visibility = Visibility.Collapsed;
                AppearanceSection.Visibility = Visibility.Collapsed;
                AboutSection.Visibility = Visibility.Collapsed;

                switch (sectionName)
                {
                    case "Home": HomeSection.Visibility = Visibility.Visible; break;
                    case "General": GeneralSection.Visibility = Visibility.Visible; break;
                    case "Monitoring": MonitoringSection.Visibility = Visibility.Visible; break;
                    case "Appearance": AppearanceSection.Visibility = Visibility.Visible; break;
                    case "About": AboutSection.Visibility = Visibility.Visible; break;
                }

                // Sync Nav selection safely
                foreach (var menuItem in SettingsNav.MenuItems)
                {
                    if (menuItem is NavigationViewItem item && string.Equals(item.Tag?.ToString(), sectionName, StringComparison.Ordinal))
                    {
                        if (!ReferenceEquals(SettingsNav.SelectedItem, item))
                            SettingsNav.SelectedItem = item;
                        break;
                    }
                }
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            App.Quit();
        }
    }

    public record RefreshIntervalOption(string Label, int Value);

    public record LabelValueOption(string Label, string Value);
}
