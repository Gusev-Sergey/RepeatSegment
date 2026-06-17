using System;
using System.IO;
using System.Linq;
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

        _ = LookupIpaAsync();
        LoadDecks();
        InitializeWebViewAsync();
    }

    private async Task LookupIpaAsync()
    {
        string fullPhrase = _matchedWord ?? _selectedWord;
        if (string.IsNullOrWhiteSpace(fullPhrase)) return;
        string[] wordsToTry = fullPhrase.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (wordsToTry.Length == 0) return;
        var ipaParts = new System.Collections.Generic.List<string>();

        foreach (string w in wordsToTry)
        {
            string word = w.Trim();
            if (word.Length < 2) { ipaParts.Add(word); continue; }
            string? ipa = null;

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
                        for (int i = 0; i < phArr.GetArrayLength(); i++)
                            if (phArr[i].TryGetProperty("text", out var pt) && !string.IsNullOrWhiteSpace(pt.GetString())) { ipa = pt.GetString(); break; }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(ipa))
            {
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    string url = $"https://en.wiktionary.org/api/rest_v1/page/mobile-sections/{Uri.EscapeDataString(word.ToLower())}";
                    string json = await http.GetStringAsync(url);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("lead", out var lead))
                    {
                        string leadText = lead.TryGetProperty("sections", out var sections)
                            ? string.Join(" ", sections.EnumerateArray().Select(s => s.TryGetProperty("text", out var t) ? t.GetString() ?? "" : ""))
                            : "";
                        var match = System.Text.RegularExpressions.Regex.Match(leadText, @"/[^/]+/");
                        if (match.Success && match.Value.Length > 4) ipa = match.Value.Trim('/');
                    }
                }
                catch { }
            }
            ipaParts.Add(ipa ?? word);
        }
        string result = string.Join(" ", ipaParts);
        Dispatcher.Invoke(() => TxtTranscription.Text = result);
    }

    private async void InitializeWebViewAsync()
    {
        try
        {
            await ImageBrowser.EnsureCoreWebView2Async(null);
            ImageBrowser.DefaultBackgroundColor = System.Drawing.Color.White;
            // Inject image click tracker once, after every page load
            ImageBrowser.CoreWebView2.DOMContentLoaded += (_, _) =>
            {
                ImageBrowser.CoreWebView2.ExecuteScriptAsync(
                    "if(!window.___rsTrack){window.___rsTrack=1;document.addEventListener('click',function(e){var i=e.target.closest('img');if(i&&i.src)window.__img=i.src;});}");
            };
        }
        catch { TxtStatus.Text = "WebView2 init failed."; }
    }

    private void LoadDecks()
    {
        CmbDeck.Items.Clear();
        var decks = AnkiExportManager.ListDecks();
        foreach (var d in decks) CmbDeck.Items.Add(d);
        if (CmbDeck.Items.Count > 0)
        {
            // Pre-select last used deck
            string? last = AnkiExportManager.LastDeck;
            if (last != null)
            {
                int idx = decks.ToList().IndexOf(last);
                CmbDeck.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else CmbDeck.SelectedIndex = 0;
        }
        else CmbDeck.Items.Add("(no decks — create new)");
        if (CmbDeck.SelectedIndex < 0 && CmbDeck.Items.Count > 0)
            CmbDeck.SelectedIndex = 0;
    }

    private void BtnNewDeck_Click(object s, RoutedEventArgs e)
    {
        string? name = Microsoft.VisualBasic.Interaction.InputBox("Deck name:", "New Anki Deck", "");
        if (string.IsNullOrWhiteSpace(name)) return;
        CmbDeck.Items.Add(name);
        CmbDeck.SelectedItem = name;
    }

    private string? _savedMp3Path;

    private void BtnPlayAudio_Click(object s, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(_savedAudioPath) && File.Exists(_savedAudioPath))
            {
                new System.Media.SoundPlayer(_savedAudioPath).Play();
                TxtStatus.Text = "Playing...";
                return;
            }
            if (_audio == null) return;
            _savedAudioPath = _audio.SaveSnippetWav(_wordStart, _wordEnd);
            _savedMp3Path = _audio.SaveSnippetMp3(_wordStart, _wordEnd);
            TxtAudio.Text = _savedAudioPath;
            new System.Media.SoundPlayer(_savedAudioPath).Play();
            TxtStatus.Text = "Audio saved ✓";
        }
        catch (Exception ex) { TxtStatus.Text = $"Audio: {ex.Message}"; }
    }

    private void BtnSearchImages_Click(object s, RoutedEventArgs e)
    {
        string query = TxtSearchQuery.Text.Trim();
        if (string.IsNullOrWhiteSpace(query)) return;
        string searchUrl = $"https://yandex.ru/images/search?text={Uri.EscapeDataString(query)}";
        if (ImageBrowser.CoreWebView2 != null)
        {
            PicturePreview.Visibility = Visibility.Collapsed;
            ImageBrowser.Visibility = Visibility.Visible;
            ImageBrowser.CoreWebView2.Navigate(searchUrl);
            TxtStatus.Text = "Click desired image, then ✓ Use";
        }
        else TxtStatus.Text = "WebView2 not ready...";
    }

    private void BtnSearchAgain_Click(object s, RoutedEventArgs e)
    {
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
            string url = await ImageBrowser.CoreWebView2.ExecuteScriptAsync("window.__img || ''");
            url = url.Trim('"');
            if (string.IsNullOrWhiteSpace(url) || url == "null" || url.Length < 10)
            {
                TxtStatus.Text = "Click on an image first, then ✓ Use";
                return;
            }

            TxtStatus.Text = "Downloading...";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            byte[] data = await http.GetByteArrayAsync(url);

            using var bmp = SKBitmap.Decode(data);
            if (bmp != null)
            {
                int maxDim = 600, w = bmp.Width, h = bmp.Height;
                if (w > maxDim || h > maxDim)
                {
                    float scale = Math.Min((float)maxDim / w, (float)maxDim / h);
                    w = (int)(w * scale); h = (int)(h * scale);
                    using var resized = bmp.Resize(new SKImageInfo(w, h), SKFilterQuality.Medium);
                    using var image = SKImage.FromBitmap(resized);
                    using var data2 = image.Encode(SKEncodedImageFormat.Jpeg, 85);
                    data = data2.ToArray();
                }
                else { using var image = SKImage.FromBitmap(bmp); using var data2 = image.Encode(SKEncodedImageFormat.Jpeg, 85); data = data2.ToArray(); }
            }

            string mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "decks", "media");
            Directory.CreateDirectory(mediaDir);
            _savedImagePath = Path.Combine(mediaDir, $"img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jpg");
            File.WriteAllBytes(_savedImagePath, data);

            var bitmap = new BitmapImage();
            bitmap.BeginInit(); bitmap.StreamSource = new MemoryStream(data);
            bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.EndInit();
            ImgPreview.Source = bitmap;

            ImageBrowser.Visibility = Visibility.Collapsed;
            PicturePreview.Visibility = Visibility.Visible;
            TxtStatus.Text = "Image saved ✓";
        }
        catch (Exception ex) { TxtStatus.Text = $"Image: {ex.Message}"; }
    }

    private void BtnCreate_Click(object s, RoutedEventArgs e)
    {
        string deckName = CmbDeck.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(deckName) || deckName.StartsWith("(")) { TxtStatus.Text = "Select or create a deck first"; return; }

        try
        {
            var mgr = new AnkiExportManager(deckName);
            string imgId = "", audId = "";

            if (!string.IsNullOrEmpty(_savedImagePath) && File.Exists(_savedImagePath))
            {
                string tmpImg = Path.Combine(Path.GetTempPath(), $"anki_img_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jpg");
                File.Copy(_savedImagePath, tmpImg, overwrite: true);
                try { imgId = mgr.AddMedia(tmpImg); } finally { try { File.Delete(tmpImg); } catch { } }
            }

            // Use MP3 for Anki — convert WAV to MP3 if needed
            if (!string.IsNullOrEmpty(_savedAudioPath) && File.Exists(_savedAudioPath))
            {
                string audioToUse;
                if (!string.IsNullOrEmpty(_savedMp3Path) && File.Exists(_savedMp3Path))
                    audioToUse = _savedMp3Path;
                else if (Path.GetExtension(_savedAudioPath).ToLowerInvariant() == ".mp3")
                    audioToUse = _savedAudioPath;
                else
                {
                    // Convert WAV to MP3 on-the-fly
                    string mp3Path = Path.ChangeExtension(_savedAudioPath, ".mp3");
                    try
                    {
                        using var reader = new NAudio.Wave.WaveFileReader(_savedAudioPath);
                        using var writer = new NAudio.Lame.LameMP3FileWriter(mp3Path, reader.WaveFormat, 128);
                        reader.CopyTo(writer);
                        audioToUse = mp3Path;
                    }
                    catch { audioToUse = _savedAudioPath; } // fallback
                }
                string ext = Path.GetExtension(audioToUse);
                string tmpAud = Path.Combine(Path.GetTempPath(), $"anki_aud_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}");
                File.Copy(audioToUse, tmpAud, overwrite: true);
                try { audId = mgr.AddMedia(tmpAud); } finally { try { File.Delete(tmpAud); } catch { } }
            }

            mgr.AddNote(TxtEn.Text.Trim(), TxtTranscription.Text.Trim(), TxtRu.Text.Trim(), imgId, audId, TxtContext.Text.Trim());
            string apkgPath = mgr.Finalize();
            TxtStatus.Text = "Cards created!";
            MessageBox.Show($"Cards added to deck:\n{apkgPath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            string detail = ex.InnerException?.Message ?? ex.Message;
            TxtStatus.Text = $"Error: {detail}";
            MessageBox.Show($"Failed: {detail}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOpenDeck_Click(object s, RoutedEventArgs e)
    {
        string deckName = CmbDeck.SelectedItem?.ToString() ?? "";
        if (string.IsNullOrWhiteSpace(deckName) || deckName.StartsWith("(")) return;

        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "decks");
        string apkgPath = Path.Combine(dir, deckName + ".apkg");
        if (!File.Exists(apkgPath) && Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, deckName + "_*.apkg");
            if (files.Length > 0) apkgPath = files.OrderByDescending(File.GetLastWriteTime).First();
        }

        if (File.Exists(apkgPath))
            try { System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{apkgPath}\""); }
            catch { TxtStatus.Text = "Could not open folder"; }
        else TxtStatus.Text = "Deck file not found";
    }

    private NAudio.Wave.WaveInEvent? _recorder;
    private NAudio.Wave.WaveFileWriter? _recorderWriter;
    private async void BtnRecordAudio_Click(object s, RoutedEventArgs e)
    {
        if (_recorder != null)
        {
            try { _recorder.StopRecording(); } catch { }
            _recorder?.Dispose(); _recorder = null;
            _recorderWriter?.Dispose(); _recorderWriter = null;
            BtnRecordAudio.Content = "Rec";
            ConvertRecordedToMp3();
            TxtStatus.Text = "Recording saved ✓";
            return;
        }

        try
        {
            string mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "decks", "media");
            Directory.CreateDirectory(mediaDir);
            _savedAudioPath = Path.Combine(mediaDir, $"rec_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.wav");

            _recorder = new NAudio.Wave.WaveInEvent { WaveFormat = new NAudio.Wave.WaveFormat(44100, 16, 1) };
            _recorderWriter = new NAudio.Wave.WaveFileWriter(_savedAudioPath, _recorder.WaveFormat);
            _recorder.DataAvailable += (_, args) => { if (_recorderWriter != null && args.BytesRecorded > 0) _recorderWriter.Write(args.Buffer, 0, args.BytesRecorded); };
            _recorder.RecordingStopped += (_, _) =>
            {
                _recorderWriter?.Dispose(); _recorderWriter = null;
                _recorder?.Dispose(); _recorder = null;
                Dispatcher.Invoke(() => { BtnRecordAudio.Content = "Rec"; ConvertRecordedToMp3(); TxtAudio.Text = _savedAudioPath ?? ""; });
            };

            _recorder.StartRecording();
            BtnRecordAudio.Content = "Stop";
            TxtAudio.Text = "Recording...";
            TxtStatus.Text = "Recording... press Stop when done";

            await Task.Delay(15000);
            if (_recorder != null) try { _recorder.StopRecording(); } catch { }
        }
        catch (Exception ex) { TxtStatus.Text = $"Record: {ex.Message}"; _recorder?.Dispose(); _recorder = null; BtnRecordAudio.Content = "Rec"; }
    }

    private void ConvertRecordedToMp3()
    {
        try
        {
            if (string.IsNullOrEmpty(_savedAudioPath) || !File.Exists(_savedAudioPath)) return;
            string mp3Path = Path.ChangeExtension(_savedAudioPath, ".mp3");
            using var reader = new NAudio.Wave.WaveFileReader(_savedAudioPath);
            using var writer = new NAudio.Lame.LameMP3FileWriter(mp3Path, reader.WaveFormat, 128);
            reader.CopyTo(writer);
            _savedMp3Path = mp3Path;
        }
        catch { /* MP3 conversion failed — will fall back to WAV */ }
    }

    private void BtnCancel_Click(object s, RoutedEventArgs e) => Close();
}
