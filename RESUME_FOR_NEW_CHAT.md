# Resume for New Chat — RepeatSegment Project

## Project Overview
**RepeatSegment** — C# WPF (.NET 8) приложение для изучения английского языка через аудиокниги. Позволяет загружать аудио, транскрибировать через Deepgram/AssemblyAI, переводить слова/фразы через Google/Yandex Translate, выделять сегменты мышкой на волновой форме и экспортировать карточки в Anki (.apkg).

**GitHub**: https://github.com/Gusev-Sergey/RepeatSegment
**Путь**: `c:\ProjectsCSharp\RepeatSegment`
**Бэкап**: `c:\ProjectsCSharp\RepeatSegment_Backup`

## Текущее состояние (21 июня 2026, ~01:00 MSK)

### Работает:
- Загрузка аудиофайлов и отображение волновой формы (SkiaSharp)
- Транскрипция через Deepgram/AssemblyAI (с пословным таймингом `WordTimings`), **11 языков**
- **Сегментация по длительности** — разбиение на отрезки заданной длительности (2/5/10/20 сек + Custom) с привязкой границ к ближайшей тишине (SnapToSilence, радиус 30% от длительности)
- Перевод через Google Translate (en→ru) + Yandex Translate, выбор сервиса в Translation Settings; 3 ретрая при rate-limit
- Ручное выделение сегментов мышкой на графике (drag overlay + persistent green highlight)
- Подсветка активного слова при воспроизведении
- **Anki экспорт** — ПОЛНОСТЬЮ РАБОЧИЙ (две карточки: en→ru + ru→en)
- **Двойное аудио в карточках Anki** — 🔊 Word (TTS) + 📖 Sentence (из книги)
- **TTS** — произнесение слов через Google TTS (по умолчанию) + Deepgram Aura; **ручной выбор провайдера** в AnkiCardWindow (Google/Deepgram/Record radio buttons), отдельный предпросмотр каждого
- **Скорость воспроизведения** — кнопка "Скорость" (0.4×–1.5×, шаг 0.1) слева от плеера; **WSOLA с сохранением тембра** (окно Бартлетта, 50% перекрытие, кросс-корреляция в зоне наложения)
- WebView2 поиск картинок (Yandex Images), автоочистка кеша при закрытии, JPEG сжатие quality=75
- Запись аудио с микрофона (NAudio)
- IPA транскрипция через dictionaryapi.dev + Wiktionary
- Инсталлятор (WiX)
- **Файловое логирование** (`%APPDATA%/RepeatSegment/repeat_segment.log`)
- **Pre-commit хук** — блокирует коммиты с API-ключами
- **Интернационализация** — EN/RU словари, меню Language с сохранением в config.ini; **все** пункты меню и дочерние окна переведены
- **User Guide** — RichTextBox с иконками кнопок, двухязычный, 11 разделов (обновлён под новую сегментацию и Settings)
- **Схлопывание транскрипции** — кнопка ▲/▼ слева-внизу от плеера
- **MP3 битрейт** — выбор 64/128 kbps в General Settings
- **Тёмная тема** — сохраняется между сессиями (config.ini: `theme = dark/light`), тёмный title bar на всех окнах (DWM Immersive Dark Mode)
- **Настройки** — разделены на 3 окна: API Keys, Translation, General
- **About window** — вместо MessageBox, с версией, тех. стеком и ссылкой на GitHub
- **Recent files** — File → список недавних файлов с корректной UTF-8 кодировкой

### Безопасность (важно):
- Ключи в `config.template.ini` заменены на `YOUR_*` плейсхолдеры
- Вся git-история очищена от реальных ключей (`git filter-branch` + force push)
- `config.ini` и `.env` в `.gitignore`
- `.env.template` для справки

