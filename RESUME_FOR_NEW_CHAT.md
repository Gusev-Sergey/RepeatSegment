# Resume for New Chat — RepeatSegment Project

## Project Overview
**RepeatSegment** — C# WPF (.NET 8) приложение для изучения языков через аудиокниги. Позволяет загружать аудио, транскрибировать через Deepgram/AssemblyAI, переводить слова/фразы через Google/Yandex Translate, выделять сегменты мышкой на волновой форме и экспортировать карточки в Anki (.apkg).

**GitHub**: https://github.com/Gusev-Sergey/RepeatSegment
**Путь**: `c:\ProjectsCSharp\RepeatSegment`
**Бэкап**: `c:\ProjectsCSharp\RepeatSegment_Backup`

## Текущее состояние (24 июня 2026)

### Работает:
- Загрузка аудиофайлов и отображение волновой формы (SkiaSharp)
- Транскрипция через Deepgram/AssemblyAI (с пословным таймингом `WordTimings`), **11 языков**
- **Сегментация по длительности** — разбиение на отрезки заданной длительности (2/5/10/20 сек + Custom) с привязкой границ к ближайшей тишине (SnapToSilence, радиус 30% от длительности)
- Перевод через Google Translate + Yandex Translate, выбор сервиса в Translation Settings; 3 ретрая при rate-limit
- Ручное выделение сегментов мышкой на графике (drag overlay + persistent green highlight)
- Подсветка активного слова при воспроизведении
- **Anki экспорт** — ПОЛНОСТЬЮ РАБОЧИЙ (две карточки: en→ru + ru→en)
- **Двойное аудио в карточках Anki** — 🔊 Word (TTS) + 📖 Sentence (из книги)
- **TTS** — произнесение слов через Google TTS (по умолчанию) + Deepgram Aura; ручной выбор провайдера в AnkiCardWindow (Google/Deepgram/Record radio buttons)
- **Скорость воспроизведения** — кнопка "Скорость" (0.4×–1.5×, шаг 0.1); **WSOLA с сохранением тембра** (окно Бартлетта, 50% перекрытие, кросс-корреляция)
- **WebView2 поиск картинок** — Google Images (основной) + Yandex Images; извлечение URL через `elementsFromPoint()` + `fetch()` внутри браузера
- Запись аудио с микрофона (NAudio)
- IPA транскрипция через dictionaryapi.dev + Wiktionary
- **WiX инсталлятор** — framework-dependent .msi (~6.5 МБ), MajorUpgrade с `AllowSameVersionUpgrades="yes"`
- **Файловое логирование** (`%APPDATA%/RepeatSegment/repeat_segment.log`)
- **Pre-commit хук** — блокирует коммиты с API-ключами
- **Интернационализация** — 5 языков (EN/RU/DE/FR/ES), внешние JSON-файлы `lang/{code}.json`, смена через перезапуск
- **User Guide** — RichTextBox с иконками кнопок, 11 разделов на всех языках
- **Схлопывание транскрипции** — кнопка ▲/▼ слева-внизу от плеера
- **MP3 битрейт** — выбор 64/128 kbps в General Settings
- **Тёмная тема** — сохраняется между сессиями (config.ini: `theme = dark/light`), тёмный title bar на всех окнах (DWM Immersive Dark Mode)
- **Адаптивные размеры** — все окна и элементы привязаны к `SystemParameters.WorkArea`, формула `Math.Max(порог, доля * WorkArea)`
- **Настройки** — разделены на 4 окна: API Keys, Translation, General (+ язык), About
- **Recent files** — File → список недавних файлов с корректной UTF-8 кодировкой
- **WebView2 warm-up** — `CoreWebView2Environment.CreateAsync` в `App.OnStartup()` (устраняет задержку 3–10 сек)
- **TextButtonStyle** — кастомный стиль кнопок без системного голубого hover в тёмной теме

### Безопасность (важно):
- Ключи в `config.template.ini` заменены на `YOUR_*` плейсхолдеры
- Вся git-история очищена от реальных ключей (`git filter-branch` + force push)
- `config.ini` и `.env` в `.gitignore`
- `.env.template` для справки

