# AI Notes — RepeatSegment C# Port

> **Версия:** 15.06.2026
> **Статус:** ✅ v1.1 — 0 ошибок, Deepgram + AssemblyAI, mouse-drag сегмент, установщик с авто-обновлением, GitHub

---

## Структура проекта

| Файл | Роль | Статус |
|------|------|--------|
| `MainWindow.xaml` | UI: Меню→Волна→Слайдер→Кнопки→Транскрипция→Статус | ✅ |
| `MainWindow.xaml.cs` | Логика окна (~220 строк) | ✅ |
| `ManualWindow.xaml/.cs` | Окно инструкции (Help → User Guide) | ✅ |
| `AudioEngine.cs` | NAudio MP3/WAV load + `WaveOutEvent` | ✅ |
| `SilenceDetector.cs` | dBFS-based silence detection | ✅ |
| `TranscriptionProvider.cs` | Deepgram + AssemblyAI, кеширование | ✅ |
| `WaveformGraph.cs` | SkiaSharp волна + mouse-drag выделение сегментов | ✅ |
| `SettingsWindow.xaml/.cs` | API ключи, провайдеры | ✅ |
| `VolumeWidget.cs` | 5-полосный виджет громкости | ✅ |
| `ConfigManager.cs` | INI config, save/restore progress | ✅ |
| `config.template.ini` | Шаблон конфига с API-ключами (для установщика) | ✅ |

### Инфраструктура
| Файл | Роль |
|------|------|
| `Setup/Product.wxs` | WiX v5 — описание пакета (v1.1, MajorUpgrade) |
| `Setup/Setup.wixproj` | WiX проект |
| `Setup/bin/Debug/RepeatSegment-Installer.msi` | Готовый установщик |
| `.gitignore` | Исключения Git |
| `AI_NOTES.md` | Этот файл |

---

## Что сделано

### AssemblyAI
- ✅ **Исправлен баг с Authorization header** — .NET silently ignores `Headers = { {"authorization", key} }`. Использовать ТОЛЬКО `req.Headers.Authorization = new AuthenticationHeaderValue(apiKey)`.
- ✅ API ключ валиден
- ⚠️ **AssemblyAI заблокирован на территории РФ** — запросы падают с `SocketException 10054` на TLS-уровне. Требуется VPN.
- ✅ Парсинг ответа: `start`/`end` — миллисекунды (int64 → `/1000`)

### Deepgram
- ✅ Работает без VPN
- ✅ Авторизация: `new AuthenticationHeaderValue("Token", apiKey)`

### Кеширование
- ✅ `SaveChunkToCache` — сохранение в `%APPDATA%/RepeatSegment/output/`
- ✅ `LoadChunkFromCache` — загрузка из кеша
- ✅ `Transcribe(forceFresh: false)` — только кеш, без API

### Mouse-drag сегмент на волне
- ✅ Зажать ЛКМ → подсветка золотистым → отпустить → новый сегмент
- ✅ Корректная перестройка фрагментов: обрезка, слияние, вставка
- ✅ Плеер переходит в начало нового сегмента

### UI
- ✅ Тёмный title bar через `DwmSetWindowAttribute(DWMWA_USE_IMMERSIVE_DARK_MODE)`
- ✅ Иконка через `ApplicationIcon` в csproj + `SetWindowIcon()` из `BaseDirectory/Icons/app.ico`
- ✅ Окно инструкции: Help → User Guide
- ✅ About: "Version 1.1, 2026. Python ported to C# via AI (VS Code + Zoo Code + DeepSeek API)"
- ✅ Меню: кастомный ControlTemplate (тонкие границы), StackPanel вместо WrapPanel
- ✅ Кнопки: адаптивный размер через `AdaptToScreen()`, HoverBorder в обрез
- ✅ Скрыты нереализованные провайдеры (VK, Yandex, Salute) в Settings
- ✅ Транскрипция перекрашивается при смене темы (`BuildWordsParagraph` в `ApplyTheme`)

