# RepeatSegment — Product Brief for AI Lovable Website

## One-Liner
**RepeatSegment** — Smart audio segment repeater: auto-split by pauses · transcribe · translate in-place · export to Anki with images & audio.

## What is RepeatSegment?
A Windows desktop application (WPF, .NET 8) for language learners who use audiobooks. It loads an audiobook, automatically splits it into repeatable segments, transcribes speech to text, translates words in-place, and exports vocabulary cards to Anki with images and audio.

## Target Audience
- Self-learners of foreign languages (English, German, French, Spanish, Japanese, etc.)
- Audiobook listeners who want to extract vocabulary
- Anki users who want to automate flashcard creation from audio content

## Key Features

### 1. Smart Audio Segmentation
- Load any MP3/WAV audiobook
- Auto-detect pauses and split into repeatable segments (2/5/10/20 sec)
- Visual waveform with mouse selection (SkiaSharp rendering)
- Snap segment boundaries to nearest silence for clean cuts

### 2. Speech-to-Text Transcription
- Transcribe audio chunks via Deepgram or AssemblyAI
- Word-level timestamps with synchronized highlighting during playback
- Support for 11 languages
- Chunk caching — transcribe once, replay forever

### 3. In-Place Translation
- Select any word/phrase in the transcription
- Instant translation via Google Translate or Yandex Translate
- Translation panel appears below the transcription
- 5 UI languages: English, Russian, German, French, Spanish

### 4. Anki Flashcard Export
- One-click "Add to Anki" from any translated word
- Two-sided cards: word→translation + translation→word
- Automatic image search (Google Images via built-in browser)
- Three audio options per card:
  - 🔊 Word pronunciation (TTS via Google/Deepgram)
  - 📖 Full sentence from the audiobook
  - 🎤 Your own voice recording
- Exports standard .apkg files compatible with Anki Desktop, AnkiDroid, AnkiWeb

### 5. Variable Speed Playback
- Adjust speed 0.4× to 1.5× without pitch distortion (WSOLA algorithm)
- Bartlett window, 50% overlap, waveform-similarity cross-correlation

### 6. Dark & Light Themes
- Full dark/light theme toggle
- Dark title bars on all windows (Windows 11 immersive dark mode)
- Theme persists between sessions

## Technical Stack
- **Framework**: .NET 8, WPF
- **Audio**: NAudio + NAudio.Lame
- **Graphics**: SkiaSharp (waveform)
- **Browser**: WebView2 (image search)
- **Database**: SQLite (Anki .apkg format)
- **APIs**: Deepgram, AssemblyAI, Google Translate, Yandex Translate, Google TTS, Deepgram Aura
- **Installer**: WiX Toolset v5 (MSI, ~6.5 MB)
- **Distribution**: Free download, Windows 10/11 x64, requires .NET 8 Desktop Runtime

## Visual Identity
- **Name**: RepeatSegment
- **Developer**: AstrorumArbor
- **Contact**: astrorum_arbor@outlook.com
- **GitHub**: https://github.com/Gusev-Sergey/RepeatSegment
- **License**: Freeware
- **Icon**: Sound waveform with repeat-loop symbol

### Suggested Color Palette
- Primary: Deep blue (#1E3A5F) — trust, technology, focus
- Accent: Teal (#00B4A6) — learning, growth, clarity
- Dark theme background: #1E1E1E (VS Code dark)
- Light theme background: #FAFAFA
- Text: #E0E0E0 (dark) / #2D2D2D (light)

### Suggested Website Sections
1. **Hero** — App name + one-liner + screenshot + Download button
2. **How It Works** — 4-step flow: Load → Transcribe → Translate → Export
3. **Features** — 6 feature cards with icons (as listed above)
4. **Screenshots** — Gallery: waveform view, transcription, Anki card window, settings
5. **Download** — Download button with system requirements, link to GitHub releases
6. **Privacy** — Link to privacy policy (GitHub Pages)
7. **Contact** — Email + GitHub link

## Website Tone
- Professional but friendly
- Focus on productivity and learning efficiency
- Technical credibility (mention real APIs, algorithms)
- Bilingual potential (EN + RU)

## Assets Needed
- App screenshots (PNG, 1920×1080)
- App icon (ICO/PNG, 256×256)
- Feature icons (optional, can use emoji or Lucide icons)
