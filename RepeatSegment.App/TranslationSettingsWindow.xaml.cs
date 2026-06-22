using System.Windows;

namespace RepeatSegment.App;

public partial class TranslationSettingsWindow : Window
{
    private readonly ConfigManager _cfg;
    private readonly MainWindow _mw;

    public TranslationSettingsWindow(ConfigManager cfg, MainWindow mw)
    {
        _cfg = cfg;
        _mw = mw;
        InjectBrushes();
        InitializeComponent();
        Owner = _mw;

        double sw = SystemParameters.WorkArea.Width, sh = SystemParameters.WorkArea.Height;
        Width = Math.Min(sw * 0.38, 500);
        MinWidth = Math.Max(360, sw * 0.26);
        MinHeight = Math.Max(240, sh * 0.20);

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
        Title = Strings.Get("sw.title_translation");
        LblTranslationHeader.Text = Strings.Get("sw.translation_header");
        LblTranslationInfo.Text = Strings.Get("sw.translation_info");
        LblGoogle.Content = Strings.Get("sw.translation_google");
        LblYandex.Content = Strings.Get("sw.translation_yandex");
        LblYandexKey.Text = Strings.Get("sw.translation_yandex_key");
        LblYandexFolder.Text = Strings.Get("sw.translation_yandex_folder");
        BtnOk.Content = Strings.Get("sw.ok");
        BtnCancel.Content = Strings.Get("sw.cancel");
    }

    private void LoadSettings()
    {
        LblGoogle.IsChecked = _cfg.TranslationProviderPreference == "google" || _cfg.TranslationProviderPreference == "google,yandex" || string.IsNullOrEmpty(_cfg.TranslationProviderPreference);
        LblYandex.IsChecked = _cfg.TranslationProviderPreference.Contains("yandex");
        PanelYandexTranslate.Visibility = LblYandex.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        TxtYandexTranslateKey.Text = _cfg.YandexTranslateApiKey ?? "";
        TxtYandexTranslateFolderId.Text = _cfg.YandexTranslateFolderId ?? "";
    }

    private void CbTranslationGoogle_Checked(object s, RoutedEventArgs e) { }
    private void CbTranslationGoogle_Unchecked(object s, RoutedEventArgs e) { }
    private void CbTranslationYandex_Checked(object s, RoutedEventArgs e) { PanelYandexTranslate.Visibility = Visibility.Visible; }
    private void CbTranslationYandex_Unchecked(object s, RoutedEventArgs e) { PanelYandexTranslate.Visibility = Visibility.Collapsed; }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        var prefs = new System.Collections.Generic.List<string>();
        if (LblGoogle.IsChecked == true) prefs.Add("google");
        if (LblYandex.IsChecked == true) prefs.Add("yandex");
        _cfg.TranslationProviderPreference = prefs.Count > 0 ? string.Join(",", prefs) : "google";
        _cfg.YandexTranslateApiKey = TxtYandexTranslateKey.Text.Trim();
        _cfg.YandexTranslateFolderId = TxtYandexTranslateFolderId.Text.Trim();
        _cfg.Save(_cfg.Path, _cfg.FileName, 0, 0);
        DialogResult = true; Close();
    }
    private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
}