## Структура проекта
```
RepeatSegment/
├── RepeatSegment.App/          # Основное WPF приложение
│   ├── AboutWindow.xaml/cs     # Окно "About" (адаптивное)
│   ├── AnkiBuilder.cs          # Сборка .apkg (DROP TABLE, ZipFile)
│   ├── AnkiExportManager.cs    # API: AddMedia/AddNote/Finalize + слияние
│   ├── AnkiCardWindow.xaml/cs  # UI форма карточки (TTS выбор, авто-поиск картинок, WebView2)
│   ├── App.xaml/cs             # Startup, WebView2 warm-up, обработка исключений
│   ├── AudioEngine.cs          # NAudio: загрузка, воспроизведение, ExtractChunk, SaveSnippetMp3/Wav
│   ├── ConfigManager.cs        # INI + .env парсинг, сохранение настроек
│   ├── GeneralSettingsWindow.xaml/cs  # Общие настройки (язык, битрейт, чанки, latency)
│   ├── MainWindow.xaml/cs      # Главное окно — меню, плеер, транскрипция, перевод, TextButtonStyle
│   ├── ManualWindow.xaml/cs    # User Guide (RichTextBox, адаптивный)
│   ├── SettingsWindow.xaml/cs  # API ключи (AssemblyAI, Deepgram)
│   ├── SilenceDetector.cs      # Сегментация по длительности + SnapToSilence
│   ├── Strings.cs              # I18n: загрузка из lang/{code}.json, fallback → en
│   ├── TranscriptionProvider.cs # Deepgram/AssemblyAI API
│   ├── TranslationProvider.cs  # Google + Yandex Translate
│   ├── TranslationSettingsWindow.xaml/cs  # Настройки перевода (Google/Yandex, ключи)
│   ├── TtsProvider.cs          # TTS: Google + Deepgram Aura, кеширование
│   ├── VolumeWidget.cs         # Виджет громкости
│   ├── WaveformGraph.cs        # Волновая форма (SkiaSharp) — drag overlay + user segment
│   ├── lang/                   # JSON-файлы локализации (en/ru/de/fr/es.json)
│   └── Icons/                  # Иконки кнопок (8 PNG + app.ico)
├── DiagAnki/                   # Диагностическая утилита
├── Setup/                      # WiX инсталлятор (framework-dependent, ~6.5 MB)
│   ├── Product.wxs             # Явное перечисление файлов, MajorUpgrade AllowSameVersionUpgrades="yes"
│   └── Setup.wixproj           # WiX v4 проект
├── docs/                       # GitHub Pages (Privacy Policy EN+RU)
├── plans/                      # Архитектурные планы
├── skills/                     # Навыки для AI-ассистента (wix-installer, wpf-i18n, wpf-adaptive-layout, dotnet-publish)
├── AI_NOTES.md                 # Anki справочник + тех. находки (все баги и решения)
├── AGENTS.md                   # Правила для AI-ассистента (Zoo Code)
├── RESUME_FOR_NEW_CHAT.md      # Этот файл
└── RepeatSegment.sln
```

## Ключевые файлы — что где лежит:

| Файл | Назначение |
|------|-----------|
| [`RepeatSegment.App/MainWindow.xaml`](RepeatSegment.App/MainWindow.xaml) | Главное окно: плеер, транскрипция, перевод, Resources (кисти), TextButtonStyle, статусбар (Border+TextBlock) |
| [`RepeatSegment.App/MainWindow.xaml.cs`](RepeatSegment.App/MainWindow.xaml.cs) | Вся логика: загрузка, транскрипция, сегментация, тема, recent files, ApplyAllStrings, GrowWindowForTranslation |
| [`RepeatSegment.App/Strings.cs`](RepeatSegment.App/Strings.cs) | I18n: загрузка JSON, fallback en, LangDir-резолюция, User Guide (жёстко в коде) |
| [`RepeatSegment.App/lang/*.json`](RepeatSegment.App/lang/en.json) | 5 языковых файлов (152 ключа): EN, RU, DE, FR, ES |
| [`RepeatSegment.App/App.xaml.cs`](RepeatSegment.App/App.xaml.cs) | WebView2 warm-up (`CoreWebView2Environment.CreateAsync` в фоне) |
| [`RepeatSegment.App/ConfigManager.cs`](RepeatSegment.App/ConfigManager.cs) | Все настройки: segment_duration_sec, theme, mp3_bitrate, язык и т.д. |
| [`RepeatSegment.App/AudioEngine.cs`](RepeatSegment.App/AudioEngine.cs) | NAudio: SaveSnippetMp3, SaveSnippetWav, WSOLA-растяжение |
| [`RepeatSegment.App/WaveformGraph.cs`](RepeatSegment.App/WaveformGraph.cs) | SkiaSharp: waveform, drag overlay, user segment (зелёный), cursor, ruler |
| [`RepeatSegment.App/AnkiBuilder.cs`](RepeatSegment.App/AnkiBuilder.cs) | Сборка .apkg: SQLite + ZIP + media.json |
| [`RepeatSegment.App/AnkiCardWindow.xaml.cs`](RepeatSegment.App/AnkiCardWindow.xaml.cs) | Карточка: авто-поиск картинок (Google/Yandex), TTS, WebView2 |
| [`Setup/Product.wxs`](Setup/Product.wxs) | WiX установщик: framework-dependent, MajorUpgrade AllowSameVersionUpgrades="yes" |
| [`AI_NOTES.md`](AI_NOTES.md) | Полный справочник: Anki, WiX, i18n, адаптивные размеры, все баги и решения |
| [`AGENTS.md`](RepeatSegment.App/AGENTS.md) | Правила для AI-ассистента |

