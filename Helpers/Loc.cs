using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kil0bitSystemMonitor.Helpers
{
    /// <summary>
    /// Central localization dictionary (RU/EN).
    /// Loc.T  — strings for the settings UI (Config.Language)
    /// Loc.O  — strings for the taskbar overlay (Config.OverlayLanguage)
    /// XAML binds to instance properties (NavHome, ShowOverlay, ...) with Mode=OneWay;
    /// a language switch raises a "reset" PropertyChanged and every binding re-evaluates live.
    /// </summary>
    public sealed class Loc : INotifyPropertyChanged
    {
        public static Loc I { get; } = new();

        private static Func<string>? _uiLang;
        private static Func<string>? _ovlLang;

        private Loc() { }

        public static void Init(Func<string> uiLang, Func<string> ovlLang)
        {
            _uiLang = uiLang;
            _ovlLang = ovlLang;
            I.Refresh();
        }

        /// <summary>String for the settings UI in the current interface language.</summary>
        public static string T(string key) => Get(_uiLang?.Invoke() ?? "ru", key);

        /// <summary>Formatted string for the settings UI.</summary>
        public static string F(string key, object arg) => string.Format(T(key), arg);

        /// <summary>String for the overlay (menu, metric labels, units) in the overlay language.</summary>
        public static string O(string key) => Get(_ovlLang?.Invoke() ?? "ru", key);

        private static string Get(string lang, string key)
        {
            if (s_map.TryGetValue(key, out var pair))
                return lang == "en" ? pair.En : pair.Ru;
            return key;
        }

        /// <summary>Re-evaluates every x:Bind localization binding (live language switch).</summary>
        public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

        public event PropertyChangedEventHandler? PropertyChanged;

        private static readonly Dictionary<string, (string Ru, string En)> s_map = new()
        {
            // Window / navigation
            ["window.title"]   = ("Kil0bit System Monitor — Настройки", "Kil0bit System Monitor — Settings"),
            ["nav.home"]       = ("Главная", "Home"),
            ["nav.general"]    = ("Общие", "General"),
            ["nav.monitoring"] = ("Мониторинг", "Monitoring"),
            ["nav.appearance"] = ("Внешний вид", "Appearance"),
            ["nav.about"]      = ("О программе", "About"),

            // Home
            ["home.welcome"]    = ("Добро пожаловать", "Welcome"),
            ["home.subtitle"]   = ("Kil0bit System Monitor активен и отслеживает производительность вашего оборудования почти без нагрузки на систему.",
                                   "Kil0bit System Monitor is active and tracking your hardware performance with near-zero overhead."),
            ["card.general"]    = ("Общие", "General"),
            ["card.general.d"]  = ("Автозапуск и основные настройки системы.", "Startup behavior and core system settings."),
            ["card.monitoring"] = ("Мониторинг", "Monitoring"),
            ["card.monitoring.d"] = ("Выбор датчиков ЦП, ГП и сети.", "Select CPU, GPU, and network sensors."),
            ["card.appearance"] = ("Внешний вид", "Appearance"),
            ["card.appearance.d"] = ("Настройка цветов и стиля шрифта.", "Customize colors and font style."),
            ["card.about"]      = ("О программе", "About"),
            ["card.about.d"]    = ("Версия, документация и ссылки для поддержки.", "Version info, documentation, and support links."),
            ["note.title"]      = ("Примечание о производительности", "Performance Note"),
            ["note.text"]       = ("Система отслеживается через низкоуровневые Win32 API для максимальной точности.",
                                   "Your system is monitored via low-level Win32 APIs for maximum precision."),

            // General
            ["sec.general"]       = ("Общие", "General"),
            ["show.overlay"]      = ("Показывать оверлей", "Show Overlay"),
            ["show.overlay.d"]    = ("Включение и отключение монитора.", "Enable or disable the monitor."),
            ["lock.position"]     = ("Блокировка позиции", "Lock Position"),
            ["lock.position.d"]   = ("Защита от случайного перетаскивания.", "Prevent accidental dragging."),
            ["run.startup"]       = ("Запуск при входе в систему", "Run at Startup"),
            ["run.startup.d"]     = ("Запускать вместе с Windows.", "Launch with Windows."),
            ["hide.fullscreen"]   = ("Скрывать в полноэкранном режиме", "Hide in Fullscreen"),
            ["hide.fullscreen.d"] = ("Скрывать во время игр и видео.", "Hide during games and videos."),
            ["snap.taskbar"]      = ("Привязать к панели задач", "Snap to Taskbar"),
            ["snap.taskbar.d"]    = ("Закрепить в области панели задач.", "Dock to the taskbar area."),
            ["keep.on.top"]       = ("Поверх всех окон", "Keep on Top"),
            ["keep.on.top.d"]     = ("Всегда располагаться выше других окон.", "Stay above other windows."),
            ["refresh.rate"]      = ("Частота обновления", "Refresh Rate"),
            ["refresh.rate.d"]    = ("Как часто монитор обновляет показатели.", "How often the monitor updates its metrics."),
            ["refresh.500"]       = ("500 мс (максимальная точность)", "500ms (High Performance)"),
            ["refresh.1000"]      = ("1000 мс (по умолчанию)", "1000ms (Default)"),
            ["refresh.2000"]      = ("2000 мс (реже)", "2000ms (Relaxed)"),
            ["refresh.5000"]      = ("5000 мс (экономия заряда)", "5000ms (Power Saver)"),
            ["toggle.on"]         = ("Вкл.", "On"),
            ["toggle.off"]        = ("Откл.", "Off"),
            ["language.card"]     = ("Язык", "Language"),
            ["ui.language"]       = ("Язык интерфейса", "Interface language"),
            ["ovl.language"]      = ("Язык оверлея", "Overlay language"),

            // Monitoring
            ["sec.monitoring"] = ("Мониторинг", "Monitoring"),
            ["telemetry.header"] = ("Аппаратная телеметрия", "Hardware Telemetry"),
            ["group.cpu"]   = ("Процессор и память", "Processor & Memory"),
            ["cpu.usage"]   = ("Загрузка ЦП", "CPU Usage"),
            ["ram.usage"]   = ("Загрузка ОЗУ", "RAM Usage"),
            ["group.gpu"]   = ("Графика и температура", "Graphics & Thermals"),
            ["gpu.usage"]   = ("Загрузка ГП", "GPU Usage"),
            ["gpu.temp"]    = ("Температура ГП", "GPU Temperature"),
            ["group.net"]   = ("Сеть и подключения", "Data & Connectivity"),
            ["net.up"]      = ("Скорость отдачи", "Upload Speed"),
            ["net.down"]    = ("Скорость загрузки", "Download Speed"),
            ["group.disk"]  = ("Дисковая активность", "Storage Activity"),
            ["disk.space"]  = ("Занято места %", "Used Space %"),
            ["disk.activity"] = ("Активность в реальном времени", "Real-time Activity"),
            ["hw.selection"] = ("Выбор оборудования", "Hardware Selection"),
            ["net.adapter"]  = ("Сетевой адаптер", "Network Adapter"),
            ["gpu.card"]     = ("Видеокарта", "Graphics Card"),
            ["drives"]       = ("Накопители", "Storage Drives"),
            ["default.item"] = ("По умолчанию", "Default"),

            // Appearance
            ["sec.appearance"] = ("Внешний вид", "Appearance"),
            ["typography"]     = ("Типографика", "Typography"),
            ["font.label"]     = ("Шрифт", "Font Family"),
            ["display.mode"]   = ("Режим отображения", "Display Mode"),
            ["mode.text"]      = ("Текст", "Text"),
            ["mode.compact"]   = ("Компактный", "Compact"),
            ["bold.text"]      = ("Жирный текст", "Bold Text"),
            ["palette"]        = ("Цветовая палитра", "Color Palette"),
            ["accent.color"]   = ("Цвет значений", "Metric Accent"),
            ["select.accent"]  = ("Выбрать цвет значений", "Select accent color"),
            ["label.color"]    = ("Цвет подписей", "Label Tone"),
            ["select.label"]   = ("Выбрать цвет подписей", "Select label color"),
            ["pod.color"]      = ("Цвет капсул", "Capsule Color"),
            ["select.pod"]     = ("Выбрать цвет капсул", "Select capsule color"),
            ["scaling.card"]   = ("Масштабирование", "Layout"),
            ["scaling.label"]  = ("Масштаб (размер)", "Scaling (Size)"),
            ["spacing.label"]  = ("Интервал между столбцами", "Column Spacing"),
            ["capsules.toggle"] = ("Показывать капсулы", "Enable Capsules"),
            ["background.toggle"] = ("Фоновая подложка", "Background Plate"),
            ["select.plate"]   = ("Выбрать цвет подложки", "Select plate color"),
            ["section.colors"] = ("Цвета секций", "Section Colors"),
            ["section.colors.d"] = ("Задайте отдельные цвета подписей и значений для каждой секции. Если не задано — используются общие цвета.",
                                    "Override label and metric colors per section. Leave unset to inherit global colors."),
            ["sec.net"]    = ("Сеть", "Network"),
            ["sec.cpuram"] = ("ЦП / ОЗУ", "CPU / RAM"),
            ["sec.gpu"]    = ("ГП / Темп", "GPU / Temp"),
            ["sec.disk"]   = ("Диск", "Disk"),
            ["sub.label"]  = ("Подпись", "Label"),
            ["sub.metric"] = ("Значение", "Metric"),
            ["cap.net"]    = ("Исх / Вх", "UP / DN"),
            ["cap.cpuram"] = ("ЦП / ОЗУ", "CPU / RAM"),
            ["cap.gpu"]    = ("ГП / ТЕМП", "GPU / TMP"),
            ["cap.disk"]   = ("Диск", "Disk"),
            ["clear.btn"]  = ("Сбросить", "Clear"),
            ["reset.appearance"] = ("Сбросить оформление", "Reset Appearance"),

            // About
            ["sec.about"]   = ("О программе", "About"),
            ["about.desc"]  = ("Лёгкий и быстрый мониторинг системы в виде оверлея для панели задач. Создан для опытных пользователей на WinUI 3 и GDI+.",
                               "A lightweight, high-performance system monitoring overlay designed for power users. Built with WinUI 3 and GDI+ rendering."),
            ["connect.title"] = ("Связь с разработчиком", "Connect with the Developer"),
            ["blog.link"]   = ("Блог (kil0bit.blogspot.com)", "Blog (kil0bit.blogspot.com)"),
            ["patreon.link"] = ("Поддержать (Patreon)", "Support Me (Patreon)"),
            ["made.with"]   = ("Сделано с ❤️ командой KB - kil0bit", "Built with ❤️ by KB - kil0bit"),
            ["app.edition"] = ("нативная версия для Windows 11", "Windows 11 native edition"),

            // Footer
            ["quit.app"]  = ("Выход из программы", "Quit Application"),
            ["reset.all"] = ("Сбросить все настройки", "Reset All Settings"),
            ["save.close"] = ("Сохранить и закрыть", "Save & Close"),

            // Dialogs
            ["reset.title"]   = ("Сброс к заводским настройкам", "Factory Reset"),
            ["reset.text"]    = ("Вы уверены, что хотите сбросить все настройки до заводских?\n\nБудут восстановлены параметры мониторинга, общие настройки и оформление. Это действие нельзя отменить.",
                                 "Are you sure you want to reset all settings to factory defaults?\n\nThis will revert all monitoring, general, and appearance preferences. This action cannot be undone."),
            ["reset.primary"] = ("Сбросить всё", "Reset All"),
            ["cancel"]        = ("Отмена", "Cancel"),
            ["ok"]            = ("ОК", "OK"),
            ["picker.alpha"]  = ("Прозрачность", "Alpha"),
            ["picker.red"]    = ("Красный", "Red"),
            ["picker.green"]  = ("Зелёный", "Green"),
            ["picker.blue"]   = ("Синий", "Blue"),
            ["picker.hex"]    = ("HEX-код", "Hex code"),
            ["picker.title"]  = ("Выбор цвета — {0}", "Select color — {0}"),
            ["tag.accent"]     = ("цвет значений", "metric accent"),
            ["tag.label"]      = ("цвет подписей", "label tone"),
            ["tag.background"] = ("цвет подложки", "background plate"),
            ["tag.pod"]        = ("цвет капсул", "capsule"),
            ["tag.netlabel"]   = ("сеть — подпись", "network — label"),
            ["tag.netaccent"]  = ("сеть — значение", "network — metric"),
            ["tag.cpulabel"]   = ("ЦП / ОЗУ — подпись", "CPU / RAM — label"),
            ["tag.cpuaccent"]  = ("ЦП / ОЗУ — значение", "CPU / RAM — metric"),
            ["tag.gpulabel"]   = ("ГП — подпись", "GPU — label"),
            ["tag.gpuaccent"]  = ("ГП — значение", "GPU — metric"),
            ["tag.disklabel"]  = ("диск — подпись", "disk — label"),
            ["tag.diskaccent"] = ("диск — значение", "disk — metric"),

            // Overlay context menu
            ["menu.settings"] = ("Настройки", "Settings"),
            ["menu.taskmgr"]  = ("Диспетчер задач", "Task Manager"),
            ["menu.ontop"]    = ("Поверх всех окон", "Keep on Top"),
            ["menu.hidefs"]   = ("Скрывать в полноэкранном режиме", "Hide in Fullscreen"),
            ["menu.lock"]     = ("Блокировка позиции", "Lock Position"),
            ["menu.snap"]     = ("Привязка к панели задач", "Snap to Taskbar"),
            ["menu.about"]    = ("О программе", "About"),
            ["menu.exit"]     = ("Выход", "Exit"),

            // Overlay metric labels
            ["ovl.netup"]     = ("Исх", "UP"),
            ["ovl.netup.c"]   = ("И", "U"),
            ["ovl.netdown"]   = ("Вх", "DN"),
            ["ovl.netdown.c"] = ("В", "D"),
            ["ovl.cpu"]       = ("ЦП", "CPU"),
            ["ovl.cpu.c"]     = ("Ц", "C"),
            ["ovl.ram"]       = ("ОЗУ", "RAM"),
            ["ovl.ram.c"]     = ("ОЗ", "R"),
            ["ovl.gpu"]       = ("ГП", "GPU"),
            ["ovl.gpu.c"]     = ("ГП", "G"),
            ["ovl.temp"]      = ("ТЕМП", "TMP"),
            ["ovl.temp.c"]    = ("Т", "T"),
            ["ovl.disk"]      = ("ДСК", "DK"),
            ["ovl.spd"]       = ("СКР", "SPD"),
            ["ovl.spd.c"]     = ("С", "S"),
            ["ovl.na"]        = ("н/д", "N/A"),
            ["ovl.net.reserve"] = ("1023 МБ/с", "1023 MB/s"),
            ["unit.kb"] = ("КБ/с", "KB/s"),
            ["unit.mb"] = ("МБ/с", "MB/s"),
            ["unit.gb"] = ("ГБ/с", "GB/s"),
        };

        // ===== XAML binding properties (settings window) =====

        public string WindowTitle => T("window.title");
        public string NavHome => T("nav.home");
        public string NavGeneral => T("nav.general");
        public string NavMonitoring => T("nav.monitoring");
        public string NavAppearance => T("nav.appearance");
        public string NavAbout => T("nav.about");

        public string HomeWelcome => T("home.welcome");
        public string HomeSubtitle => T("home.subtitle");
        public string CardGeneral => T("card.general");
        public string CardGeneralDesc => T("card.general.d");
        public string CardMonitoring => T("card.monitoring");
        public string CardMonitoringDesc => T("card.monitoring.d");
        public string CardAppearance => T("card.appearance");
        public string CardAppearanceDesc => T("card.appearance.d");
        public string CardAbout => T("card.about");
        public string CardAboutDesc => T("card.about.d");
        public string NoteTitle => T("note.title");
        public string NoteText => T("note.text");

        public string SecGeneral => T("sec.general");
        public string ShowOverlay => T("show.overlay");
        public string ShowOverlayDesc => T("show.overlay.d");
        public string LockPosition => T("lock.position");
        public string LockPositionDesc => T("lock.position.d");
        public string RunAtStartup => T("run.startup");
        public string RunAtStartupDesc => T("run.startup.d");
        public string HideFullscreen => T("hide.fullscreen");
        public string HideFullscreenDesc => T("hide.fullscreen.d");
        public string SnapTaskbar => T("snap.taskbar");
        public string SnapTaskbarDesc => T("snap.taskbar.d");
        public string KeepOnTop => T("keep.on.top");
        public string KeepOnTopDesc => T("keep.on.top.d");
        public string RefreshRate => T("refresh.rate");
        public string RefreshRateDesc => T("refresh.rate.d");
        public string OnLabel => T("toggle.on");
        public string OffLabel => T("toggle.off");
        public string UiLanguageLabel => T("ui.language");
        public string OverlayLangLabel => T("ovl.language");

        public string SecMonitoring => T("sec.monitoring");
        public string TelemetryHeader => T("telemetry.header");
        public string GroupCpu => T("group.cpu");
        public string CpuUsage => T("cpu.usage");
        public string RamUsage => T("ram.usage");
        public string GroupGpu => T("group.gpu");
        public string GpuUsage => T("gpu.usage");
        public string GpuTemp => T("gpu.temp");
        public string GroupNet => T("group.net");
        public string NetUp => T("net.up");
        public string NetDown => T("net.down");
        public string GroupDisk => T("group.disk");
        public string DiskSpace => T("disk.space");
        public string DiskActivity => T("disk.activity");
        public string HwSelection => T("hw.selection");
        public string NetAdapter => T("net.adapter");
        public string GpuCard => T("gpu.card");
        public string Drives => T("drives");

        public string SecAppearance => T("sec.appearance");
        public string Typography => T("typography");
        public string FontLabel => T("font.label");
        public string DisplayMode => T("display.mode");
        public string BoldText => T("bold.text");
        public string Palette => T("palette");
        public string AccentColor => T("accent.color");
        public string SelectAccent => T("select.accent");
        public string LabelColor => T("label.color");
        public string SelectLabel => T("select.label");
        public string PodColor => T("pod.color");
        public string SelectPod => T("select.pod");
        public string ScalingCard => T("scaling.card");
        public string ScalingLabel => T("scaling.label");
        public string SpacingLabel => T("spacing.label");
        public string CapsulesToggle => T("capsules.toggle");
        public string BackgroundToggle => T("background.toggle");
        public string SelectPlate => T("select.plate");
        public string SectionColors => T("section.colors");
        public string SectionColorsDesc => T("section.colors.d");
        public string SecNet => T("sec.net");
        public string SecCpuRam => T("sec.cpuram");
        public string SecGpu => T("sec.gpu");
        public string SecDisk => T("sec.disk");
        public string SubLabel => T("sub.label");
        public string SubMetric => T("sub.metric");
        public string CapNet => T("cap.net");
        public string CapCpuRam => T("cap.cpuram");
        public string CapGpu => T("cap.gpu");
        public string CapDisk => T("cap.disk");
        public string ClearBtn => T("clear.btn");
        public string ResetAppearance => T("reset.appearance");

        public string SecAbout => T("sec.about");
        public string AboutDesc => T("about.desc");
        public string ConnectTitle => T("connect.title");
        public string BlogLink => T("blog.link");
        public string PatreonLink => T("patreon.link");
        public string MadeWith => T("made.with");

        public string QuitApp => T("quit.app");
        public string ResetAll => T("reset.all");
        public string SaveClose => T("save.close");
    }
}
