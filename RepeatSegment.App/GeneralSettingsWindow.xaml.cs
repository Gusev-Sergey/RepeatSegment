using System.Windows;

namespace RepeatSegment.App;

public partial class GeneralSettingsWindow : Window
{
    private readonly ConfigManager _cfg;
    private readonly MainWindow _mw;

    public GeneralSettingsWindow(ConfigManager cfg, MainWindow mw)
    {
        _cfg = cfg;
        _mw = mw;
        InjectBrushes();
        InitializeComponent();
        Owner = _mw;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ApplyStrings();
        LoadSettings();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mw.IsDarkTheme)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
            int useDark = 1;
            DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int));
        }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private void InjectBrushes()
    {
        foreach (var key in _mw.Resources.Keys)
            if (_mw.Resources[key] is System.Windows.Media.SolidColorBrush brush)
                Resources[key] = new System.Windows.Media.SolidColorBrush(brush.Color);
        if (Resources["WindowBackgroundBrush"] is System.Windows.Media.SolidColorBrush bg) Background = bg;
    }

    private void ApplyStrings()
    {
        Title = Strings.Get("sw.title_general");
        GrpGeneral.Header = Strings.Get("sw.general_header");
        GrpAnki.Header = Strings.Get("sw.anki_header");
        LblUiLang.Text = Strings.Get("sw.ui_lang");
        LblTranscriptionLang.Text = Strings.Get("sw.transcription_lang");
        LblMp3Bitrate.Text = Strings.Get("sw.mp3_bitrate");
        LblImageProvider.Text = Strings.Get("sw.image_provider");
        LblChunkMinutes.Text = Strings.Get("sw.chunk_minutes");
        LblHighlightLatency.Text = Strings.Get("sw.highlight_latency");
        BtnOk.Content = Strings.Get("sw.ok");
        BtnCancel.Content = Strings.Get("sw.cancel");
    }

    private void LoadSettings()
    {
        // UI language
        var uiLangs = new[] { ("en","English"),("ru","Русский"),("de","Deutsch"),("fr","Français"),("es","Español") };
        CmbUiLang.Items.Clear();
        foreach (var (code, name) in uiLangs) { var item = new System.Windows.Controls.ComboBoxItem { Content = name, Tag = code }; CmbUiLang.Items.Add(item); if (code == _cfg.Language) item.IsSelected = true; }
        if (CmbUiLang.SelectedIndex < 0 && CmbUiLang.Items.Count > 0) CmbUiLang.SelectedIndex = 0;

        var transLangs = new[] { ("en","English"),("es","Español — Spanish"),("fr","Français — French"),("de","Deutsch — German"),("it","Italiano — Italian"),("pt","Português — Portuguese"),("ru","Русский — Russian"),("ja","日本語 — Japanese"),("ko","한국어 — Korean"),("zh","中文 — Chinese"),("hi","हिन्दी — Hindi") };
        CmbTranscriptionLang.Items.Clear();
        foreach (var (code, name) in transLangs) { var item = new System.Windows.Controls.ComboBoxItem { Content = name, Tag = code }; CmbTranscriptionLang.Items.Add(item); if (code == _cfg.TranscriptionLanguage) item.IsSelected = true; }
        if (CmbTranscriptionLang.SelectedIndex < 0 && CmbTranscriptionLang.Items.Count > 0) CmbTranscriptionLang.SelectedIndex = 0;

        // MP3 bitrate
        CmbMp3Bitrate.Items.Clear();
        CmbMp3Bitrate.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = Strings.Get("sw.mp3_64"), Tag = "64" });
        CmbMp3Bitrate.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = Strings.Get("sw.mp3_128"), Tag = "128" });
        foreach (System.Windows.Controls.ComboBoxItem item in CmbMp3Bitrate.Items) if ((string)item.Tag == _cfg.Mp3BitrateKbps.ToString()) { item.IsSelected = true; break; }
        if (CmbMp3Bitrate.SelectedIndex < 0) CmbMp3Bitrate.SelectedIndex = 1;

        // Image search provider
        CmbImageProvider.Items.Clear();
        CmbImageProvider.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Google", Tag = "google" });
        CmbImageProvider.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Yandex", Tag = "yandex" });
        foreach (System.Windows.Controls.ComboBoxItem item in CmbImageProvider.Items) if ((string)item.Tag == _cfg.ImageSearchProvider) { item.IsSelected = true; break; }
        if (CmbImageProvider.SelectedIndex < 0) CmbImageProvider.SelectedIndex = 0;

        TxtChunkMinutes.Text = _cfg.ChunkMinutes.ToString();
        TxtPlaybackLatency.Text = _cfg.PlaybackLatency.ToString("F2");
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        string uiLang = (CmbUiLang.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "en";
        if (_cfg.Language != uiLang) { _cfg.Language = uiLang; Strings.SetLanguage(uiLang); _mw.ApplyAllStrings(); }
        _cfg.TranscriptionLanguage = (CmbTranscriptionLang.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "en";
        if (int.TryParse((CmbMp3Bitrate.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "128", out int br)) _cfg.Mp3BitrateKbps = br;
        _cfg.ImageSearchProvider = (CmbImageProvider.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag as string ?? "google";
        if (int.TryParse(TxtChunkMinutes.Text.Trim(), out int cm)) _cfg.ChunkMinutes = cm;
        if (float.TryParse(TxtPlaybackLatency.Text.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float pl)) _cfg.PlaybackLatency = pl;
        DialogResult = true; Close();
    }
    private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
