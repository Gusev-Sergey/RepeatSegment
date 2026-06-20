# Resume for New Chat — RepeatSegment Project

## Project Overview
**RepeatSegment** — C# WPF (.NET 8) приложение для изучения английского языка через аудиокниги. Позволяет загружать аудио, транскрибировать через Deepgram/AssemblyAI, переводить слова/фразы через Google Translate, выделять сегменты мышкой на волновой форме и экспортировать карточки в Anki (.apkg).

**GitHub**: https://github.com/Gusev-Sergey/RepeatSegment
**Путь**: `c:\ProjectsCSharp\RepeatSegment`
**Бэкап**: `c:\ProjectsCSharp\RepeatSegment_Backup`

## Текущее состояние (18 июня 2026, ~02:00 MSK)

### Работает:
- Загрузка аудиофайлов и отображение волновой формы (SkiaSharp)
- Транскрипция через Deepgram/AssemblyAI (с пословным таймингом `WordTimings`), **11 языков**
- **Smart chunk boundaries** — границы чанков привязываются к тишине, не режут слова
- Перевод через Google Translate (en→ru), 3 ретрая при rate-limit; выбор сервиса в Settings (Google/Yandex)
- Ручное выделение сегментов мышкой на графике
- Подсветка активного слова при воспроизведении
- **Anki экспорт** — ПОЛНОСТЬЮ РАБОЧИЙ (две карточки: en→ru + ru→en)
- **Двойное аудио в карточках Anki** — 🔊 Word (TTS) + 📖 Sentence (из книги)
- **TTS** — произнесение слов через Deepgram Aura + Google TTS fallback (кешируется на диск)
- WebView2 поиск картинок (Yandex Images), автоочистка кеша при закрытии, JPEG сжатие quality=75
- Запись аудио с микрофона (NAudio)
- IPA транскрипция через dictionaryapi.dev + Wiktionary
- Инсталлятор (WiX)
- **Файловое логирование** (`%APPDATA%/RepeatSegment/repeat_segment.log`)
- **Pre-commit хук** — блокирует коммиты с API-ключами
- **Интернационализация** — EN/RU словари, меню Language с сохранением в config.ini
- **User Guide** — RichTextBox с иконками кнопок, двухязычный, 11 разделов
- **Схлопывание транскрипции** — кнопка ▲/▼ слева-внизу от плеера

### Безопасность (важно):
- Ключи в `config.template.ini` заменены на `YOUR_*` плейсхолдеры
- Вся git-история очищена от реальных ключей (`git filter-branch` + force push)
- `config.ini` и `.env` в `.gitignore`
- `.env.template` для справки

### Anki .apkg экспорт — КЛЮЧЕВЫЕ ФАЙЛЫ:
- [`RepeatSegment.App/AnkiBuilder.cs`](RepeatSegment.App/AnkiBuilder.cs) — основная логика сборки .apkg
- [`RepeatSegment.App/AnkiExportManager.cs`](RepeatSegment.App/AnkiExportManager.cs) — API для UI (AddMedia, AddNote, Finalize)
- [`RepeatSegment.App/AnkiCardWindow.xaml`](RepeatSegment.App/AnkiCardWindow.xaml) — UI форма карточки
- [`RepeatSegment.App/AnkiCardWindow.xaml.cs`](RepeatSegment.App/AnkiCardWindow.xaml.cs) — логика UI карточки
- [`RepeatSegment.App/AudioEngine.cs`](RepeatSegment.App/AudioEngine.cs) — SaveSnippetMp3(), SaveSnippetWav()
- [`AI_NOTES.md`](AI_NOTES.md) — полный справочник по Anki формату

### Критические правила Anki (выстраданы):
1. Медиафайлы в ZIP: **числовые имена без расширений** (`"0"`, `"1"`)
2. `media.json`: `{"0":"m0.jpg","1":"m1.mp3"}` — UTF-8 без BOM
3. Поле image: `<img src="m0.jpg">`
4. Поле sound: `[sound:m1.mp3]` (MP3 обязателен, WAV не работает)
5. Модель: поля `word/transcription/translation/image/sound/context`
6. Шаблоны: `{{FrontSide}}` + `<hr id=answer>` для сохранения лица на обороте
7. `mid` = `1728000001L`, `csum` = `unchecked((int)Cr32(f))`
8. CSS: тёмная тема `background-color:#1e1e1e`

