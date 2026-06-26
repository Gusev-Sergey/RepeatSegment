# Graph Report - C:\ProjectsCSharp\RepeatSegment  (2026-06-24)

## Corpus Check
- cluster-only mode — file stats not available

## Summary
- 443 nodes · 813 edges · 47 communities (34 shown, 13 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 1 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `a0a14967`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Project Documentation & Planning|Project Documentation & Planning]]
- [[_COMMUNITY_Window Setup & Interop|Window Setup & Interop]]
- [[_COMMUNITY_Anki Card Creation UI|Anki Card Creation UI]]
- [[_COMMUNITY_Waveform Graph Widget|Waveform Graph Widget]]
- [[_COMMUNITY_Anki Deck Builder & Serialization|Anki Deck Builder & Serialization]]
- [[_COMMUNITY_Audio Playback Engine|Audio Playback Engine]]
- [[_COMMUNITY_Transcription & API Client|Transcription & API Client]]
- [[_COMMUNITY_Main Window UI & Actions|Main Window UI & Actions]]
- [[_COMMUNITY_Project Dependencies & Packages|Project Dependencies & Packages]]
- [[_COMMUNITY_Playback Control & Timeline|Playback Control & Timeline]]
- [[_COMMUNITY_Configuration Manager|Configuration Manager]]
- [[_COMMUNITY_Unit Tests|Unit Tests]]
- [[_COMMUNITY_Translation Selection & Key Handling|Translation Selection & Key Handling]]
- [[_COMMUNITY_Language Generation Project|Language Generation Project]]
- [[_COMMUNITY_Icon Generation Project|Icon Generation Project]]
- [[_COMMUNITY_Slider Drag Event|Slider Drag Event]]
- [[_COMMUNITY_Large Tile Logo|Large Tile Logo]]
- [[_COMMUNITY_Small Tile Logo|Small Tile Logo]]
- [[_COMMUNITY_150x150 App Logo|150x150 App Logo]]
- [[_COMMUNITY_44x44 App Logo|44x44 App Logo]]
- [[_COMMUNITY_Store Logo|Store Logo]]
- [[_COMMUNITY_Wide Logo|Wide Logo]]
- [[_COMMUNITY_WiX Build Tracking|WiX Build Tracking]]
- [[_COMMUNITY_WiX File List (obj2)|WiX File List (obj2)]]
- [[_COMMUNITY_WiX File List (self-contained)|WiX File List (self-contained)]]

## God Nodes (most connected - your core abstractions)
1. `MainWindow` - 107 edges
2. `AnkiCardWindow` - 36 edges
3. `AudioEngine` - 29 edges
4. `Project Overview Resume for New Chat` - 27 edges
5. `TranscriptionProvider` - 25 edges
6. `WaveformGraph` - 20 edges
7. `Anki .apkg Technical Reference & Bug Solutions` - 18 edges
8. `TranslationSettingsWindow` - 15 edges
9. `ConfigManager` - 13 edges
10. `VolumeWidget` - 13 edges

## Surprising Connections (you probably didn't know these)
- `Project Overview Resume for New Chat` --references--> `lang/en.json`  [EXTRACTED]
  RESUME_FOR_NEW_CHAT.md → RepeatSegment.App/lang/en.json
- `Project Overview Resume for New Chat` --references--> `lang/ru.json`  [EXTRACTED]
  RESUME_FOR_NEW_CHAT.md → RepeatSegment.App/lang/ru.json
- `Project Overview Resume for New Chat` --references--> `Graphify Usage Rules for RepeatSegment`  [EXTRACTED]
  RESUME_FOR_NEW_CHAT.md → AGENTS.md
- `Audit of Critical Rules Compliance` --evaluates--> `Anki .apkg Technical Reference & Bug Solutions`  [EXTRACTED]
  plans/audit_june21.md → AI_NOTES.md
- `Audit of Critical Rules Compliance` --evaluates--> `Project Overview Resume for New Chat`  [EXTRACTED]
  plans/audit_june21.md → RESUME_FOR_NEW_CHAT.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Project Build & Design Skills** — dotnet_publish_skill, wix_installer_skill, wpf_adaptive_layout_skill, wpf_i18n_skill [EXTRACTED 0.90]
- **Microsoft Store App Icons** — icons_large_tile_image, icons_small_tile_image, icons_square150x150logo_image, icons_square44x44logo_image, icons_storelogo_image, icons_wide310x150logo_image [EXTRACTED 0.95]
- **Player Control Icons** — icons_play_image, icons_stop_play_image, icons_repeat_image, icons_next_play_image, icons_pre_play_image, icons_play_go_image, icons_first_image, icons_last_image [EXTRACTED 0.95]

## Communities (47 total, 13 thin omitted)

