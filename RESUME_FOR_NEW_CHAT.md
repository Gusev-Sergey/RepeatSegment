# Resume for New Chat — RepeatSegment Project

## Project Overview
**RepeatSegment** — C# WPF (.NET 8) приложение для изучения английского языка через аудиокниги. Позволяет загружать аудио, транскрибировать через Deepgram/AssemblyAI, переводить слова/фразы через Google Translate, выделять сегменты мышкой на волновой форме и экспортировать карточки в Anki (.apkg).

**GitHub**: https://github.com/Gusev-Sergey/RepeatSegment
**Путь**: `c:\ProjectsCSharp\RepeatSegment`
**Бэкап**: `c:\ProjectsCSharp\RepeatSegment_Backup`

## Текущее состояние (18 июня 2026, ~1:00 MSK)

### Работает:
- Загрузка аудиофайлов и отображение волновой формы (OxyPlot)
- Транскрипция через Deepgram API (с пословным таймингом `WordTimings`)
- Перевод через Google Translate (en→ru), 3 ретрая при rate-limit; выбор сервиса в Settings (Google/Yandex)
- Ручное выделение сегментов мышкой на графике
- Подсветка активного слова при воспроизведении
- **Anki экспорт** — ПОЛНОСТЬЮ РАБОЧИЙ
- **Двойное аудио в карточках Anki** — sentence из книги + TTS слова (Deepgram/Google)
- **TTS** — произнесение слов через Deepgram Aura + Google TTS fallback (кешируется)
- WebView2 поиск картинок (Yandex Images), автоочистка кеша при закрытии
- Запись аудио с микрофона (NAudio)
- IPA транскрипция через dictionaryapi.dev + Wiktionary
- Инсталлятор (WiX)
- **Файловое логирование** (`%APPDATA%/RepeatSegment/repeat_segment.log`)
- **Pre-commit хук** — блокирует коммиты с API-ключами

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
| [`RepeatSegment.App/.env.template`](RepeatSegment.App/.env.template) | Шаблон для .env с ключами |
| [`plans/tts_dual_audio_plan.md`](plans/tts_dual_audio_plan.md) | Архитектурный план TTS + dual audio |
| [`.git/hooks/pre-commit`](.git/hooks/pre-commit) | Pre-commit хук (защита от утечек) |
| `.git/hooks/pre-commit.ps1` | PowerShell версия хука |

## Известные TODO / недоделки:
- Anki auto-play проигрывает все `[sound:...]` теги подряд (обход: уникальный dconf id)
- Слияние колод работает но может дублировать медиафайлы при повторном создании
- Deepgram ключ был отозван (засвечен) — получен новый, актуален
- AssemblyAI ключ пока жив, но тоже был в истории — может быть отозван
- `autoplay` в dconf кешируется Anki локально — обход через уникальный `dcid`
