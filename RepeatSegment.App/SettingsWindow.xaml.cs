using System.Collections.Generic;
using System.Windows;

namespace RepeatSegment.App;

public partial class SettingsWindow : Window
{
    private readonly ConfigManager _cfg;
    private readonly MainWindow _mainWindow;
    public bool Saved { get; private set; }

    public SettingsWindow(ConfigManager cfg, MainWindow mainWindow)
    {
        _cfg = cfg;
        _mainWindow = mainWindow;
        InjectBrushesFromMainWindow();
        InitializeComponent();
        Owner = _mainWindow;
        ApplyStrings();
        LoadSettings();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mainWindow.IsDarkTheme)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
            int useDark = 1;
            DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int));
        }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private void InjectBrushesFromMainWindow()
    {
        foreach (var key in _mainWindow.Resources.Keys)
            if (_mainWindow.Resources[key] is System.Windows.Media.SolidColorBrush brush)
                Resources[key] = new System.Windows.Media.SolidColorBrush(brush.Color);
        if (Resources["WindowBackgroundBrush"] is System.Windows.Media.SolidColorBrush bg) Background = bg;
    }

    private void ApplyStrings()
    {
        Title = Strings.Get("sw.title_api");
        GrpProviders.Header = Strings.Get("sw.providers");
        LblAssemblyAi.Content = Strings.Get("sw.assemblyai");
        LblDeepgram.Content = Strings.Get("sw.deepgram");
        LblApiKeysHeader.Text = Strings.Get("sw.apikeys_header");
        LblAssemblyAiHeader.Text = Strings.Get("sw.assemblyai_header");
        LblAssemblyAiWarn.Text = Strings.Get("sw.assemblyai_warn");
        LblDeepgramHeader.Text = Strings.Get("sw.deepgram_header");
        LblApiKey1.Text = Strings.Get("sw.assemblyai_key");
        LblApiKey2.Text = Strings.Get("sw.deepgram_key");
        BtnOk.Content = Strings.Get("sw.ok");
        BtnCancel.Content = Strings.Get("sw.cancel");
    }

    private void LoadSettings()
    {
        LblAssemblyAi.IsChecked = _cfg.ProvidersEnabled.Contains("assemblyai");
        LblDeepgram.IsChecked = _cfg.ProvidersEnabled.Contains("deepgram");
        TxtAssemblyAiKey.Text = _cfg.AssemblyAiApiKey ?? "";
        TxtDeepgramKey.Text = _cfg.DeepgramApiKey ?? "";
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        var enabled = new List<string>();
        if (LblAssemblyAi.IsChecked == true) enabled.Add("assemblyai");
        if (LblDeepgram.IsChecked == true) enabled.Add("deepgram");
        _cfg.ProvidersEnabled = enabled.Count > 0 ? enabled : new List<string> { "deepgram" };
        _cfg.AssemblyAiApiKey = TxtAssemblyAiKey.Text.Trim();
        _cfg.DeepgramApiKey = TxtDeepgramKey.Text.Trim();
        Saved = true; DialogResult = true; Close();
    }
    private void BtnCancel_Click(object sender, RoutedEventArgs e) { Saved = false; DialogResult = false; Close(); }
    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); } catch { } e.Handled = true; }
}
