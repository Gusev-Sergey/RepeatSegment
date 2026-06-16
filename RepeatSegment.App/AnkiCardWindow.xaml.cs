using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SkiaSharp;

namespace RepeatSegment.App;

public partial class AnkiCardWindow : Window
{
    private readonly AudioEngine? _audio;
    private readonly TranscriptionProvider? _transcriptionProvider;
    private readonly TranslationProvider? _translationProvider;
    private readonly string _selectedWord;
    private readonly double _wordStart;
    private readonly double _wordEnd;
    private string? _savedAudioPath;
    private string? _savedImagePath;

    public AnkiCardWindow(string selectedWord, string context, double wordStart, double wordEnd,
                          AudioEngine? audio, TranscriptionProvider? transcription, TranslationProvider? translation,
                          string? ruTranslation, string? transcriptionText)
    {
        InitializeComponent();
        _selectedWord = selectedWord;
        _wordStart = wordStart;
        _wordEnd = wordEnd;
        _audio = audio;
        _transcriptionProvider = transcription;
        _translationProvider = translation;

        TxtEn.Text = selectedWord;
        TxtTranscription.Text = transcriptionText ?? "";
        TxtRu.Text = ruTranslation ?? "";
        TxtContext.Text = context;
        TxtSearchQuery.Text = selectedWord;

        LoadDecks();
        InitializeWebViewAsync();
    }

    private async void InitializeWebViewAsync()
    {
        try
        {
            await ImageBrowser.EnsureCoreWebView2Async(null);
            ImageBrowser.DefaultBackgroundColor = System.Drawing.Color.White;
        }
        catch { TxtStatus.Text = "WebView2 init failed. Install WebView2 Runtime."; }
    }

    private void LoadDecks()
    {
        CmbDeck.Items.Clear();
        var decks = AnkiExportManager.ListDecks();
        foreach (var d in decks) CmbDeck.Items.Add(d);
        if (CmbDeck.Items.Count > 0) CmbDeck.SelectedIndex = 0;
        else CmbDeck.Items.Add("(no decks — create new)");
    }

    private void BtnNewDeck_Click(object s, RoutedEventArgs e)
    {
        string? name = Microsoft.VisualBasic.Interaction.InputBox("Deck name:", "New Anki Deck", "");
        if (string.IsNullOrWhiteSpace(name)) return;
        CmbDeck.Items.Add(name);
        CmbDeck.SelectedItem = name;
    }

    private void BtnPlayAudio_Click(object s, RoutedEventArgs e)
    {
        if (_audio == null) return;
        try
        {
            _savedAudioPath = _audio.SaveSnippetWav(_wordStart, _wordEnd);
            TxtAudio.Text = _savedAudioPath;
            TxtStatus.Text = "Audio saved ✓";
            // Play
            var player = new System.Media.SoundPlayer(_savedAudioPath);
            player.Play();
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Audio: {ex.Message}";
        }
    }

    private void BtnSearchImages_Click(object s, RoutedEventArgs e)
    {
        string query = TxtSearchQuery.Text.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;
        string url = $"https://yandex.ru/images/search?text={Uri.EscapeDataString(query)}";
        if (ImageBrowser.CoreWebView2 != null)
            ImageBrowser.CoreWebView2.Navigate(url);
        else
            TxtStatus.Text = "WebView2 not ready yet...";
    }

    private void BtnDownloadImage_Click(object s, RoutedEventArgs e)
    {
        // Get current URL from WebView2
        if (ImageBrowser.CoreWebView2 == null) return;
        _ = DownloadImageFromBrowserAsync();
    }

    private async Task DownloadImageFromBrowserAsync()
    {
        try
        {
            // Execute JS to get the URL of the currently selected/visible image
            string url = await ImageBrowser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.serp-item_selected img')?.src || document.querySelector('.ImagesContentImage-Image')?.src || document.querySelector('img.MMImage-Origin')?.src || ''");
            url = url.Trim('"');
            if (string.IsNullOrWhiteSpace(url) || url == "null")
            {
                TxtStatus.Text = "Click on an image to select it, then press ✓ Use selected";
                return;
            }

            TxtStatus.Text = "Downloading image...";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            byte[] data = await http.GetByteArrayAsync(url);

            // Resize with SkiaSharp
            using var bmp = SKBitmap.Decode(data);
            if (bmp != null)
            {
                int maxDim = 600;
                int w = bmp.Width, h = bmp.Height;
                if (w > maxDim || h > maxDim)
                {
                    float scale = Math.Min((float)maxDim / w, (float)maxDim / h);
                    w = (int)(w * scale);
                    h = (int)(h * scale);
                    using var resized = bmp.Resize(new SKImageInfo(w, h), SKFilterQuality.Medium);
                    using var image = SKImage.FromBitmap(resized);
                    using var data2 = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                    data = data2.ToArray();
                }
                else
                {
                    using var image = SKImage.FromBitmap(bmp);
                    using var data2 = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                    data = data2.ToArray();
                }
            }

            string mediaDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RepeatSegment", "decks", "media");
            Directory.CreateDirectory(mediaDir);
            _savedImagePath = Path.Combine(mediaDir, $"img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jpg");
            File.WriteAllBytes(_savedImagePath, data);

            // Show preview
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(data);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            ImgPreview.Source = bitmap;
            PicturePreview.Visibility = Visibility.Visible;
            ImageBrowser.Visibility = Visibility.Collapsed;

            TxtStatus.Text = "Image saved ✓";
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Image: {ex.Message}";
        }
    }

    private void BtnCreate_Click(object s, RoutedEventArgs e)
    {
        string deckName = CmbDeck.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(deckName) || deckName.StartsWith("("))
        {
            TxtStatus.Text = "Select or create a deck first";
            return;
        }

        try
        {
            var mgr = new AnkiExportManager(deckName);

            string imgId = "";
            string audId = "";

            if (!string.IsNullOrEmpty(_savedImagePath) && File.Exists(_savedImagePath))
                imgId = mgr.AddMedia(_savedImagePath);

            if (!string.IsNullOrEmpty(_savedAudioPath) && File.Exists(_savedAudioPath))
                audId = mgr.AddMedia(_savedAudioPath);

            mgr.AddNote(
                TxtEn.Text.Trim(),
                TxtTranscription.Text.Trim(),
                TxtRu.Text.Trim(),
                imgId,
                audId,
                TxtContext.Text.Trim()
            );

            string apkgPath = mgr.Finalize();
            TxtStatus.Text = $"Cards created! Deck: {apkgPath}";
            MessageBox.Show($"Cards added to deck:\n{apkgPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Error: {ex.Message}";
        }
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e) => Close();
}
