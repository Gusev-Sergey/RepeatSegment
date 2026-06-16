using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Web.WebView2.Wpf;
using SkiaSharp;

namespace RepeatSegment.App;

public partial class AnkiCardWindow : Window
{
    private readonly AudioEngine? _audio;
    private readonly TranslationProvider? _translationProvider;
    private readonly string _selectedWord;
    private readonly double _wordStart;
    private readonly double _wordEnd;
    private string? _savedAudioPath;
    private string? _savedImagePath;
    private string? _matchedWord;

    public AnkiCardWindow(string selectedWord, string context, double wordStart, double wordEnd,
                          AudioEngine? audio, TranscriptionProvider? transcription, TranslationProvider? translation,
                          string? ruTranslation, string? matchedWord)
    {
        InitializeComponent();
        _selectedWord = selectedWord;
        _wordStart = wordStart;
        _wordEnd = wordEnd;
        _audio = audio;
        _translationProvider = translation;
        _matchedWord = matchedWord;

        TxtEn.Text = selectedWord;
        TxtRu.Text = ruTranslation ?? "";
        TxtContext.Text = context;
        TxtSearchQuery.Text = matchedWord ?? selectedWord;

        // Try IPA lookup
        _ = LookupIpaAsync();

        LoadDecks();
        InitializeWebViewAsync();
    }

    private async Task LookupIpaAsync()
    {
        string word = _matchedWord ?? _selectedWord;
        if (string.IsNullOrWhiteSpace(word)) return;

        string? ipa = null;

        // Try 1: Free Dictionary API
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            string url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{Uri.EscapeDataString(word.ToLower())}";
            string json = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                var entry = root[0];
                if (entry.TryGetProperty("phonetic", out var ph) && !string.IsNullOrWhiteSpace(ph.GetString()))
                    ipa = ph.GetString();
                else if (entry.TryGetProperty("phonetics", out var phArr) && phArr.ValueKind == JsonValueKind.Array && phArr.GetArrayLength() > 0)
                {
                    for (int i = 0; i < phArr.GetArrayLength(); i++)
                    {
                        if (phArr[i].TryGetProperty("text", out var pt) && !string.IsNullOrWhiteSpace(pt.GetString()))
                        {
                            ipa = pt.GetString();
                            break;
                        }
                    }
                }
            }
        }
        catch { /* try fallback */ }

        // Try 2: Wiktionary REST API (if dictionaryapi blocked)
        if (string.IsNullOrWhiteSpace(ipa))
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                string url = $"https://en.wiktionary.org/api/rest_v1/page/definition/{Uri.EscapeDataString(word.ToLower())}";
                string json = await http.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);

                // Wiktionary response: {"en": [{"partOfSpeech": "noun", "definitions": [...]}]}
                // Look for IPA in the response — not in this API, need the mobile-sections API
                // Actually try /api/rest_v1/page/mobile-sections/{word}
                url = $"https://en.wiktionary.org/api/rest_v1/page/mobile-sections/{Uri.EscapeDataString(word.ToLower())}";
                json = await http.GetStringAsync(url);
                using var doc2 = JsonDocument.Parse(json);
                if (doc2.RootElement.TryGetProperty("lead", out var lead))
                {
                    string leadText = lead.TryGetProperty("sections", out var sections)
                        ? string.Join(" ", sections.EnumerateArray().Select(s => s.TryGetProperty("text", out var t) ? t.GetString() ?? "" : ""))
                        : "";
                    // Extract /IPA/ from lead (standard format in Wiktionary)
                    var match = System.Text.RegularExpressions.Regex.Match(leadText, @"/[^/]+/");
                    if (match.Success && match.Value.Length > 4)
                        ipa = match.Value.Trim('/');
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(ipa))
        {
            string finalIpa = ipa.Trim('/').Trim('[').Trim(']');
            Dispatcher.Invoke(() => TxtTranscription.Text = finalIpa);
        }
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
        {
            PicturePreview.Visibility = Visibility.Collapsed;
            ImageBrowser.Visibility = Visibility.Visible;
            ImageBrowser.CoreWebView2.Navigate(url);
        }
        else TxtStatus.Text = "WebView2 not ready yet...";
    }

    private void BtnSearchAgain_Click(object s, RoutedEventArgs e)
    {
        // Go back to image search
        PicturePreview.Visibility = Visibility.Collapsed;
        ImageBrowser.Visibility = Visibility.Visible;
    }

    private void BtnDownloadImage_Click(object s, RoutedEventArgs e)
    {
        if (ImageBrowser.CoreWebView2 == null) return;
        _ = DownloadImageFromBrowserAsync();
    }

    private async Task DownloadImageFromBrowserAsync()
    {
        try
        {
            string url = await ImageBrowser.CoreWebView2.ExecuteScriptAsync(
                "document.querySelector('.serp-item_selected img')?.src || document.querySelector('.ImagesContentImage-Image')?.src || document.querySelector('img.MMImage-Origin')?.src || ''");
            url = url.Trim('"');
            if (string.IsNullOrWhiteSpace(url) || url == "null")
            {
                TxtStatus.Text = "Click on an image to select it, then press ✓ Use";
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

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = new MemoryStream(data);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            ImgPreview.Source = bitmap;

            // Show preview, hide browser (Search again button to go back)
            ImageBrowser.Visibility = Visibility.Collapsed;
            PicturePreview.Visibility = Visibility.Visible;

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
            string imgId = "", audId = "";

            // Copy media to temp before adding (avoid file lock on source)
            if (!string.IsNullOrEmpty(_savedImagePath) && File.Exists(_savedImagePath))
            {
                string tmpImg = Path.Combine(Path.GetTempPath(), $"anki_img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jpg");
                File.Copy(_savedImagePath, tmpImg, overwrite: true);
                try { imgId = mgr.AddMedia(tmpImg); } finally { try { File.Delete(tmpImg); } catch { } }
            }

            if (!string.IsNullOrEmpty(_savedAudioPath) && File.Exists(_savedAudioPath))
            {
                string tmpAud = Path.Combine(Path.GetTempPath(), $"anki_aud_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.wav");
                File.Copy(_savedAudioPath, tmpAud, overwrite: true);
                try { audId = mgr.AddMedia(tmpAud); } finally { try { File.Delete(tmpAud); } catch { } }
            }

            mgr.AddNote(
                TxtEn.Text.Trim(),
                TxtTranscription.Text.Trim(),
                TxtRu.Text.Trim(),
                imgId,
                audId,
                TxtContext.Text.Trim()
            );

            string apkgPath = mgr.Finalize();
            TxtStatus.Text = $"Cards created!";
            MessageBox.Show($"Cards added to deck:\n{apkgPath}", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Failed to create cards:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e) => Close();
}