## NuGet пакеты:
- Microsoft.Data.Sqlite 10.0.9
- NAudio 2.2.1
- NAudio.Lame 2.1.0 (MP3 конвертация)
- Microsoft.Web.WebView2 1.0.4022.49
- SkiaSharp.Views.WPF 2.88.7
- OxyPlot.Wpf 2.2.0
- System.Configuration.ConfigurationManager 8.0.0

## Сборка и публикация
- **Сборка**: `dotnet build -c Release`
- **Тесты**: `dotnet test`
- **WiX установщик**: `dotnet build Setup/Setup.wixproj -c Release` → `Setup/bin/Release/RepeatSegment-Installer.msi` (~6.5 МБ)
- **Перед пересборкой WiX**: очистить `Setup/bin/` и `Setup/obj/`
- **Framework-dependent** (для WiX): `<SelfContained>false</SelfContained>` в csproj

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
18. **ComboBox в дочерних окнах**: нужен полный `ControlTemplate` с `{DynamicResource InputBgBrush}`
19. **TextButtonStyle**: кнопки без кастомного ControlTemplate показывают системный голубой hover в тёмной теме
20. **Theme switch + транскрипция**: после `ApplyTheme()` перестроить параграф: `BuildWordsParagraph(); _lastHlIdx = -1`

### i18n
21. **`Strings.SetLanguage("en")` в конструкторе MainWindow ДО `LoadOnStartup`** — иначе меню показывает raw key strings
22. **Смена языка → перезапуск**: `Process.Start(exe) + Environment.Exit(0)`
23. **Новые ключи**: добавлять во все 5 JSON-файлов (en/ru/de/fr/es)

### WiX Installer
24. **Framework-dependent только**: self-contained несовместим с WiX (5308 ошибок ICE30 из-за дубликатов имён в .NET Runtime)
25. **`<SelfContained>false</SelfContained>`** в csproj обязательно
26. **Явное перечисление файлов** в Product.wxs — wildcard нестабилен
27. **`MajorUpgrade AllowSameVersionUpgrades="yes"`** — иначе старая версия не перезаписывается
28. **lang/ JSON** брать из исходной папки, не из Publish/Release (их там нет при framework-dependent)
29. **Без UI диалога** — `WixUI_Minimal` не работает в WiX v4

### WebView2
30. **Warm-up**: `CoreWebView2Environment.CreateAsync` в `App.OnStartup()` предотвращает задержку 3–10 сек
31. **Одна папка userData**: `Path.Combine(LocalAppData, "RepeatSegment", "WebView2")` — и в App, и в AnkiCardWindow

### Кодировка
32. **Всегда `Encoding.UTF8`** при `File.WriteAllLines`/`File.ReadAllLines`
33. **UTF-8 без BOM** для `media.json`: `new UTF8Encoding(false)`

### Адаптивные размеры
34. **Никаких хардкод-пикселей**: все размеры через `Math.Max(порог, WorkArea * доля)`
35. **GrowWindowForTranslation**: использовать `_baseWindowH` (фиксируется один раз), а не `ActualHeight`

### Запуск приложения
36. Не использовать `ShowDialog()` в `OnStartup` до `base.OnStartup(e)` с `StartupUri`
37. `ShutdownMode = OnMainWindowClose` при ручном создании MainWindow

## Известные TODO / недоделки:
- Auto-play **осознанно отключён** (`"autoplay":false` в dconf)
- Слияние колод может дублировать медиафайлы (нет content-based дедупликации в BuildDeck)
- `dcid` timestamp-based — накопление старых dconf-записей (не критично)
- **Store публикация**: ожидание регистрации в Partner Center → MSIX пакет
- Burn bootstrapper для WiX (автоустановка .NET 8 Runtime) — опционально
