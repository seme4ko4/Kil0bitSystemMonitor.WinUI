using System;
using System.IO;

namespace Kil0bitSystemMonitor.Helpers
{
    internal static class Diag
    {
        private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "ksm_winui.log");

        public static void Log(string msg)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss.fff} | {msg}\r\n");
            }
            catch { }
        }

        public static void Log(string stage, Exception ex) => Log($"ERROR [{stage}] {ex.GetType().FullName}: {ex.Message}\r\n{ex.StackTrace}");
    }
}