### Community 0 - "Project Documentation & Planning"
Cohesion: 0.07
Nodes (31): Graphify Usage Rules for RepeatSegment, Anki .apkg Technical Reference & Bug Solutions, Android Port Plan, Application, Audit of AGENTS.md Rules Compliance, Audit of Critical Rules Compliance, Build Output (Warnings), Privacy Policy Page (EN & RU) (+23 more)

### Community 1 - "Window Setup & Interop"
Cohesion: 0.07
Nodes (8): DllImport, IntPtr, AboutWindow, GeneralSettingsWindow, ManualWindow, SettingsWindow, RequestNavigateEventArgs, Window

### Community 2 - "Anki Card Creation UI"
Cohesion: 0.09
Nodes (5): AnkiCardWindow, TranslationSettingsWindow, RoutedEventArgs, WaveFileWriter, WaveInEvent

### Community 3 - "Waveform Graph Widget"
Cohesion: 0.10
Nodes (16): bool, double, MouseButtonEventArgs, MouseEventArgs, Polygon, Rectangle, VolumeWidget, WaveformGraph (+8 more)

### Community 4 - "Anki Deck Builder & Serialization"
Cohesion: 0.09
Nodes (13): byte, End, List, AnkiBuilder, MediaData, NoteData, SqE, AnkiExportManager (+5 more)

### Community 5 - "Audio Playback Engine"
Cohesion: 0.10
Nodes (11): float, IDisposable, int, IWaveProvider, long, object, AudioEngine, Log (+3 more)

### Community 6 - "Transcription & API Client"
Cohesion: 0.13
Nodes (8): HashSet, HttpClient, JsonElement, TranscriptionProvider, TranscriptionResult, TranslationProvider, TtsProvider, Task

### Community 7 - "Main Window UI & Actions"
Cohesion: 0.10
Nodes (6): BitmapImage, CancellationTokenSource, DateTime, DispatcherTimer, MainWindow, Run

### Community 8 - "Project Dependencies & Packages"
Cohesion: 0.09
Nodes (18): net8.0-windows, Microsoft.Data.Sqlite (10.0.9), NAudio.Lame (2.1.0), Microsoft.NET.Sdk, coverlet.collector (6.0.0), Microsoft.NET.Test.Sdk (17.8.0), Microsoft.Web.WebView2 (1.0.4022.49), NAudio (2.2.1) (+10 more)

### Community 9 - "Playback Control & Timeline"
Cohesion: 0.15
Nodes (3): DragCompletedEventArgs, EventArgs, RoutedPropertyChangedEventArgs

### Community 10 - "Configuration Manager"
Cohesion: 0.22
Nodes (3): Dictionary, ConfigManager, Strings

### Community 11 - "Unit Tests"
Cohesion: 0.25
Nodes (4): Fact, AudioEngineTests, ConfigManagerTests, SilenceDetectorTests

## Knowledge Gaps
- **45 isolated node(s):** `net8.0-windows`, `Microsoft.Data.Sqlite (10.0.9)`, `NAudio.Lame (2.1.0)`, `Microsoft.NET.Sdk`, `net8.0-windows` (+40 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **13 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainWindow` connect `Main Window UI & Actions` to `Project Documentation & Planning`, `Window Setup & Interop`, `Anki Card Creation UI`, `Waveform Graph Widget`, `Anki Deck Builder & Serialization`, `Audio Playback Engine`, `Transcription & API Client`, `Playback Control & Timeline`, `Configuration Manager`, `Segment Navigation Buttons`, `Transcription & Word Highlighting`, `Audio File Loading & Recent Files`, `Translation Selection & Key Handling`, `Recent Files Management`, `Theme Management`, `Segment Duration & Waveform`, `Slider Drag Event`?**
  _High betweenness centrality (0.433) - this node is a cross-community bridge._
- **Why does `AudioEngine` connect `Audio Playback Engine` to `Project Documentation & Planning`, `Anki Card Creation UI`, `Waveform Graph Widget`, `Transcription & API Client`, `Main Window UI & Actions`?**
  _High betweenness centrality (0.101) - this node is a cross-community bridge._
- **Why does `Project Overview Resume for New Chat` connect `Project Documentation & Planning` to `Window Setup & Interop`, `Anki Card Creation UI`?**
  _High betweenness centrality (0.089) - this node is a cross-community bridge._
- **What connects `net8.0-windows`, `Microsoft.Data.Sqlite (10.0.9)`, `NAudio.Lame (2.1.0)` to the rest of the system?**
  _45 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Project Documentation & Planning` be split into smaller, more focused modules?**
  _Cohesion score 0.07020408163265306 - nodes in this community are weakly interconnected._
- **Should `Window Setup & Interop` be split into smaller, more focused modules?**
  _Cohesion score 0.06923076923076923 - nodes in this community are weakly interconnected._
- **Should `Anki Card Creation UI` be split into smaller, more focused modules?**
  _Cohesion score 0.08974358974358974 - nodes in this community are weakly interconnected._