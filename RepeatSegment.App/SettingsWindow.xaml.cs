using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

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
        ApplyComboBoxTheme();
        LoadSettings();
    }

    private void InjectBrushesFromMainWindow()
    {
        foreach (var key in _mainWindow.Resources.Keys)
        {
            if (_mainWindow.Resources[key] is SolidColorBrush brush)
            {
                Resources[key] = new SolidColorBrush(brush.Color);
            }
        }
        if (Resources["WindowBackgroundBrush"] is SolidColorBrush bg)
            Background = bg;
    }

    private void ApplyComboBoxTheme()
    {
        var fg = Resources["TextBrush"] as SolidColorBrush ?? Brushes.Black;
        var bg = Resources["InputBgBrush"] as SolidColorBrush ?? Brushes.White;
        var border = Resources["InputBorderBrush"] as SolidColorBrush ?? Brushes.Gray;

        CmbSaluteLang.Background = bg;
        CmbSaluteLang.Foreground = fg;
        CmbSaluteLang.BorderBrush = border;

        foreach (ComboBoxItem item in CmbSaluteLang.Items)
        {
            item.Background = bg;
            item.Foreground = fg;
        }

        // After template is applied, find ToggleButton and theme it
        CmbSaluteLang.Loaded += (_, _) =>
        {
            var tb = FindVisualChild<ToggleButton>(CmbSaluteLang);
            if (tb != null)
            {
                tb.Background = bg;
                tb.Foreground = fg;
                tb.BorderBrush = border;
            }
        };
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var result = FindVisualChild<T>(child);
            if (result != null) return result;
        }
        return null;
    }

    private void LoadSettings()
    {
        // Hidden providers — always unchecked (not yet implemented)
        CbVkCloud.IsChecked = false;
        CbYandex.IsChecked = false;
        CbSalute.IsChecked = false;
        CbAssemblyAi.IsChecked = _cfg.ProvidersEnabled.Contains("assemblyai");
        CbDeepgram.IsChecked = _cfg.ProvidersEnabled.Contains("deepgram");

        TxtVkToken.Text = _cfg.VkCloudToken ?? "";
        TxtYandexKey.Text = _cfg.YandexApiKey ?? "";
        TxtYandexFolderId.Text = _cfg.YandexFolderId ?? "";
        TxtSaluteClientId.Text = _cfg.SaluteClientId ?? "";
        TxtSaluteSecret.Text = _cfg.SaluteClientSecret ?? "";
        TxtSaluteScope.Text = _cfg.SaluteScope ?? "SALUTE_SPEECH_PERS";
        TxtSaluteProfileId.Text = _cfg.SaluteProfileId ?? "";
        TxtAssemblyAiKey.Text = _cfg.AssemblyAiApiKey ?? "";
        TxtDeepgramKey.Text = _cfg.DeepgramApiKey ?? "";
        TxtYandexTranslateKey.Text = _cfg.YandexTranslateApiKey ?? "";
        TxtYandexTranslateFolderId.Text = _cfg.YandexTranslateFolderId ?? "";

        // Translation provider — mutual exclusion: Google default, Yandex optional
        bool preferYandex = _cfg.TranslationProviderPreference == "yandex";
        CbTranslationGoogle.IsChecked = !preferYandex;
        CbTranslationYandex.IsChecked = preferYandex;
        PanelYandexTranslate.Visibility = preferYandex ? Visibility.Visible : Visibility.Collapsed;

        // Transcription language
        var transLangs = new[] { ("en","English"), ("es","Español"), ("fr","Français"),
            ("de","Deutsch"), ("it","Italiano"), ("pt","Português"), ("ru","Русский"),
            ("ja","日本語"), ("ko","한국어"), ("zh","中文"), ("hi","हिन्दी") };
        CmbTranscriptionLang.Items.Clear();
        foreach (var (code, name) in transLangs)
        {
            var item = new ComboBoxItem { Content = name, Tag = code };
            CmbTranscriptionLang.Items.Add(item);
            if (code == _cfg.TranscriptionLanguage) item.IsSelected = true;
        }
        if (CmbTranscriptionLang.SelectedIndex < 0 && CmbTranscriptionLang.Items.Count > 0)
            CmbTranscriptionLang.SelectedIndex = 0;

        string curLang = _cfg.SaluteLang ?? "ru-RU";
        foreach (ComboBoxItem item in CmbSaluteLang.Items)
        {
            if ((string)item.Tag == curLang)
            {
                item.IsSelected = true;
                break;
            }
        }

        TxtChunkMinutes.Text = _cfg.ChunkMinutes.ToString();
        TxtPlaybackLatency.Text = _cfg.PlaybackLatency.ToString("F2");
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
        var enabled = new List<string>();
        // Hidden providers not included (VK, Yandex, Salute — not implemented)
        if (CbAssemblyAi.IsChecked == true) enabled.Add("assemblyai");
        if (CbDeepgram.IsChecked == true) enabled.Add("deepgram");
        _cfg.ProvidersEnabled = enabled.Count > 0 ? enabled : new List<string> { "deepgram" };

        _cfg.VkCloudToken = TxtVkToken.Text.Trim();
        _cfg.YandexApiKey = TxtYandexKey.Text.Trim();
        _cfg.YandexFolderId = TxtYandexFolderId.Text.Trim();
        _cfg.SaluteClientId = TxtSaluteClientId.Text.Trim();
        _cfg.SaluteClientSecret = TxtSaluteSecret.Text.Trim();
        _cfg.SaluteScope = TxtSaluteScope.Text.Trim();
        _cfg.SaluteLang = (CmbSaluteLang.SelectedItem as ComboBoxItem)?.Tag as string ?? "ru-RU";
        _cfg.SaluteProfileId = TxtSaluteProfileId.Text.Trim();
        _cfg.AssemblyAiApiKey = TxtAssemblyAiKey.Text.Trim();
        _cfg.DeepgramApiKey = TxtDeepgramKey.Text.Trim();
        _cfg.YandexTranslateApiKey = TxtYandexTranslateKey.Text.Trim();
        _cfg.YandexTranslateFolderId = TxtYandexTranslateFolderId.Text.Trim();

        // Translation provider preference — mutual exclusion
        _cfg.TranslationProviderPreference = CbTranslationYandex.IsChecked == true ? "yandex" : "google";

        _cfg.TranscriptionLanguage = (CmbTranscriptionLang.SelectedItem as ComboBoxItem)?.Tag as string ?? "en";

        if (int.TryParse(TxtChunkMinutes.Text.Trim(), out int cm))
            _cfg.ChunkMinutes = cm;
        if (float.TryParse(TxtPlaybackLatency.Text.Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float pl))
            _cfg.PlaybackLatency = pl;

        Saved = true;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Saved = false;
        DialogResult = false;
        Close();
    }

    // --- Translation provider mutual exclusion ---

    private void CbTranslationGoogle_Checked(object sender, RoutedEventArgs e)
    {
        CbTranslationYandex.IsChecked = false;
        PanelYandexTranslate.Visibility = Visibility.Collapsed;
    }

    private void CbTranslationGoogle_Unchecked(object sender, RoutedEventArgs e)
    {
        if (CbTranslationYandex.IsChecked != true)
            CbTranslationGoogle.IsChecked = true; // keep at least one checked
    }

    private void CbTranslationYandex_Checked(object sender, RoutedEventArgs e)
    {
        CbTranslationGoogle.IsChecked = false;
        PanelYandexTranslate.Visibility = Visibility.Visible;
    }

    private void CbTranslationYandex_Unchecked(object sender, RoutedEventArgs e)
    {
        PanelYandexTranslate.Visibility = Visibility.Collapsed;
        if (CbTranslationGoogle.IsChecked != true)
            CbTranslationGoogle.IsChecked = true; // fall back to Google
    }
}