## Структура проекта
```
RepeatSegment/
├── RepeatSegment.App/          # Основное WPF приложение
│   ├── AboutWindow.xaml/cs     # Окно "About" (замена MessageBox)
│   ├── AnkiBuilder.cs          # Сборка .apkg (DROP TABLE, ZipFile)
│   ├── AnkiExportManager.cs    # API: AddMedia/AddNote/Finalize + слияние
│   ├── AnkiCardWindow.xaml/cs  # UI форма карточки (TTS выбор, авто-поиск картинок)
│   ├── App.xaml/cs             # Startup, обработка исключений
│   ├── AudioEngine.cs          # NAudio: загрузка, воспроизведение, ExtractChunk, SaveSnippetMp3/Wav
│   ├── ConfigManager.cs        # INI + .env парсинг, сохранение настроек
│   ├── GeneralSettingsWindow.xaml/cs  # Общие настройки (язык, битрейт, чанки, latency)
│   ├── MainWindow.xaml/cs      # Главное окно — меню, плеер, транскрипция, перевод
│   ├── ManualWindow.xaml/cs    # User Guide (RichTextBox)
│   ├── RSWindow.cs             # Базовый класс окна (не используется — dark title bar в каждом окне отдельно)
│   ├── SettingsWindow.xaml/cs  # API ключи (AssemblyAI, Deepgram)
│   ├── SilenceDetector.cs      # Сегментация по длительности + SnapToSilence
│   ├── Strings.cs              # I18n: EN/RU словари + User Guide текст
│   ├── TranscriptionProvider.cs # Deepgram/AssemblyAI API
│   ├── TranslationProvider.cs  # Google + Yandex Translate
│   ├── TranslationSettingsWindow.xaml/cs  # Настройки перевода (Google/Yandex, ключи)
│   ├── TtsProvider.cs          # TTS: Google + Deepgram Aura, кеширование
│   ├── VolumeWidget.cs         # Виджет громкости
│   ├── WaveformGraph.cs        # Волновая форма (SkiaSharp) — drag overlay + user segment
│   └── Icons/                  # Иконки кнопок (8 PNG + app.ico)
├── DiagAnki/                   # Диагностическая утилита
├── Setup/                      # WiX инсталлятор
├── plans/                      # Архитектурные планы
├── AI_NOTES.md                 # Anki справочник + тех. находки
└── RepeatSegment.sln
```

## Ключевые файлы — что где лежит:

| Файл | Назначение |
|------|-----------|
| [`RepeatSegment.App/MainWindow.xaml`](RepeatSegment.App/MainWindow.xaml) | Главное окно: меню, плеер, транскрипция, перевод, Resources (кисти) |
| [`RepeatSegment.App/MainWindow.xaml.cs`](RepeatSegment.App/MainWindow.xaml.cs) | Вся логика главного окна: загрузка, транскрипция, сегментация, тема, recent files, ApplyAllStrings |
| [`RepeatSegment.App/SilenceDetector.cs`](RepeatSegment.App/SilenceDetector.cs) | Сегментация: деление на отрезки + SnapToSilence (радиус 30%) |
| [`RepeatSegment.App/ConfigManager.cs`](RepeatSegment.App/ConfigManager.cs) | Все настройки: segment_duration_sec, theme, mp3_bitrate, язык и т.д. |
| [`RepeatSegment.App/Strings.cs`](RepeatSegment.App/Strings.cs) | Все строки EN/RU + User Guide (GetGuideSection, GetGuideContent) |
| [`RepeatSegment.App/AudioEngine.cs`](RepeatSegment.App/AudioEngine.cs) | NAudio: SaveSnippetMp3 (Mp3BitrateKbps), SaveSnippetWav, воспроизведение |
| [`RepeatSegment.App/WaveformGraph.cs`](RepeatSegment.App/WaveformGraph.cs) | SkiaSharp: waveform, drag overlay, user segment (зелёный), cursor, ruler |
| [`RepeatSegment.App/AnkiBuilder.cs`](RepeatSegment.App/AnkiBuilder.cs) | Сборка .apkg: SQLite + ZIP + media.json |
| [`RepeatSegment.App/AnkiCardWindow.xaml`](RepeatSegment.App/AnkiCardWindow.xaml) | UI карточки: TTS radio buttons, запись, картинки, Decks |
| [`RepeatSegment.App/AnkiCardWindow.xaml.cs`](RepeatSegment.App/AnkiCardWindow.xaml.cs) | Логика карточки: авто-поиск картинок, TTS выбор, создание карточки |
| [`RepeatSegment.App/TtsProvider.cs`](RepeatSegment.App/TtsProvider.cs) | TTS: Google + Deepgram, кеширование (gg_/dg_ префиксы), HasDeepgram |
| [`RepeatSegment.App/SettingsWindow.xaml`](RepeatSegment.App/SettingsWindow.xaml) | Окно API ключей |
| [`RepeatSegment.App/TranslationSettingsWindow.xaml`](RepeatSegment.App/TranslationSettingsWindow.xaml) | Окно настроек перевода |
| [`RepeatSegment.App/GeneralSettingsWindow.xaml`](RepeatSegment.App/GeneralSettingsWindow.xaml) | Окно общих настроек (ComboBox с ControlTemplate для тёмной темы) |
| [`RepeatSegment.App/AboutWindow.xaml`](RepeatSegment.App/AboutWindow.xaml) | Окно "About" |
| [`RepeatSegment.App/ManualWindow.xaml`](RepeatSegment.App/ManualWindow.xaml) | User Guide |
| [`AI_NOTES.md`](AI_NOTES.md) | Anki справочник + тех. находки |

