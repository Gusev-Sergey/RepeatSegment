using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace RepeatSegment.App;

public partial class MainWindow : Window
{
    // ── DWM dark title bar ──────────────────────────────────────────
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    private void SetDarkTitleBar(bool dark)
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
        int useDark = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
    }

    private AudioEngine _audio = null!;
    private SilenceDetector _silenceDetector = null!;
    private TranscriptionProvider? _transcriptionProvider;
    private ConfigManager _config = null!;
    private TranslationProvider? _translationProvider;
    private TtsProvider? _ttsProvider;
    private SettingsWindow? _settingsWindow;
    private BitmapImage? _iconFirst, _iconLast, _iconPrePlay, _iconNextPlay, _iconPlay, _iconRepeat, _iconPlayGo, _iconStopPlay;
    private bool _pop = true, _pte, _repeatSegment, _playGoMode, _isDarkTheme;
    private double _positionSeconds, _durationSeconds, _dt, _t1, _t2; private int _counter; private DateTime _playStartTime;
    private List<(double T1, double T2)> _fragments = new();
    private DispatcherTimer? _positionTimer; private const int TMR_INTERVAL = 33;
    private DateTime _lastSave = DateTime.Now; private const int SAVE_INTERVAL = 10;
    private bool _sliderDragInProgress, _btnBlocked;
    private string _lastFilePath = "";
    private Run[]? _wordRuns; private Run[]? _spaceRuns; private int _lastHlIdx = -1; private int _prevWordCount;

    private static string AppDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment");
    public MainWindow() { InitializeComponent(); SetWindowIcon(); LoadIcons(); ApplyInitialSkins(); _positionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(TMR_INTERVAL) }; _positionTimer.Tick += PositionTimer_Tick; AdaptToScreen(); ApplyTheme(false); Closed += (_, _) => SaveState(); Loaded += (_, _) => { LoadOnStartup(); ApplyAllStrings(); }; WaveformGraph.SegmentSelected += WaveformGraph_SegmentSelected; }
    private void SetWindowIcon()
    {
        string ico = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons", "app.ico");
        if (File.Exists(ico))
        {
            try { Icon = new BitmapImage(new Uri(ico, UriKind.Absolute)); }
            catch { }
        }
    }
    private void LoadOnStartup() { Directory.CreateDirectory(AppDataDir); MigrateConfigFromExe(); _config = new ConfigManager(AppDataDir); _config.Load(); if (!string.IsNullOrEmpty(_config.FileName)) { string sp = Path.Combine(_config.Path, _config.FileName); if (File.Exists(sp)) { _lastFilePath = sp; LoadAudioFile(sp); return; } } _t1 = 0; _t2 = 0; TxtStatus.Text = "Use File > Load"; }
    private static void MigrateConfigFromExe() { string src = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini"); string dst = Path.Combine(AppDataDir, "config.ini"); if (File.Exists(src) && !File.Exists(dst)) { try { File.Copy(src, dst); } catch { } } if (!File.Exists(dst)) { try { File.WriteAllText(dst, "[Settings]\npath = \nfile = \nposition = 0\ncounter = 0\nsplit_interval = 100\nlanguage = \n\n[Transcription]\nproviders_enabled = deepgram\nvkcloud_token = \nyandex_api_key = \nyandex_folder_id = \nsalute_client_id = \nsalute_client_secret = \nsalute_scope = SALUTE_SPEECH_PERS\nsalute_lang = ru-RU\nsalute_profile_id = \nassemblyai_api_key = \ndeepgram_api_key = \nchunk_minutes = 10\nplayback_latency = 0.32\n"); } catch { } } }
    private void LoadIcons() { string d = FindIconsDir(); _iconFirst = LoadIcon(d + "\\first.png"); _iconLast = LoadIcon(d + "\\last.png"); _iconPrePlay = LoadIcon(d + "\\pre_play.png"); _iconNextPlay = LoadIcon(d + "\\next_play.png"); _iconPlay = LoadIcon(d + "\\play.png"); _iconRepeat = LoadIcon(d + "\\repeat.png"); _iconPlayGo = LoadIcon(d + "\\play_go.png"); _iconStopPlay = LoadIcon(d + "\\stop_play.png"); }
    private static string FindIconsDir() { string d = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons"); if (Directory.Exists(d)) return d; d = Path.Combine(Directory.GetCurrentDirectory(), "Icons"); if (Directory.Exists(d)) return d; return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Icons"); }
    private static BitmapImage? LoadIcon(string p) { if (!File.Exists(p)) return null; try { var b = new BitmapImage(); b.BeginInit(); b.CacheOption = BitmapCacheOption.OnLoad; b.UriSource = new Uri(p); b.EndInit(); b.Freeze(); return b; } catch { return null; } }
    private void ApplyInitialSkins() { ImgFirst.Source = _iconFirst; ImgLast.Source = _iconLast; ImgPrev.Source = _iconPrePlay; ImgNext.Source = _iconNextPlay; ImgPlayPause.Source = _iconPlay; ImgRepeat.Source = _iconRepeat; ImgPlayGo.Source = _iconPlayGo; SetAllButtonsEnabled(false); SliderPosition.IsEnabled = false; }
    private void SetAllButtonsEnabled(bool e) { ButtonFirst.IsEnabled = ButtonLast.IsEnabled = ButtonPrevSegment.IsEnabled = ButtonNextSegment.IsEnabled = ButtonPlay.IsEnabled = ButtonRepeat.IsEnabled = ButtonPlayGo.IsEnabled = e; }

    public void LoadAudioFile(string fp)
    {
        try
        {
            _audio?.Stop(); _audio?.Dispose(); _audio = new AudioEngine();
            if (!_audio.Load(fp)) { MessageBox.Show("Failed to load", "Error"); return; }
            _durationSeconds = _audio.Duration.TotalSeconds; _positionSeconds = 0; _counter = 0; _fragments.Clear(); _pop = true; _pte = false; _playGoMode = false; _dt = 0; _lastHlIdx = -1; _prevWordCount = 0; _wordRuns = null; _spaceRuns = null;
            if (_config == null) _config = new ConfigManager(Path.GetDirectoryName(fp)!); else _config.Path = Path.GetDirectoryName(fp)!; _config.Load();
            if (_config.FileName == Path.GetFileName(fp) && _config.Position > 0 && _config.Position < _durationSeconds) { _positionSeconds = _config.Position; _counter = _config.Counter; }
            _silenceDetector = new SilenceDetector { MinSilenceLenMs = _config.MinSilenceLenMs > 0 ? _config.MinSilenceLenMs : 300 };
            var s = _audio.GetSamples(); if (_silenceDetector.Detect(s ?? Array.Empty<float>(), _audio.SampleRate, _durationSeconds)) _fragments = _silenceDetector.T1T2Array.ToList();
            if (_fragments.Count > 0) { if (_counter >= _fragments.Count) _counter = 0; _t1 = _fragments[_counter].T1; _t2 = _fragments[_counter].T2; _positionSeconds = _t1; } else { _t1 = 0; _t2 = _durationSeconds; }
            _transcriptionProvider = new TranscriptionProvider(_config, _audio);
            _translationProvider = new TranslationProvider(_config.YandexTranslateApiKey, _config.YandexTranslateFolderId, _config.TranslationProviderPreference);
            _ttsProvider = new TtsProvider(_config.DeepgramApiKey);
            _transcriptionProvider.StatusChanged += msg => Dispatcher.Invoke(() => TxtStatus.Text = msg);
            SliderPosition.Minimum = 0; SliderPosition.Maximum = _durationSeconds; SliderPosition.Value = _positionSeconds;
            LabelPosition.Text = FormatTime(_positionSeconds); LabelDuration.Text = FormatTime(_durationSeconds);
            TxtStatus.Text = Path.GetFileName(fp); SetAllButtonsEnabled(true); SliderPosition.IsEnabled = true; UpdateButtonSkins(); UpdateWaveform();
            TextTranscription.Document = new FlowDocument(new Paragraph(new Run("Transcription not loaded. Use Transcription menu.") { Foreground = Brushes.Gray }));
        }
        catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error"); }
    }

    // ── Transcription ──────────────────────────────────────────────
    private void BuildWordsParagraph()
    {
        var words = _transcriptionProvider?.WordTimings; if (words == null || words.Count == 0) return;
        var para = new Paragraph(); var fg = Resources["TextBrush"] as SolidColorBrush ?? Brushes.Black;
        _spaceRuns = new Run[words.Count]; _wordRuns = new Run[words.Count];
        for (int i = 0; i < words.Count; i++) { if (i > 0) { var sr = new Run(" ") { Foreground = fg }; para.Inlines.Add(sr); _spaceRuns[i] = sr; } var wr = new Run(words[i].Word) { Foreground = fg }; para.Inlines.Add(wr); _wordRuns[i] = wr; }
        TextTranscription.Document = new FlowDocument(para); _prevWordCount = words.Count;
    }
    private void AppendNewWords()
    {
        var words = _transcriptionProvider?.WordTimings; var doc = TextTranscription.Document;
        if (words == null || doc == null || doc.Blocks.FirstBlock is not Paragraph para) return;
        var fg = Resources["TextBrush"] as SolidColorBrush ?? Brushes.Black;
        if (_wordRuns == null || _spaceRuns == null) { BuildWordsParagraph(); return; }
        if (_wordRuns.Length < words.Count) { Array.Resize(ref _spaceRuns, words.Count); Array.Resize(ref _wordRuns, words.Count); }
        while (_prevWordCount < words.Count) { int i = _prevWordCount; if (i > 0) { var sr = new Run(" ") { Foreground = fg }; para.Inlines.Add(sr); _spaceRuns[i] = sr; } var wr = new Run(words[i].Word) { Foreground = fg }; para.Inlines.Add(wr); _wordRuns[i] = wr; _prevWordCount++; }
    }
    private void HighlightActiveWord()
    {
        if (_transcriptionProvider == null) return; var words = _transcriptionProvider.WordTimings; if (words.Count == 0) return;
        if (_wordRuns == null) BuildWordsParagraph(); AppendNewWords();
        double chunkLen = (_config?.ChunkMinutes ?? 10) * 60.0; int neededChunk = (int)(_positionSeconds / chunkLen) + 1; int maxChunks = (int)(_audio.Duration.TotalSeconds / chunkLen) + 1;
        for (int ci = 1; ci <= neededChunk && ci <= maxChunks; ci++) _transcriptionProvider.EnsureChunk(ci);
        int idx = _lastHlIdx >= 0 && _lastHlIdx < words.Count ? _lastHlIdx : 0;
        while (idx < words.Count && words[idx].End <= _positionSeconds) idx++;
        if (idx < words.Count && _positionSeconds < words[idx].Start) idx = idx > 0 ? idx - 1 : -1;
        if (idx >= words.Count) idx = words.Count - 1;
        if (idx < 0 || idx == _lastHlIdx) return;
        if (_lastHlIdx >= 0 && _lastHlIdx < (_wordRuns?.Length ?? 0) && _wordRuns![_lastHlIdx] != null) { _wordRuns[_lastHlIdx].Background = null; _wordRuns[_lastHlIdx].Foreground = Resources["TextBrush"] as SolidColorBrush ?? Brushes.Black; _wordRuns[_lastHlIdx].FontWeight = FontWeights.Normal; }
        if (idx < (_wordRuns?.Length ?? 0) && _wordRuns![idx] != null) { var cur = _wordRuns[idx]; cur.Background = new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)); cur.Foreground = Brushes.White; cur.FontWeight = FontWeights.Bold; TextScrollViewer.ScrollToVerticalOffset(TextScrollViewer.VerticalOffset + (cur.ElementStart.GetCharacterRect(LogicalDirection.Forward).Top - TextScrollViewer.ViewportHeight / 2 - TextScrollViewer.VerticalOffset)); }
        _lastHlIdx = idx;
    }

    private async void BtnTranscribeCache_Click(object s, RoutedEventArgs e)
    {
        if (_transcriptionProvider == null || _audio == null) { MessageBox.Show("Load audio file first."); return; }
        TranscribeOverlay.Visibility = Visibility.Visible; TranscribeStatus.Text = "Loading from cache..."; TxtStatus.Text = "Loading from cache...";
        _wordRuns = null; _spaceRuns = null; _lastHlIdx = -1; _prevWordCount = 0;
        try { var r = await _transcriptionProvider.Transcribe(_audio.FilePath, false); int wc = _transcriptionProvider.WordTimings.Count; if (wc == 0) { TxtStatus.Text = "No cache"; TextTranscription.Document = new FlowDocument(new Paragraph(new Run("No cached data. Use Request from API.") { Foreground = Brushes.Gray })); } else { BuildWordsParagraph(); TxtStatus.Text = $"Loaded — {wc} words"; } }
        catch (Exception ex) { TxtStatus.Text = $"Error: {ex.Message}"; }
        finally { TranscribeOverlay.Visibility = Visibility.Collapsed; }
    }
    private async void BtnTranscribeApi_Click(object s, RoutedEventArgs e)
    {
        if (_transcriptionProvider == null || _audio == null) { MessageBox.Show("Load audio file first."); return; }
        TranscribeOverlay.Visibility = Visibility.Visible; TranscribeStatus.Text = "Calling API..."; TxtStatus.Text = "Transcribing...";
        _wordRuns = null; _spaceRuns = null; _lastHlIdx = -1; _prevWordCount = 0;
        try { var result = await _transcriptionProvider.Transcribe(_audio.FilePath, true); int wc = _transcriptionProvider.WordTimings.Count; if (wc == 0) { TxtStatus.Text = "0 words"; TextTranscription.Document = new FlowDocument(new Paragraph(new Run("API returned 0 words.") { Foreground = Brushes.Red })); } else { BuildWordsParagraph(); TxtStatus.Text = $"Done — {wc} words"; } }
        catch (Exception ex) { TxtStatus.Text = $"Error: {ex.Message}"; }
        finally { TranscribeOverlay.Visibility = Visibility.Collapsed; }
    }

    // PATCH: old handler kept for keyboard shortcut compatibility
    private void BtnTranscribe_Click(object s, RoutedEventArgs e) => BtnTranscribeApi_Click(s, e);

    private void UpdateSkinsPlaying() { ImgPlayPause.Source = _iconStopPlay; ImgRepeat.Source = _iconStopPlay; ImgPlayGo.Source = _iconStopPlay; }
    private void UpdateSkinsStopped() { ImgPlayPause.Source = _iconPlay; ImgRepeat.Source = _iconRepeat; ImgPlayGo.Source = _iconPlayGo; }
    private void UpdateButtonSkins() { if (!_pop) UpdateSkinsPlaying(); else UpdateSkinsStopped(); }
    private void ButtonsPressed(string key) { if (_btnBlocked || _audio?.Samples == null) return; _btnBlocked = true; try { if (key is "preplay" or "next_play" or "first" or "last") { _sliderDragInProgress = true; try { SkipToFragment(key); } finally { _sliderDragInProgress = false; } _btnBlocked = false; return; } if (_pop) { _pte = (key == "play"); _playGoMode = (key == "play_go"); _repeatSegment = false; SkipToFragment(key, keepPte: (key == "play" || key == "replay")); UpdateSkinsPlaying(); _pop = false; StartPlayback(); } else { StopPlaybackInternal(); UpdateSkinsStopped(); _pop = true; _pte = false; _playGoMode = false; _repeatSegment = false; if (_positionSeconds > _t2) _positionSeconds = _t2; for (int i = 0; i < _fragments.Count; i++) if (_fragments[i].T1 <= _positionSeconds && _positionSeconds <= _fragments[i].T2) { _counter = i; _t1 = _fragments[i].T1; _t2 = _fragments[i].T2; break; } UpdateGraphView(); } } finally { _btnBlocked = false; } }
    private void SkipToFragment(string key, bool keepPte = false) { _audio?.Pause(); _positionTimer?.Stop(); if (!keepPte) _pte = false; if (_fragments.Count == 0) return; if (key != "replay" && key != "play") { switch (key) { case "preplay": if (_positionSeconds > 0) _counter--; break; case "next_play": _counter++; break; case "play_go": if (_positionSeconds > 0) _counter++; break; case "first": _counter = 0; break; case "last": _counter = _fragments.Count - 1; break; } } _counter = Math.Max(0, Math.Min(_counter, _fragments.Count - 1)); _t1 = _fragments[_counter].T1; _t2 = _fragments[_counter].T2; _positionSeconds = _t1; SliderPosition.Value = _positionSeconds; LabelPosition.Text = FormatTime(_positionSeconds); UpdateGraphView(); }
    private void StartPlayback() { var ss = _pte ? _audio.GetPlaySamplesToEnd(_t1) : _audio.GetPlaySamples(_t1, _t2); if (ss == null) return; _audio.Stop(); _audio.PlaySegment(ss); _playStartTime = DateTime.Now; _dt = 0; _positionTimer?.Stop(); _positionTimer?.Start(); }
    private void StopPlaybackInternal() { _audio?.Stop(); _positionTimer?.Stop(); }
    private void PositionTimer_Tick(object? s, EventArgs e) { if (_audio == null || _audio.IsDisposed) return; try { _positionSeconds = _t1 + _dt + (DateTime.Now - _playStartTime).TotalSeconds; if (!_pte && _positionSeconds >= _t2) { _positionSeconds = _t2; SliderPosition.Value = _positionSeconds; LabelPosition.Text = FormatTime(_positionSeconds); StopPlaybackInternal(); UpdateSkinsStopped(); _pop = true; UpdateGraphView(); SaveState(); return; } if (_pte && _positionSeconds >= _t2) { if (_counter + 1 < _fragments.Count) { _counter++; _t1 = _fragments[_counter].T1; _t2 = _fragments[_counter].T2; _playStartTime = DateTime.Now; _dt = 0; } else { StopPlaybackInternal(); UpdateSkinsStopped(); _pop = true; _positionSeconds = _durationSeconds; SliderPosition.Value = _positionSeconds; LabelPosition.Text = FormatTime(_positionSeconds); SaveState(); return; } } if (!_sliderDragInProgress) SliderPosition.Value = _positionSeconds; LabelPosition.Text = FormatTime(_positionSeconds); UpdateGraphView(); HighlightActiveWord(); if ((DateTime.Now - _lastSave).TotalSeconds >= SAVE_INTERVAL) { SaveState(); _lastSave = DateTime.Now; } } catch { } }
    private void UpdateGraphView() => UpdateWaveform();
    private void SaveState() { if (_config == null || _audio == null) return; try { _config.Save(Path.GetDirectoryName(_audio.FilePath)!, Path.GetFileName(_audio.FilePath), _positionSeconds, _counter); } catch { } }
    private void ButtonFirst_Click(object s, RoutedEventArgs e) => ButtonsPressed("first");
    private void ButtonPrevSegment_Click(object s, RoutedEventArgs e) => ButtonsPressed("preplay");
    private void ButtonRepeat_Click(object s, RoutedEventArgs e) => ButtonsPressed("replay");
    private void ButtonPlayGo_Click(object s, RoutedEventArgs e) => ButtonsPressed("play_go");
    private void ButtonPlay_Click(object s, RoutedEventArgs e) => ButtonsPressed("play");
    private void ButtonNextSegment_Click(object s, RoutedEventArgs e) => ButtonsPressed("next_play");
    private void ButtonLast_Click(object s, RoutedEventArgs e) => ButtonsPressed("last");
    private void Slider_DragStarted(object s, System.Windows.Controls.Primitives.DragStartedEventArgs e) => _sliderDragInProgress = true;
    private void Slider_DragCompleted(object s, System.Windows.Controls.Primitives.DragCompletedEventArgs e) { _sliderDragInProgress = false; var sv = SliderPosition.Value; for (int i = 0; i < _fragments.Count; i++) if (_fragments[i].T1 <= sv && sv < _fragments[i].T2) { _counter = i; _t1 = _fragments[i].T1; _t2 = _fragments[i].T2; _positionSeconds = sv; if (!_pop) { StopPlaybackInternal(); StartPlayback(); } UpdateGraphView(); LabelPosition.Text = FormatTime(_positionSeconds); return; } if (_fragments.Count > 0) { _counter = _fragments.Count - 1; _t1 = _fragments[_counter].T1; _t2 = _fragments[_counter].T2; } _positionSeconds = sv; LabelPosition.Text = FormatTime(_positionSeconds); UpdateGraphView(); }
    private void Slider_ValueChanged(object s, RoutedPropertyChangedEventArgs<double> e) { if (_sliderDragInProgress) { _positionSeconds = e.NewValue; LabelPosition.Text = FormatTime(_positionSeconds); } }
    private void Window_KeyDown(object s, KeyEventArgs e) { switch (e.Key) { case Key.Space: ButtonsPressed((Keyboard.Modifiers & ModifierKeys.Control) != 0 ? "play_go" : "play"); e.Handled = true; break; case Key.Escape: ButtonsPressed("play"); e.Handled = true; break; case Key.Left: ButtonsPressed("preplay"); e.Handled = true; break; case Key.Right: ButtonsPressed("next_play"); e.Handled = true; break; case Key.M: ButtonRepeat_Click(s, e); e.Handled = true; break; case Key.Home: ButtonsPressed("first"); e.Handled = true; break; case Key.End: ButtonsPressed("last"); e.Handled = true; break; case Key.L: if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) { BtnTranscribeCache_Click(s, e); e.Handled = true; } break; case Key.T: if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) { BtnTranscribeApi_Click(s, e); e.Handled = true; } break; } }
    private void BtnLoad_Click(object s, RoutedEventArgs e) { string d; if (!string.IsNullOrEmpty(_config?.Path) && Directory.Exists(_config.Path)) d = _config.Path; else if (!string.IsNullOrEmpty(_lastFilePath) && Directory.Exists(Path.GetDirectoryName(_lastFilePath))) d = Path.GetDirectoryName(_lastFilePath)!; else d = Path.GetPathRoot(Environment.SystemDirectory)!; var dlg = new Microsoft.Win32.OpenFileDialog { Title = "Select audio file", Filter = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*", InitialDirectory = d }; if (dlg.ShowDialog(this) == true) { _lastFilePath = dlg.FileName; LoadAudioFile(dlg.FileName); } }
    private void BtnExit_Click(object s, RoutedEventArgs e) => Close();
    private void MnuSilence_Click(object s, RoutedEventArgs e) { if (s is MenuItem mi && mi.Tag is string t && int.TryParse(t, out int ms)) UpdateMinSilenceLen(ms); }
    private void MnuSilenceCustom_Click(object s, RoutedEventArgs e) { string? i = Microsoft.VisualBasic.Interaction.InputBox("Enter silence length (ms):", "Custom", "500"); if (int.TryParse(i, out int ms) && ms >= 50 && ms <= 5000) UpdateMinSilenceLen(ms); else if (!string.IsNullOrEmpty(i)) MessageBox.Show("Value 50-5000 ms.", "Invalid"); }
    public void UpdateMinSilenceLen(int ms) { if (_config == null || _audio == null) return; _config.MinSilenceLenMs = ms; _silenceDetector!.MinSilenceLenMs = ms; var s = _audio.GetSamples(); if (s != null && _silenceDetector.Detect(s, _audio.SampleRate, _durationSeconds)) { _fragments = _silenceDetector.T1T2Array.ToList(); if (_fragments.Count > 0) { _counter = -1; for (int i = 0; i < _fragments.Count; i++) if (_fragments[i].T1 <= _positionSeconds && _positionSeconds <= _fragments[i].T2) { _counter = i; _t1 = _fragments[i].T1; _t2 = _fragments[i].T2; break; } if (_counter < 0) { _counter = 0; _t1 = _fragments[0].T1; _t2 = _fragments[0].T2; } } UpdateWaveform(); ButtonPrevSegment.IsEnabled = ButtonNextSegment.IsEnabled = ButtonFirst.IsEnabled = ButtonLast.IsEnabled = _fragments.Count > 1 || _audio != null; } foreach (var obj in MnuSplitInterval.Items) if (obj is MenuItem item && item.Tag is string tag && int.TryParse(tag, out int v)) item.IsChecked = (v == ms); }

    private void WaveformGraph_SegmentSelected(double t1, double t2)
    {
        if (_audio == null || _fragments == null) return;

        if (t2 - t1 < 0.5 || t1 >= _durationSeconds) return;
        if (t2 > _durationSeconds) t2 = _durationSeconds;

        // Merge: trim overlapping fragments, drop fully inner ones, add new segment
        var newFrags = new List<(double, double)>();
        foreach (var f in _fragments)
        {
            if (f.Item2 <= t1) newFrags.Add(f);       // left of range — keep as is
            else if (f.Item1 >= t2) newFrags.Add(f);   // right of range — keep as is
            else if (f.Item1 < t1 && f.Item2 > t2)     // overlaps entire range — split
            {
                if (t1 - f.Item1 >= 0.5) newFrags.Add((f.Item1, t1));
                if (f.Item2 - t2 >= 0.5) newFrags.Add((t2, f.Item2));
            }
            else if (f.Item1 < t1) newFrags.Add((f.Item1, t1)); // overlaps left edge — trim right
            else if (f.Item2 > t2) newFrags.Add((t2, f.Item2)); // overlaps right edge — trim left
            // else: fully inside — dropped
        }
        newFrags.Add((t1, t2));
        newFrags.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        _fragments = newFrags;
        _positionSeconds = t1;
        _t1 = t1;
        _t2 = t2;
        _counter = _fragments.FindIndex(f => f.Item1 == t1 && f.Item2 == t2);
        if (_counter < 0) _counter = 0;

        SliderPosition.Value = _positionSeconds;
        LabelPosition.Text = FormatTime(_positionSeconds);
        UpdateWaveform();
        ButtonPrevSegment.IsEnabled = ButtonNextSegment.IsEnabled = ButtonFirst.IsEnabled = ButtonLast.IsEnabled = _fragments.Count > 1;
    }
    private void MnuThemeLight_Click(object s, RoutedEventArgs e) => ApplyTheme(false);
    private void MnuThemeDark_Click(object s, RoutedEventArgs e) => ApplyTheme(true);
    public bool IsDarkTheme => _isDarkTheme;
    private void ApplyTheme(bool dark) { _isDarkTheme = dark; SetDarkTitleBar(dark); if (WaveformGraph != null) WaveformGraph.IsDarkTheme = dark; if (VolumeControl != null) { VolumeControl.BarColor = System.Drawing.Color.FromArgb(0x56, 0xB4, 0xE9); VolumeControl.BgBarColor = dark ? System.Drawing.Color.FromArgb(0x4A, 0x4A, 0x4A) : System.Drawing.Color.FromArgb(0xDD, 0xDD, 0xDD); } var c = dark ? new Dictionary<string, Color> { ["WindowBackgroundBrush"] = Color.FromRgb(0x1E, 0x1E, 0x1E), ["PanelBackgroundBrush"] = Color.FromRgb(0x2D, 0x2D, 0x2D), ["TextBrush"] = Color.FromRgb(0xE0, 0xE0, 0xE0), ["TextDimBrush"] = Color.FromRgb(0x88, 0x88, 0x88), ["WaveformBgBrush"] = Color.FromRgb(0x25, 0x25, 0x25), ["SliderAccentBrush"] = Color.FromRgb(0x56, 0xB4, 0xE9), ["ButtonBgBrush"] = Color.FromRgb(0x3A, 0x3A, 0x3A), ["ButtonHoverBrush"] = Color.FromRgb(0x4A, 0x4A, 0x4A), ["ButtonPressedBrush"] = Color.FromRgb(0x5A, 0x5A, 0x5A), ["ButtonBorderBrush"] = Color.FromRgb(0x55, 0x55, 0x55), ["MenuBackgroundBrush"] = Color.FromRgb(0x2D, 0x2D, 0x2D), ["MenuTextBrush"] = Color.FromRgb(0xE0, 0xE0, 0xE0), ["MenuDropBgBrush"] = Color.FromRgb(0x33, 0x33, 0x33), ["StatusBarBrush"] = Color.FromRgb(0x1E, 0x1E, 0x1E), ["SeparatorBrush"] = Color.FromRgb(0x44, 0x44, 0x44), ["VolumeBarBrush"] = Color.FromRgb(0x56, 0xB4, 0xE9), ["VolumeBarBgBrush"] = Color.FromRgb(0x4A, 0x4A, 0x4A), ["InputBgBrush"] = Color.FromRgb(0x2D, 0x2D, 0x2D), ["InputBorderBrush"] = Color.FromRgb(0x55, 0x55, 0x55) } : new Dictionary<string, Color> { ["WindowBackgroundBrush"] = Color.FromRgb(0xF0, 0xF2, 0xF5), ["PanelBackgroundBrush"] = Color.FromRgb(0xFF, 0xFF, 0xFF), ["TextBrush"] = Color.FromRgb(0x22, 0x22, 0x22), ["TextDimBrush"] = Color.FromRgb(0x66, 0x66, 0x66), ["WaveformBgBrush"] = Color.FromRgb(0xE8, 0xEC, 0xF0), ["SliderAccentBrush"] = Color.FromRgb(0x33, 0x66, 0xCC), ["ButtonBgBrush"] = Color.FromRgb(0xE0, 0xE0, 0xE0), ["ButtonHoverBrush"] = Color.FromRgb(0xD0, 0xD0, 0xD0), ["ButtonPressedBrush"] = Color.FromRgb(0xC0, 0xC0, 0xC0), ["ButtonBorderBrush"] = Color.FromRgb(0x99, 0x99, 0x99), ["MenuBackgroundBrush"] = Color.FromRgb(0xEE, 0xEE, 0xEE), ["MenuTextBrush"] = Color.FromRgb(0x22, 0x22, 0x22), ["MenuDropBgBrush"] = Color.FromRgb(0xF8, 0xF8, 0xF8), ["StatusBarBrush"] = Color.FromRgb(0xE0, 0xE0, 0xE0), ["SeparatorBrush"] = Color.FromRgb(0xBB, 0xBB, 0xBB), ["VolumeBarBrush"] = Color.FromRgb(0x56, 0xB4, 0xE9), ["VolumeBarBgBrush"] = Color.FromRgb(0xDD, 0xDD, 0xDD), ["InputBgBrush"] = Color.FromRgb(0xFF, 0xFF, 0xFF), ["InputBorderBrush"] = Color.FromRgb(0x99, 0x99, 0x99) }; foreach (var kv in c) Resources[kv.Key] = new SolidColorBrush(kv.Value); MnuThemeLight.IsChecked = !dark; MnuThemeDark.IsChecked = dark; if (_wordRuns != null && _transcriptionProvider?.WordTimings?.Count > 0) BuildWordsParagraph(); }
    private CancellationTokenSource? _translateCts;
    private double _originalHeight;
    private async void TextTranscription_SelectionChanged(object s, RoutedEventArgs e)
    {
        if (_translationProvider == null || _audio == null) return;
        var sel = TextTranscription.Selection;
        if (sel == null || sel.IsEmpty)
        {
            // Hide translation and restore original window height
            _translateCts?.Cancel();
            TranslationPanel.Visibility = Visibility.Collapsed;
            LayoutRoot.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
            LayoutRoot.RowDefinitions[5].Height = GridLength.Auto;
            if (_originalHeight > 0 && ActualHeight != _originalHeight)
                Height = _originalHeight;
            return;
        }
        var range = new TextRange(sel.Start, sel.End);
        string text = range.Text.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return;

        // Cancel previous pending translation
        _translateCts?.Cancel();
        var cts = new CancellationTokenSource();
        _translateCts = cts;

        try
        {
            await Task.Delay(600, cts.Token);
            if (!cts.IsCancellationRequested)
            {
                var sel2 = TextTranscription.Selection;
                if (sel2 == null || sel2.IsEmpty) return;
                var range2 = new TextRange(sel2.Start, sel2.End);
                string text2 = range2.Text.Trim();
                if (text2 == text) await DoTranslateSelection();
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task DoTranslateSelection()
    {
        var sel = TextTranscription.Selection;
        if (sel == null || sel.IsEmpty) return;

        // Use TextRange for reliable multi-paragraph selection text
        var range = new TextRange(sel.Start, sel.End);
        string text = range.Text.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return;
        if (_translationProvider == null) return;

        TxtTranslationOriginal.Text = text;
        TxtTranslationResult.Text = "Translating...";
        TranslationPanel.Visibility = Visibility.Visible;

        // Row 5 = Auto so panel sizes to its natural content height
        LayoutRoot.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
        LayoutRoot.RowDefinitions[5].Height = GridLength.Auto;

        // Ensure minimum growth so panel is visible + border is shown
        double minGrowth = Math.Max(0, 80 - LayoutRoot.RowDefinitions[5].ActualHeight);
        if (minGrowth > 0) Height += minGrowth;

        string result = await _translationProvider.TranslateEnRu(text);
        TxtTranslationResult.Text = result;
        TxtStatus.Text = _translationProvider.LastUsedProvider != null
            ? $"Translation ready (via {_translationProvider.LastUsedProvider})"
            : "Translation ready";

        _ = Dispatcher.InvokeAsync(() => GrowWindowForTranslation(), DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Grow window so translation panel fits without clipping.
    /// Falls back to ScrollViewer if window reaches 90% screen height.
    /// </summary>
    private void GrowWindowForTranslation()
    {
        // Multiple passes needed: first pass sizes the panel, second pass computes overflow.
        UpdateLayout();

        // Read panel's natural height after Row 5 = Auto layout
        double panelH = TranslationPanel.ActualHeight;
        if (panelH <= 0) return; // not yet laid out

        // Find where panel bottom lands relative to window
        var pt = TranslationPanel.TranslatePoint(new Point(0, panelH), this);
        double panelBottom = pt.Y;
        double statusH = LayoutRoot.RowDefinitions[6].ActualHeight;
        if (statusH <= 0) statusH = 28;

        double contentBottom = panelBottom + statusH;
        double overflow = contentBottom - ActualHeight;

        if (overflow <= 6) return; // fits fine

        double maxH = SystemParameters.WorkArea.Height * 0.90;
        double neededH = ActualHeight + overflow;

        if (neededH <= maxH && neededH > ActualHeight + 5)
        {
            // Grow the window
            Height = neededH;
            // Re-check after grow: if still overflowing, wrap
            Dispatcher.InvokeAsync(() =>
            {
                UpdateLayout();
                double newBottom = TranslationPanel.TranslatePoint(new Point(0, TranslationPanel.ActualHeight), this).Y;
                if (newBottom + statusH > ActualHeight + 4)
                {
                    WrapTranslationInScrollViewer();
                }
            }, DispatcherPriority.Loaded);
        }
        else
        {
            WrapTranslationInScrollViewer();
            if (maxH > ActualHeight + 5) Height = maxH;
        }
    }

    private void WrapTranslationInScrollViewer()
    {
        if (TranslationInnerGrid.Parent is ScrollViewer) return;
        TranslationPanel.Child = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = TranslationInnerGrid
        };
    }

    private void BtnAddToAnki_Click(object s, RoutedEventArgs e)
    {
        var sel = TextTranscription.Selection;
        if (sel == null || sel.IsEmpty || _audio == null) return;

        var range = new TextRange(sel.Start, sel.End);
        string wordText = range.Text.Trim();
        if (string.IsNullOrWhiteSpace(wordText) || wordText.Length < 2) return;

        // ── Reliable timing: match via _wordRuns (1:1 with WordTimings) ──
        double wStart = 0, wEnd = 0;
        var words = _transcriptionProvider?.WordTimings;
        if (_wordRuns != null && words != null && words.Count > 0)
        {
            int first = -1, last = -1;
            for (int i = 0; i < _wordRuns.Length && i < words.Count; i++)
            {
                if (_wordRuns[i] == null) continue;
                // Run overlaps with user selection
                if (sel.Start.CompareTo(_wordRuns[i].ElementEnd) < 0 &&
                    sel.End.CompareTo(_wordRuns[i].ContentStart) > 0)
                {
                    if (first < 0) first = i;
                    last = i;
                }
            }
            if (first >= 0 && last >= first)
            {
                wStart = Math.Max(0, words[first].Start - 0.02);
                wEnd = Math.Max(wStart + 0.05, words[last].End - 0.08);
            }
        }

        // Fallback: use _lastHlIdx
        if (wEnd <= 0 || wEnd <= wStart)
        {
            if (_lastHlIdx >= 0 && _lastHlIdx < (words?.Count ?? 0))
            {
                wStart = words![_lastHlIdx].Start;
                wEnd = words[_lastHlIdx].End;
            }
            else
            {
                wStart = Math.Max(0, _positionSeconds - 0.5);
                wEnd = Math.Min(_durationSeconds, _positionSeconds + 2);
            }
        }

        // ── Context: extract the sentence containing the selected word ──
        string context = wordText;
        var para = TextTranscription.Document?.Blocks.FirstBlock as Paragraph;
        if (para != null)
        {
            string fullText = new TextRange(para.ContentStart, para.ContentEnd).Text;
            context = ExtractSentenceContaining(fullText, wordText);
        }

        string? ru = TxtTranslationResult.Text;
        if (string.IsNullOrEmpty(ru) || ru == "Translating...") ru = null;

        // Pass full selected phrase for IPA lookup and image search
        var window = new AnkiCardWindow(wordText, context, wStart, wEnd,
            _audio, _transcriptionProvider, _translationProvider, ru, wordText,
            _transcriptionProvider?.WordTimings, _ttsProvider);
        window.Owner = this;
        window.ShowDialog();
    }

    // Ctrl+T hotkey — also triggers translation of selection
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        // Do NOT intercept Ctrl+T if already used for API transcription
        if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            _ = DoTranslateSelection();
        }
    }
    private static string TruncStatus(string s, int max = 60) => s.Length <= max ? s : s[..max] + "...";

    private void OpenSettings() { if (_settingsWindow != null && _settingsWindow.IsVisible) { _settingsWindow.Focus(); return; } if (_config == null) { _config = new ConfigManager(AppDomain.CurrentDomain.BaseDirectory); _config.Load(); } _settingsWindow = new SettingsWindow(_config, this) { Owner = this }; _settingsWindow.ShowDialog(); if (_settingsWindow.Saved) { _translationProvider = new TranslationProvider(_config.YandexTranslateApiKey, _config.YandexTranslateFolderId, _config.TranslationProviderPreference); } }
    private void ButtonSettings_Click(object s, RoutedEventArgs e) => OpenSettings();
    public void OnFileSelected(string fp) => LoadAudioFile(fp);
    private void VolumeControl_VolumeChanged(object s, double v) { if (_audio != null) _audio.Volume = (float)v; }
    private void BtnManual_Click(object s, RoutedEventArgs e) => new ManualWindow(this).ShowDialog();
    private void BtnAbout_Click(object s, RoutedEventArgs e) => MessageBox.Show(Strings.Get("mw.dlg.about"), Strings.Get("mw.dlg.about_title"));
    protected override void OnClosed(EventArgs e) { SaveState(); _audio?.Stop(); _audio?.Dispose(); _positionTimer?.Stop(); _settingsWindow?.Close(); base.OnClosed(e); }

    // ── Language switching ──
    private void MnuLangEn_Click(object s, RoutedEventArgs e) => SwitchLanguage("en");
    private void MnuLangRu_Click(object s, RoutedEventArgs e) => SwitchLanguage("ru");

    private void SwitchLanguage(string lang)
    {
        Strings.SetLanguage(lang);
        if (_config != null) { _config.Language = lang; _config.Save(_config.Path, _config.FileName, _positionSeconds, _counter); }
        ApplyAllStrings();
    }

    private bool _transcriptionCollapsed;
    private void BtnCollapseTranscription_Click(object s, RoutedEventArgs e)
    {
        _transcriptionCollapsed = !_transcriptionCollapsed;
        var border = LayoutRoot.Children.OfType<System.Windows.Controls.Border>()
            .FirstOrDefault(b => b.Child is Grid g && g.Children.OfType<ScrollViewer>().Any());
        if (border != null)
            border.Visibility = _transcriptionCollapsed ? Visibility.Collapsed : Visibility.Visible;
        BtnCollapseTranscription.Content = _transcriptionCollapsed ? "▼" : "▲";
    }

    public void ApplyAllStrings()
    {
        if (MainMenu.Items.Count > 0) ((MenuItem)MainMenu.Items[0]).Header = Strings.Get("mw.menu.file");
        if (MainMenu.Items.Count > 1) ((MenuItem)MainMenu.Items[1]).Header = Strings.Get("mw.menu.split");
        if (MainMenu.Items.Count > 2) ((MenuItem)MainMenu.Items[2]).Header = Strings.Get("mw.menu.theme");
        if (MainMenu.Items.Count > 3) ((MenuItem)MainMenu.Items[3]).Header = Strings.Get("mw.menu.transcription");
        if (MainMenu.Items.Count > 4) ((MenuItem)MainMenu.Items[4]).Header = Strings.Get("mw.menu.settings");
        if (MainMenu.Items.Count > 5) ((MenuItem)MainMenu.Items[5]).Header = Strings.Get("mw.menu.lang");
        if (MainMenu.Items.Count > 6) ((MenuItem)MainMenu.Items[6]).Header = Strings.Get("mw.menu.help");
    }

    private void AdaptToScreen() { double sw = SystemParameters.WorkArea.Width, sh = SystemParameters.WorkArea.Height; Width = sw * 0.85; Height = sh * 0.48; MinWidth = sw * 0.5; MinHeight = 500; double bs = Math.Max(64, Math.Min(sw * 0.055, 100)), isize = bs * 0.85; foreach (var btn in new Button[] { ButtonFirst, ButtonPrevSegment, ButtonRepeat, ButtonPlayGo, ButtonPlay, ButtonNextSegment, ButtonLast }) { btn.Width = bs; btn.Height = bs; } foreach (var img in new[] { ImgFirst, ImgPrev, ImgRepeat, ImgPlayGo, ImgPlayPause, ImgNext, ImgLast }) if (img != null) { img.Width = isize; img.Height = isize; } if (WaveformGraph != null) WaveformGraph.Height = Math.Max(70, sh * 0.09); if (VolumeControl != null) VolumeControl.Width = Math.Max(160, Math.Min(sw * 0.25, 650)); }
    private void UpdateWaveform() { if (WaveformGraph == null || _audio == null) return; WaveformGraph.SamplesSmall = _audio.SamplesSmall; WaveformGraph.SampleRateSmall = _audio.SampleRateSmall; WaveformGraph.DurationSeconds = _durationSeconds; WaveformGraph.PositionSeconds = _positionSeconds; WaveformGraph.Fragments = _fragments; WaveformGraph.CurrentCounter = _counter; WaveformGraph.RepeatSegment = _repeatSegment; WaveformGraph.PlayGoMode = _playGoMode; }
    public ConfigManager? Config => _config; public TranscriptionProvider? Transcription => _transcriptionProvider;
    private static string FormatTime(double s) { if (s < 0) s = 0; var ts = TimeSpan.FromSeconds(s); return ts.Hours > 0 ? $"{ts.Hours}:{ts.Minutes:D2}:{ts.Seconds:D2}" : $"{ts.Minutes:D2}:{ts.Seconds:D2}"; }

    /// <summary>Extract the sentence containing the given word/phrase.</summary>
    private static string ExtractSentenceContaining(string fullText, string word)
    {
        if (string.IsNullOrWhiteSpace(fullText)) return word;
        int pos = fullText.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        if (pos < 0) return word;

        // Walk back to sentence start
        int start = pos;
        while (start > 0 && !IsSentenceEnd(fullText[start - 1]))
            start--;
        // Walk forward to sentence end
        int end = pos + word.Length;
        while (end < fullText.Length && !IsSentenceEnd(fullText[end]))
            end++;
        if (end < fullText.Length) end++; // include terminal punctuation

        return fullText.Substring(start, end - start).Trim();
    }

    private static bool IsSentenceEnd(char c) => c == '.' || c == '!' || c == '?' || c == '\n' || c == '\r';
}
