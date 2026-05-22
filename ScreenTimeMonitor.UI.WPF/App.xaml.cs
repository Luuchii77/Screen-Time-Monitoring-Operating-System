using System.Windows;
using ScreenTimeMonitor.UI.WPF.Services;

namespace ScreenTimeMonitor.UI.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Load configuration from appsettings.json
            SettingsManager.Load();
        }
    }
}
