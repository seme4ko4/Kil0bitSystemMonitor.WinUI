using System.ComponentModel;
using System.Runtime.CompilerServices;
using Kil0bitSystemMonitor.Models;

namespace Kil0bitSystemMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public string AppVersion
        {
            get
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return $"v{version?.Major}.{version?.Minor}.{version?.Build} ({Kil0bitSystemMonitor.Helpers.Loc.T("app.edition")})";
            }
        }

        /// <summary>Re-raises AppVersion so x:Bind refreshes after a language switch.</summary>
        public void RefreshLocalized() => OnPropertyChanged(nameof(AppVersion));

        private SystemMetrics _metrics = new();
        public SystemMetrics Metrics
        {
            get => _metrics;
            set { _metrics = value; OnPropertyChanged(); }
        }

        private AppConfig _config = new();
        public AppConfig Config
        {
            get => _config;
            set { _config = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
