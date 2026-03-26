using System.Diagnostics;
using System.Runtime.InteropServices;
using OpenRouterBudget;

namespace OpenRouterBudget;

static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        // Single-instance check
        const string mutexName = "OpenRouterBudget_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("OpenRouter Budget is already running in the system tray.",
                "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string appDir = AppDomain.CurrentDomain.BaseDirectory;

        // Create default config if missing
        ConfigManager.CreateDefaultConfig(appDir);

        // Load API key
        string apiKey = ConfigManager.LoadApiKey(appDir);

        if (string.IsNullOrEmpty(apiKey))
        {
            string configPath = Path.Combine(appDir, "config.json");
            MessageBox.Show(
                $"No API key found.\n\nEdit {configPath}\nand set your OpenRouter API key:\n\n{{\"api_key\": \"sk-or-v1-YOUR_KEY\"}}",
                "OpenRouter Budget - Setup",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            // Open the config file for editing
            try
            {
                Process.Start(new ProcessStartInfo { FileName = configPath, UseShellExecute = true });
            }
            catch { }

            return;
        }

        // Run the tray app
        Application.Run(new TrayApp(apiKey, appDir));
    }
}
