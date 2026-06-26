# RepeatSegment

**Smart audio segment repeater: auto-split by pauses · transcribe · translate in-place · export to Anki with images & audio.**

RepeatSegment is a Windows desktop app for language learners who use audiobooks. Load an audiobook, auto-split into repeatable segments, transcribe speech to text, translate words in-place, and export vocabulary cards to Anki — all in one workflow.

> 🖼️ *Screenshots coming soon — add PNG files to `docs/screenshots/`*

---

## ✨ Features

### 🎧 Smart Audio Segmentation
- Load **MP3/WAV** audiobooks
- Auto-detect pauses, split into 2/5/10/20 sec repeatable segments
- Visual waveform with mouse selection (SkiaSharp)
- Segment boundaries snap to nearest silence

### 📝 Speech-to-Text
- Transcribe via **Deepgram** or **AssemblyAI**
- Word-level timestamps with real-time highlighting during playback
- **11 languages** supported
- Chunk caching — transcribe once, replay forever

### 🌐 In-Place Translation
- Click any word in transcription → instant translation
- **Google Translate** + **Yandex Translate** with auto-fallback
- Translation panel appears below transcription
- **5 UI languages**: English, Русский, Deutsch, Français, Español

### 🃏 Anki Flashcard Export
- One-click "Add to Anki" from any translated word
- Two-sided cards (word ↔ translation)
- Automatic image search via **Google Images** or **Yandex**
- Three audio options per card:
  - 🔊 TTS pronunciation (Google / Deepgram)
  - 📖 Full sentence from the audiobook
  - 🎤 Your own voice recording
- Exports standard `.apkg` — compatible with Anki Desktop, AnkiDroid, AnkiWeb

### ⚡ Variable Speed Playback
- **0.4× to 1.5×** without pitch distortion (WSOLA algorithm)
- Bartlett window, 50% overlap, waveform-similarity cross-correlation

### 🎨 Dark & Light Themes
- Full theme toggle, persisted between sessions
- Dark title bars on all windows (Windows 11 immersive dark mode)

---

## 📦 Download

| File | Size | Description |
|------|------|-------------|
| [`RepeatSegment-Installer.msi`](https://github.com/Gusev-Sergey/RepeatSegment/releases/latest) | ~5.7 MB | WiX installer (framework-dependent) |

**[Download latest release →](https://github.com/Gusev-Sergey/RepeatSegment/releases/latest)**

---

## 💻 System Requirements

| Component | Requirement |
|-----------|------------|
| **OS** | Windows 10 x64 (build 17763+) / Windows 11 |
| **.NET Runtime** | [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **WebView2** | Preinstalled on Windows 11; auto-downloaded if missing |
| **API keys** | User-provided: Deepgram (transcription), AssemblyAI (alt), Google/Yandex (translation) |

---

## 🔧 Quick Start

1. Install [.NET 8 Desktop Runtime x64](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Download and run `RepeatSegment-Installer.msi`
3. Launch from Start Menu or Desktop shortcut
4. Enter API keys in **Settings → API keys**
5. Load an audiobook and start learning!

---

## 🛠 Tech Stack

| Component | Technology |
|-----------|-----------|
| Framework | .NET 8, WPF |
| Audio | NAudio + NAudio.Lame |
| Waveform | SkiaSharp |
| Browser | WebView2 |
| Database | SQLite (Anki .apkg) |
| Installer | WiX Toolset v5 |
| APIs | Deepgram, AssemblyAI, Google Translate, Yandex Translate, Google TTS, Deepgram Aura |

---

## 🚧 Roadmap

- [ ] Microsoft Store publication (MSIX package)
- [ ] SRT subtitle export
- [ ] More transcription languages
- [ ] Custom segment naming for Anki decks
- [ ] Portable self-contained build

---

## 👤 Credits

- **Developer**: AstrorumArbor (Sergey Gusev)
- **AI assistance**: VS Code + Zoo Code (Claude 4) + DeepSeek API
- **Contact**: [astrorum_arbor@outlook.com](mailto:astrorum_arbor@outlook.com)
- **Privacy Policy**: [gusev-sergey.github.io/RepeatSegment](https://gusev-sergey.github.io/RepeatSegment/)

---

## 📄 License

Freeware. All rights reserved © 2026 Sergey Gusev.
