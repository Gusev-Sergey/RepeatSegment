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
        bool preferYandex = _cfg.TranslationProviderPreference == "yandex";
        LblGoogle.IsChecked = !preferYandex;
        LblYandex.IsChecked = preferYandex;
        PanelYandexTranslate.Visibility = preferYandex ? Visibility.Visible : Visibility.Collapsed;
        TxtYandexTranslateKey.Text = _cfg.YandexTranslateApiKey ?? "";
        TxtYandexTranslateFolderId.Text = _cfg.YandexTranslateFolderId ?? "";
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        _cfg.TranslationProviderPreference = LblYandex.IsChecked == true ? "yandex" : "google";
        _cfg.YandexTranslateApiKey = TxtYandexTranslateKey.Text.Trim();
        _cfg.YandexTranslateFolderId = TxtYandexTranslateFolderId.Text.Trim();
        DialogResult = true; Close();
    }
    private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    private void LblGoogle_Checked(object sender, RoutedEventArgs e) { LblYandex.IsChecked = false; PanelYandexTranslate.Visibility = Visibility.Collapsed; }
    private void LblGoogle_Unchecked(object sender, RoutedEventArgs e) { if (LblYandex.IsChecked != true) LblGoogle.IsChecked = true; }
    private void LblYandex_Checked(object sender, RoutedEventArgs e) { LblGoogle.IsChecked = false; PanelYandexTranslate.Visibility = Visibility.Visible; }
    private void LblYandex_Unchecked(object sender, RoutedEventArgs e) { PanelYandexTranslate.Visibility = Visibility.Collapsed; if (LblGoogle.IsChecked != true) LblGoogle.IsChecked = true; }

    // Old cb handler aliases
    private void CbTranslationGoogle_Checked(object sender, RoutedEventArgs e) => LblGoogle_Checked(sender, e);
    private void CbTranslationGoogle_Unchecked(object sender, RoutedEventArgs e) => LblGoogle_Unchecked(sender, e);
    private void CbTranslationYandex_Checked(object sender, RoutedEventArgs e) => LblYandex_Checked(sender, e);
    private void CbTranslationYandex_Unchecked(object sender, RoutedEventArgs e) => LblYandex_Unchecked(sender, e);
}