### Установщик
- ✅ WiX Toolset v5, `PublishSingleFile=true` → один EXE (~173 MB, self-contained)
- ✅ `MajorUpgrade` — установка поверх старых версий
- ✅ `config.ini` убран из MSI (не затирает при обновлении)
- ✅ Миграция/создание `config.ini` в `%APPDATA%` при первом запуске
- ✅ Shortcut в Start Menu + Desktop
- ✅ WixUI_InstallDir (выбор папки установки)
- ✅ Деинсталляция через стандартные средства Windows

### Данные
- ✅ `config.ini` → `%APPDATA%/RepeatSegment/config.ini`
- ✅ `output/` → `%APPDATA%/RepeatSegment/output/`

### Git / GitHub
- ✅ git init + коммит
- ✅ `.gitignore`
- ✅ Push: https://github.com/Gusev-Sergey/RepeatSegment

---

## Что НЕ трогать (работает отлично)

### Логика кнопок
```csharp
// MainWindow.xaml.cs — ButtonsPressed(key)
// Кнопки: first | preplay | repeat | play_go | play | next | last
// "repeat" = воспроизвести сегмент и остановиться
// "play" = play-to-end (непрерывно)
// "play_go" = перейти к следующему сегменту и остановиться
// SkipToFragment не меняет _counter для "play"/"replay"
```

### Подсветка слов
```csharp
// MainWindow.xaml.cs — HighlightActiveWord()
Run[]? _wordRuns;   // прямой доступ к Run по индексу слова
Run[]? _spaceRuns;  // пробелы между словами
// Подсвечивает ТОЛЬКО слово (не пробел)
// Жёлтый фон #FFD700 + белый текст + Bold
// cur.ElementStart.GetCharacterRect + ScrollToVerticalOffset для авто-прокрутки
// Поиск активного слова: линейный вперёд от _lastHlIdx
```

### Детектор тишины
```csharp
// SilenceDetector.cs — dBFS = 20*log10(RMS_all)
// silenceThreshDB = dBFS - offset (20 dB)
// Per-window dBFS сравнение
// СОВМЕСТИМ с pydub silence.detect_silence
```

### График волны
```csharp
// WaveformGraph.cs
// WindowWidthSec = 25.0
// Сегментные линии: оранжевый #FF8C00, толщина 4px, штрих 8-4
// Шкала времени: 22px снизу, метки каждые 5 сек
// Mouse-drag: OnMouseLeftButtonDown → OnMouseMove → OnMouseLeftButtonUp
// DragOverlay: золотой SKColor(0xFF, 0xD7, 0x00, 0x60)
// Событие SegmentSelected(Action<double,double>) — подписка в MainWindow конструкторе
```

### Перестройка фрагментов (WaveformGraph_SegmentSelected)
```csharp
// MainWindow.xaml.cs — логика слияния:
// - Фрагменты слева/справа от диапазона → сохраняются
// - Фрагменты перекрывающие диапазон → обрезаются
// - Фрагменты охватывающие диапазон → разрезаются на два
// - Фрагменты внутри диапазона → удаляются
// - Новый сегмент (t1,t2) вставляется, сортировка по T1
```

---

## Что ДОРАБОТАТЬ

### 1. Другие провайдеры (VK Cloud, Yandex, SaluteSpeech)
- В XAML скрыты (`Visibility="Collapsed"`)
- Код в `TranscribeChunkAsync` → `_ => null`
- Нужно реализовать методы транскрипции

### 2. Подгрузка чанков при проигрывании
- `HighlightActiveWord()` вызывает `EnsureChunk(ci)` для следующих чанков
- Но чанки загружаются fire-and-forget — нет ожидания
- Может не успеть за проигрыванием

### 3. Иконка в таскбаре
- Периодически пропадает (Windows кеширует старую)
- `ApplicationIcon` в csproj + `SetWindowIcon()` должны решать
- При установке через MSI на чистой машине — работает

