# Аудит соответствия правилам — 21 июня 2026

Сверка 22 правил из [`RESUME_FOR_NEW_CHAT.md`](RESUME_FOR_NEW_CHAT.md#⚠️-критические-правила-выстраданы-не-нарушать) и находок из [`AI_NOTES.md`](AI_NOTES.md).

## ✅ Правила, которые соблюдены

| # | Правило | Статус | Где проверено |
|---|---------|--------|---------------|
| 1 | mid уникальный при изменении шаблонов | ✅ | `mid = 1728005000L + timestamp % 1000000L` — динамический (строка 77) |
| 2 | name модели уникальный | ✅ | `"RepeatSegment " + mid` (строка 110) |
| 3 | `[sound:...]` на отдельной строке | ✅ | `string.Join("\n", sndParts)` (строка 94) |
| 4 | HTML `<audio>` не используется | ✅ | Только `[sound:]` |
| 5 | `bury:false` в dconf | ✅ | Строка 118 |
| 6 | `req` использует `"any"` | ⚠️ | `[0,"any",[0]]` — но должно быть 3 элемента как в July_2015? |
| 7 | Числовые имена без расширений в ZIP | ✅ | `m.ZipName` — числа, `mediaMap` маппинг (строки 47-48) |
| 8 | `media.json` UTF-8 без BOM | ✅ | `new UTF8Encoding(false)` (строка 52) |
| 9 | `<img src="m0.jpg">` | ✅ | `img` поле (строка 85) |
| 10 | `[sound:mp3]` — MP3 обязателен | ✅ | Сохраняется через `LameMP3FileWriter` в AudioEngine |
| 11 | Поля модели верны | ✅ | word/transcription/translation/image/sound/context (строка 112) |
| 12 | `{{FrontSide}}` + `<hr id=answer>` | ✅ | Строки 107, 109 |
| 13 | `csum = unchecked((int)Cr32(f))` | ✅ | Строка 96 |
| 14 | CSS: тёмная тема `#1e1e1e` | ✅ | Строка 113 |
| 15 | Frozen Brush — `new SolidColorBrush` | ✅ | `Resources[key] = new SolidColorBrush(kv.Value)` (MainWindow 107) |
| 16 | Title bar DWM | ✅ | Все окна: API, Translation, General, Manual, About |
| 17 | Дочерние окна InjectBrushes | ✅ | Все окна копируют кисти из MainWindow |
| 18 | ComboBox ControlTemplate | ✅ | GeneralSettingsWindow.xaml |
| 19 | Encoding.UTF8 для файлов | ✅ | Recent, Config, TTS — все через UTF8 |
| 20 | UTF-8 без BOM для media.json | ✅ | AnkiBuilder строка 52 |
| 21 | Не хардкодить язык транскрипции | ✅ | `_cfg.TranscriptionLanguage` |
| 22 | SegmentDurationSec в ConfigManager | ✅ | По умолчанию 5.0 |

## ⚠️ Находки — что можно улучшить

### 1. `File.WriteAllText` без Encoding в TranscriptionProvider

[`TranscriptionProvider.cs:192`](RepeatSegment.App/TranscriptionProvider.cs:192) — `File.WriteAllText(filePath, json)` без указания кодировки. По умолчанию .NET использует UTF-8 без BOM, так что это **формально ок**, но неконсистентно с остальным кодом, где кодировка явная.

**Рекомендация**: добавить `System.Text.Encoding.UTF8` для консистентности.

### 2. `config.template.ini` не соответствует `ConfigManager.Load`

[`config.template.ini`](RepeatSegment.App/config.template.ini) содержит устаревшие поля:
- `split_interval = 100` — должно быть `segment_duration_sec = 5.0`
- Отсутствуют поля: `segment_duration_sec`, `theme`, `mp3_bitrate`

**Рекомендация**: обновить config.template.ini до актуальной схемы.

### 3. Неиспользуемый пакет NuGet: OxyPlot.Wpf

В [`RepeatSegment.App.csproj`](RepeatSegment.App/RepeatSegment.App.csproj) есть `<PackageReference Include="OxyPlot.Wpf" Version="2.2.0"/>`, но **ни один файл не содержит `using OxyPlot` или `OxyPlot.`**. WaveformGraph использует SkiaSharp.

**Рекомендация**: удалить пакет OxyPlot.Wpf — уменьшит размер сборки.

### 4. Неиспользуемый файл: RSWindow.cs

[`RSWindow.cs`](RepeatSegment.App/RSWindow.cs) задуман как базовый класс для тёмного title bar, но dark title bar реализован в каждом окне отдельно через `Window_Loaded`. Файл не используется.

**Рекомендация**: либо использовать RSWindow как базовый класс, либо удалить файл.

### 5. `App.xaml.cs` — пустой Startup

[`App.xaml.cs`](RepeatSegment.App/App.xaml.cs) — только `public partial class App : Application { }`. Нет обработки необработанных исключений (`DispatcherUnhandledException`), что может приводить к «тихим» падениям.

**Рекомендация**: добавить `DispatcherUnhandledException` с записью в лог.

### 6. `_originalHeight` — неиспользуемое поле

[`MainWindow.xaml.cs:110`](RepeatSegment.App/MainWindow.xaml.cs:110) предупреждение CS0649 — поле `_originalHeight` нигде не используется. Заменено на `_baseWindowHeight`, но старое поле осталось.

**Рекомендация**: удалить `_originalHeight`.

### 7. `req` в AnkiBuilder может быть неполным

[`AnkiBuilder.cs:114`](RepeatSegment.App/AnkiBuilder.cs:114) — `req=new[]{new object[]{0,"any",new[]{0}},new object[]{1,"any",new[]{0}}}`. Второй массив ссылается только на поле 0 (word), но ru→en карточка показывает translation+image → должно быть `new[]{2,3}` (translation и image).

**Рекомендация**: проверить на реальной колоде, генерируются ли ru→en карточки без image. Если да — ок. Если нет — исправить на `[1,"any",[2,3,5]]`.

### 8. `dconf` — `autoplay:true` для Default (id=1)

[`AnkiBuilder.cs:118`](RepeatSegment.App/AnkiBuilder.cs:118) — Default deck config имеет `"autoplay":true`. Это влияет на все колоды, использующие Default config. Если пользователь импортирует в существующую колоду — авто-проигрывание включится.

**Рекомендация**: изменить `"autoplay":true` → `"autoplay":false` и для Default конфига.