## Структура проекта
```
RepeatSegment/
├── RepeatSegment.App/          # Основное WPF приложение
│   ├── AnkiBuilder.cs          # Сборка .apkg (DROP TABLE, ZipFile)
│   ├── AnkiExportManager.cs    # API: AddMedia/AddNote/Finalize + слияние
│   ├── AnkiCardWindow.xaml     # UI форма карточки
│   ├── AnkiCardWindow.xaml.cs  # Логика: поиск картинок, аудио, создание
│   ├── AudioEngine.cs          # SaveSnippetMp3 (NAudio.Lame)
│   ├── MainWindow.xaml/cs      # Главное окно
│   ├── WaveformGraph.cs        # Волновая форма (OxyPlot)
│   ├── TranscriptionProvider.cs # Deepgram/AssemblyAI API
│   ├── TranslationProvider.cs  # Google Translate
│   └── ...
├── DiagAnki/                   # Диагностическая утилита
│   ├── Program.cs              # Тесты .apkg
│   └── DiagAnki.csproj
├── Setup/                      # WiX инсталлятор
├── AI_NOTES.md                 # Anki справочник
└── RepeatSegment.sln
```

## NuGet пакеты:
- Microsoft.Data.Sqlite 10.0.9
- NAudio 2.2.1
- NAudio.Lame 2.1.0 (MP3 конвертация)
- Microsoft.Web.WebView2 1.0.4022.49
- SkiaSharp.Views.WPF 2.88.7
- OxyPlot.Wpf 2.2.0

## Новые ключевые файлы:
| Файл | Назначение |
|------|-----------|
| [`RepeatSegment.App/TtsProvider.cs`](RepeatSegment.App/TtsProvider.cs) | TTS: Deepgram Aura + Google fallback |
| [`RepeatSegment.App/Strings.cs`](RepeatSegment.App/Strings.cs) | I18n: EN/RU словари + User Guide |
| [`RepeatSegment.App/.env.template`](RepeatSegment.App/.env.template) | Шаблон для .env с ключами |
| [`plans/tts_dual_audio_plan.md`](plans/tts_dual_audio_plan.md) | Архитектурный план TTS + dual audio |
| [`plans/i18n_and_userguide.md`](plans/i18n_and_userguide.md) | Архитектурный план i18n |
| [`plans/android_port_plan.md`](plans/android_port_plan.md) | План портирования на Android |
| [`.git/hooks/pre-commit`](.git/hooks/pre-commit) | Pre-commit хук (защита от утечек) |
| `.git/hooks/pre-commit.ps1` | PowerShell версия хука |

## Известные TODO / недоделки:
- Auto-play **осознанно отключён** (`"autoplay":false` в dconf) — при двух аудио (sentence + TTS) Anki проигрывает все `[sound:...]` подряд, нельзя выбрать конкретный
- Слияние колод работает но может дублировать медиафайлы при повторном создании (нет content-based дедупликации в BuildDeck)
- `autoplay` в dconf кешируется Anki локально — **решено** через уникальный `dcid` (timestamp-based)

## ⚠️ Критические правила (выстраданы, не нарушать)

### Anki
1. **mid** всегда уникальный при изменении шаблонов — иначе Anki кеширует старую модель
2. **name модели** тоже должен быть уникальным — Anki deduplicates по name, а не только по mid
3. **`[sound:...]`** должен быть на отдельной строке, без HTML-тегов перед ним
4. HTML `<audio>` тег **не работает** в поле `{{sound}}` — только `[sound:...]`
5. **`bury:false`** в dconf — иначе sibling-карточки скрываются
6. **`req`** используй `"any"` вместо `"all"` — иначе карточка не генерируется без картинки/контекста

### Запуск приложения
7. **Не использовать `FirstRunWindow` с `ShowDialog()` в `OnStartup`** — WPF завершает приложение когда закрывается последнее окно. StartupUri должен быть всегда
8. Если нужно диалоговое окно до MainWindow — делать его в `OnStartup` ДО `base.OnStartup(e)`
9. **`ShutdownMode = OnMainWindowClose`** обязательно при ручном создании MainWindow

### Транскрипция
10. Язык передаётся через `_cfg.TranscriptionLanguage` — не хардкодить `"en"`
11. Кэш-файлы именуются с языком: `..._chunk0000_deepgram_{lang}.json`

### Anki карточки — расположение
12. **en→ru лицо:** word + transcription + sound + `<br>` + image. **Оборот:** FrontSide + `<hr>` + translation + context
13. **ru→en лицо:** translation + image. **Оборот:** FrontSide + `<hr>` + word + transcription + sound + context
14. **Image сжатие:** JPEG quality 75, ресайз до 600px. Маленькие картинки не пережимать