## NuGet пакеты:
- Microsoft.Data.Sqlite 10.0.9
- NAudio 2.2.1
- NAudio.Lame 2.1.0 (MP3 конвертация)
- Microsoft.Web.WebView2 1.0.4022.49
- SkiaSharp.Views.WPF 2.88.7
- OxyPlot.Wpf 2.2.0
- System.Configuration.ConfigurationManager 8.0.0

## ⚠️ Критические правила (выстраданы, не нарушать)

### Anki
1. **mid** всегда уникальный при изменении шаблонов — иначе Anki кеширует старую модель
2. **name модели** тоже должен быть уникальным — Anki deduplicates по name, а не только по mid
3. **`[sound:...]`** должен быть на отдельной строке, без HTML-тегов перед ним
4. HTML `<audio>` тег **не работает** в поле `{{sound}}` — только `[sound:...]`
5. **`bury:false`** в dconf — иначе sibling-карточки скрываются
6. **`req`** используй `"any"` вместо `"all"` — иначе карточка не генерируется без картинки/контекста
7. Медиафайлы в ZIP: **числовые имена без расширений** (`"0"`, `"1"`)
8. `media.json`: `{"0":"m0.jpg","1":"m1.mp3"}` — UTF-8 без BOM
9. Поле image: `<img src="m0.jpg">`
10. Поле sound: `[sound:m1.mp3]` (MP3 обязателен, WAV не работает)
11. Модель: поля `word/transcription/translation/image/sound/context`
12. Шаблоны: `{{FrontSide}}` + `<hr id=answer>` для сохранения лица на обороте
13. `mid` = `1728000001L`, `csum` = `unchecked((int)Cr32(f))`
14. CSS: тёмная тема `background-color:#1e1e1e`

### WPF / Тёмная тема
15. **Frozen Brush**: `SolidColorBrush` из XAML заморожен — всегда `new SolidColorBrush(color)` при смене темы
16. **Title bar**: `DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int))` в `Window_Loaded`
17. **Дочерние окна**: кисти копировать из MainWindow через `InjectBrushes()`, проверять `_mw.IsDarkTheme` для title bar
18. **ComboBox в дочерних окнах**: нужен полный `ControlTemplate` с `{DynamicResource InputBgBrush}` и т.д. — StaticResource из родительского окна не наследуется

### Кодировка
19. **Всегда `Encoding.UTF8`** при `File.WriteAllLines`/`File.ReadAllLines` — ANSI (windows-1251) портит кириллицу
20. **UTF-8 без BOM** для `media.json`: `new UTF8Encoding(false)`

### Сегментация
21. Не хардкодить язык транскрипции — использовать `_cfg.TranscriptionLanguage`
22. `SegmentDurationSec` в ConfigManager (по умолчанию 5.0)

### Запуск приложения
23. Не использовать `ShowDialog()` в `OnStartup` до `base.OnStartup(e)` с `StartupUri`
24. `ShutdownMode = OnMainWindowClose` при ручном создании MainWindow

### Размеры окон
25. `SizeToContent` не может опуститься ниже `MinHeight`
26. Для схлопывания: `Height = double.NaN; MinHeight = 0; SizeToContent = Height`

## Известные TODO / недоделки:
- Auto-play **осознанно отключён** (`"autoplay":false` в dconf) — при двух аудио Anki проигрывает все `[sound:...]` подряд
- Слияние колод работает но может дублировать медиафайлы при повторном создании (нет content-based дедупликации в BuildDeck)
- `dcid` timestamp-based — накопление старых dconf-записей в JSON клиента (не критично)
