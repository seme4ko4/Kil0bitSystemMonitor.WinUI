using System;
using System.IO;
using System.Text.Json;
using Kil0bitSystemMonitor.Models;

namespace Kil0bitSystemMonitor.Services
{
    public class ConfigService
    {
        private readonly string _configPath;
        public AppConfig Config { get; private set; }

        public ConfigService()
        {
            // Portable mode: if config.json sits next to the exe, settings live in the app folder
            // (copy the folder to another PC and everything travels with it). Otherwise %APPDATA%.
            // Environment.ProcessPath (not AppContext.BaseDirectory) — correct for single-file builds too.
            string exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
            string localPath = Path.Combine(exeDir, "config.json");
            if (File.Exists(localPath))
            {
                _configPath = localPath;
            }
            else
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string configDir = Path.Combine(appData, "kil0bit-system-monitor");
                Directory.CreateDirectory(configDir);
                _configPath = Path.Combine(configDir, "config.json");
            }

            Config = LoadConfig();
            Kil0bitSystemMonitor.Helpers.Diag.Log($"config path: {_configPath}");

            // Ensure Windows Registry matches the loaded config unconditionally
            StartupService.SetStartup(Config.LaunchOnStartup);

            // Auto-save and sync any future changes made from UI directly
            Config.PropertyChanged += (s, e) =>
            {
                SaveConfig();
                if (e.PropertyName == nameof(AppConfig.LaunchOnStartup))
                {
                    StartupService.SetStartup(Config.LaunchOnStartup);
                }
            };
        }

        private AppConfig LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    string json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                }
                catch { }
            }
            return new AppConfig();
        }

        public event Action? SettingsChanged;
        
        public void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
                SettingsChanged?.Invoke();
            }
            catch { }
        }
    }
}
