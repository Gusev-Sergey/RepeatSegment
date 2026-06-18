# Android Port Plan — RepeatSegment

## Overview

Port RepeatSegment (WPF .NET 8 → Android) with maximum code reuse of business logic. The core challenge is not C# → Kotlin/Java — it's desktop UI → mobile UX redesign.

## Tech Stack Recommendation

**Primary: .NET MAUI** (C# + XAML)
- 70%+ code reuse: TranscriptionProvider, TranslationProvider, TtsProvider, AnkiBuilder, AnkiExportManager, SilenceDetector
- Single language (C# throughout)
- NuGet ecosystem preserved (SkiaSharp for waveform, SQLite for Anki)

**Alternative: Kotlin + shared C# core**
- `RepeatSegment.Core` (netstandard2.0) — all business logic
- Android UI in Kotlin/Compose
- More native feel, but two codebases to maintain

**Recommendation:** Start with MAUI for MVP, extract to Core if needed later.

---

## File Migration Map

| WPF File | Android Status | Notes |
|----------|---------------|-------|
| `AudioEngine.cs` | **Rewrite** | NAudio → `AudioTrack`/`MediaPlayer`/`MediaExtractor` |
| `SilenceDetector.cs` | **Reuse** | Pure math on float[] — works as-is |
| `TranscriptionProvider.cs` | **Reuse** | HTTP calls only — no changes |
| `TranslationProvider.cs` | **Reuse** | Google Translate API — no changes |
| `TtsProvider.cs` | **Reuse** | Deepgram/Google TTS — no changes |
| `AnkiBuilder.cs` | **Reuse** | ZIP + SQLite — works as-is |
| `AnkiExportManager.cs` | **Reuse** | File operations — works as-is |
| `ConfigManager.cs` | **Adapt** | `AppData` path → `Context.FilesDir` |
| `Strings.cs` | **Reuse** | EN/RU dictionaries — works as-is |
| `MainWindow.xaml` | **Rewrite** | PlayerActivity + bottom sheet |
| `AnkiCardWindow.xaml` | **Rewrite** | Full-screen dialog |
| `SettingsWindow.xaml` | **Rewrite** | PreferenceScreen / new activity |
| `ManualWindow.xaml` | **Adapt** | Scrollable text screen |
| `WaveformGraph.cs` | **Rewrite** | SkiaSharp on MAUI Canvas |
| `VolumeWidget.cs` | **Drop** | Use device volume keys |

---

## Screen Redesign — Desktop → Mobile

### Desktop Layout (current)
```
┌──────────────────────────────────────────────┐
│ Menu: File | Split | Theme | Trscr | Set | Lang | Help │
├──────────────────────────────────────────────┤
│ Waveform (110px height, full width)           │
├──────────────────────────────────────────────┤
│ [00:00] ════════════●══════════ [05:30]       │
├──────────────────────────────────────────────┤
│ [⏮] [⏪] [🔄] [▶⏩] [⏸] [⏩] [⏭]    [Volume]   │
├──────────────────────────────────────────────┤
│ Transcription text (~40% screen)              │
│ The quick brown fox jumps over the lazy dog...│
├──────────────────────────────────────────────┤
│ Translation panel (appears on selection)      │
│ "быстрая коричневая лиса"           [Anki]   │
├──────────────────────────────────────────────┤
│ Status: Ready                                 │
└──────────────────────────────────────────────┘
```

### Phone Target Layout
```
┌──────────────┐
│ ≡ Player ⚙  │  ← Top bar: hamburger + title + settings
├──────────────┤
│              │
│  Waveform    │  ← Full width, pinch-zoomable
│  (SkiaSharp) │     Horizontal scroll for long audio
│              │
├──────────────┤
│ [00:00] ═══●═══ [05:30] │  ← Slider
├──────────────┤
│              │
│ Transcription│  ← Bottom Sheet (peek: 80px,
│ text here... │     expandable to 60% screen)
│              │
├──────────────┤
│ ⏮ ⏸ ▶ ⏭ 🔄 │  ← Fixed bottom bar, 5 buttons max
└──────────────┘

FAB:  [+Anki]        ← Floating Action Button (appears on text selection)
```

### Screen Structure (3-4 screens)

```
Navigation:
├── PlayerScreen (main)
│   ├── TopAppBar (hamburger + title)
│   ├── Waveform view (full width, SkiaSharp)
│   ├── Slider
│   ├── BottomSheet (transcription + translation)
│   └── BottomBar (5 media buttons)
├── AnkiCardScreen (full-screen dialog)
│   ├── EN word + transcription
│   ├── RU translation
│   ├── Context text
│   ├── Sentence audio ▶
│   ├── TTS audio ▶
│   ├── Picture search → Image picker
│   └── [Create Cards] button
├── SettingsScreen
│   ├── API keys (Deepgram, AssemblyAI)
│   ├── Translation provider (Google/Yandex)
│   ├── Chunk minutes
│   └── Language (EN/RU)
└── LibraryScreen (future)
    └── List of downloaded audiobooks
```

---

## Component Mapping (Desktop → Android)

| Desktop Component | Android Equivalent |
|-------------------|-------------------|
| `Window` | `Activity` or `ContentPage` (MAUI) |
| `Menu` (7 items) | `NavigationView` drawer (hamburger) |
| `StackPanel` / `Grid` | `LinearLayout` / `ConstraintLayout` / `Grid` (MAUI) |
| `Button` | `Button` / `ImageButton` |
| `Slider` | `SeekBar` / `Slider` (MAUI) |
| `TextBox` / `RichTextBox` | `TextView` / `EditText` / `Label` / `Editor` (MAUI) |
| `ComboBox` | `Spinner` / `Picker` (MAUI) |
| `WebView2` | `WebView` (Android Chromium) — for image search |
| `MessageBox` | `AlertDialog` / `DisplayAlert` (MAUI) |
| `OpenFileDialog` | `Intent.ACTION_OPEN_DOCUMENT` / SAF |
| `SoundPlayer` | `MediaPlayer` |
| `DispatcherTimer` | `Handler.PostDelayed` / `IDispatcherTimer` (MAUI) |
| `StatusBar` | `Snackbar` / bottom text |
| `ProgressBar` | `ProgressBar` (horizontal or circular) |

---

## What to DROP for MVP

| Feature | Reason |
|---------|--------|
| `VolumeWidget` | Use device volume buttons |
| `FirstRunWindow` | Android system locale detection |
| `ManualWindow` (complex) | Simple About screen |
| Microphone recording | Rarely used, can add later |
| OxyPlot (waveform graph) | Replace with SkiaSharp (already in project!) |
| Dark/Light theme toggle | Follow system theme |
| Installer (WiX) | Google Play / APK |

---

## Audio Engine Rewrite

Current (NAudio): `Mp3FileReader` → float[] samples → `WaveOutEvent`

Android:
```
MediaExtractor (MP3) → MediaCodec (decode) → float[] buffer
                                                   ↓
                                          SilenceDetector (unchanged)
                                                   ↓
                                          Playback: AudioTrack.write(float[])
                                          Seek: MediaExtractor.seekTo()
                                          ExtractChunk: TrimmedMediaExtractor
```

Key challenges:
- **Seeking in MP3:** Android `MediaExtractor` + `MediaCodec` can seek precisely
- **Chunk extraction:** Decode segment, re-encode as WAV (for API) or MP3 (for Anki)
- **Realtime highlight:** Same logic as desktop — `WordTimings` with time-based lookup

---

## Android-Specific Features (new opportunities)

| Feature | How |
|---------|-----|
| Share .apkg to AnkiDroid | `Intent.ACTION_SEND` + content URI |
| Open audio from file manager | `Intent.ACTION_VIEW` filter |
| Background playback | `MediaSession` + notification controls |
| Headset controls | `MediaSession.Callback.onMediaButtonEvent` |
| Picture search | Android `WebView` (Chromium-based) |
| Offline cache | Room/SQLite for transcription cache |
| System dark mode | `AppCompatDelegate.setDefaultNightMode` |

---

## Implementation Phases

### Phase 1: MVP (2-3 weeks)
1. MAUI project setup + shared core library
2. AudioEngine rewrite (MediaExtractor + AudioTrack)
3. Player screen: waveform (SkiaSharp), slider, 5 buttons
4. Transcription: bottom sheet with text
5. Translation: tap-to-select → popup
6. Settings: API keys screen
7. AnkiCard: basic card creation (text + TTS only)

### Phase 2 (1-2 weeks)
8. Anki card: sentence audio extraction + picture search (WebView)
9. Library: file browser for audiobooks
10. Background playback + notification

### Phase 3 (1-2 weeks)
11. Polish: proper Material Design theming
12. Google Play Store listing
13. Crash reporting (AppCenter/Sentry)

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| NAudio not available on Android | Complete rewrite of AudioEngine — highest effort |
| MAUI bugs on Android | Test early, consider Kotlin fallback |
| WebView2 → Android WebView | Both are Chromium-based, similar API |
| File paths different | Abstract via `IPlatformPaths` interface |
| OxyPlot.WPF → SkiaSharp | Already use SkiaSharp for images — expand to waveform |
| Large APK size | Use Android App Bundle, proguard/R8 |
