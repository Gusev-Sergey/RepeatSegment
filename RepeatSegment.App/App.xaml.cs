using System.IO;
using System.Windows;

namespace RepeatSegment.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ApplySavedLanguage();
        var mw = new MainWindow();
        mw.ApplyAllStrings();
        mw.Show();
    }

    internal static void ApplySavedLanguage()
    {
        string configDir = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "RepeatSegment");
        string configPath = Path.Combine(configDir, "config.ini");

        string lang = "";
        if (File.Exists(configPath))
        {
            foreach (var line in File.ReadAllLines(configPath))
            {
                if (line.StartsWith("language"))
                {
                    lang = line.Split('=')[1].Trim().ToLowerInvariant();
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(lang) || (lang != "en" && lang != "ru"))
        {
            var dlg = new FirstRunWindow();
            dlg.ShowDialog();
            lang = dlg.SelectedLanguage ?? "en";
        }

        Strings.SetLanguage(lang);
    }
}