---

## Идеи (nice to have)

- **Индикатор прогресса транскрибации на волне**
- **Drag-and-drop файлов** на окно
- **Логирование в файл** (сейчас только Debug.WriteLine)
- **Настройка громкости колесом мыши**
- **Экспорт транскрипции в файл**
- **Поддержка видео-файлов** (извлечение аудиодорожки)

---

## Critical Rules

### Authorization header
```csharp
// ❌ WRONG — silently ignored by .NET
Headers = { { "authorization", apiKey } }

// ✅ CORRECT
req.Headers.Authorization = new AuthenticationHeaderValue(apiKey);
// Deepgram: new AuthenticationHeaderValue("Token", apiKey);
// AssemblyAI: new AuthenticationHeaderValue(apiKey);
```

### Brushes — всегда new SolidColorBrush()
```csharp
// ❌ WRONG — frozen brush exception
Resources["TextBrush"].Color = newColor;

// ✅ CORRECT
Resources["TextBrush"] = new SolidColorBrush(newColor);
```

### Конфиг и данные — %APPDATA%
```csharp
string AppDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment");
// config.ini → AppDataDir
// output/ → AppDataDir/output
```

### Menu XAML
- Кастомный ControlTemplate в MainWindow.xaml
- `MenuItem.Margin="-4,0,-4,0"` в `Menu.Resources` — убирает системные зазоры
- `ItemsPanelTemplate → StackPanel` вместо WrapPanel

### SKColor — порядок аргументов важен
```csharp
// ❌ WRONG — прозрачность как R
new SKColor(0x60, 0xFF, 0xD7, 0x00)

// ✅ CORRECT — R, G, B, A
new SKColor(0xFF, 0xD7, 0x00, 0x60)
```

---

## Deepgram JSON (пример ответа)

```json
{
  "results": {
    "channels": [{
      "alternatives": [{
        "transcript": "Chapter one...",
        "words": [
          {"word": "chapter", "start": 0.08, "end": 0.58, "punctuated_word": "Chapter"}
        ]
      }]
    }]
  }
}
```

**Важно:** `results` — **ОБЪЕКТ**, не массив!

## AssemblyAI JSON (пример ответа)

```json
{
  "id": "...", "status": "completed",
  "text": "Chapter one...",
  "words": [
    {"text": "Chapter", "start": 80, "end": 580, "confidence": 0.99}
  ]
}
```

`start`/`end` — **миллисекунды, целые числа**.

---

## Сборка и деплой

```bash
# Debug
dotnet build RepeatSegment.App\RepeatSegment.App.csproj -c Debug

# Publish (self-contained single file)
rd /s /q Publish
dotnet publish RepeatSegment.App\RepeatSegment.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=none -o Publish

# MSI
cd Setup && dotnet build
# → Setup\bin\Debug\RepeatSegment-Installer.msi
```

**Git push:**
```bash
cd c:\ProjectsCSharp\RepeatSegment
git add -A && git commit -m "update" && git push
```

Предупреждения: 4 штуки CS1998 (async без await) — не критично.

---

## Связь с Python оригиналом

| Python | C# |
|--------|-----|
| `main.py` → `DlgMain` | `MainWindow` |
| `audio_engine.py` → `AudioEngine` | `AudioEngine.cs` |
| `silence_detector.py` → `SilenceDetector` | `SilenceDetector.cs` |
| `transcription.py` → `TranscriptionProvider` | `TranscriptionProvider.cs` |
| `config_manager.py` → `ConfigManager` | `ConfigManager.cs` |
| `ui/metrics.py` → `UiMetrics` | `AdaptToScreen()` в MainWindow |
| PyQt5 `QTimer` | WPF `DispatcherTimer` |
| PyQt5 `sounddevice.play` | NAudio `WaveOutEvent` |
| `matplotlib` график | SkiaSharp `SKElement` |
