# RepeatSegment — Resume

**WPF .NET 8** | Изучение английского по аудиокнигам + экспорт в Anki
GitHub: `github.com/Gusev-Sergey/RepeatSegment` | Path: `c:\ProjectsCSharp\RepeatSegment`

## Функционал (18 июня 2026)

- 📁 MP3/WAV → волновая форма (SkiaSharp) + авторазбивка по тишине
- 🎙️ Транскрипция: Deepgram/AssemblyAI, **11 языков**, smart chunk boundaries (не режет слова)
- 🌐 Перевод: Google (default) / Yandex (опционально), 3 ретрая
- 🃏 **Anki .apkg**: 2 карточки (en→ru + ru→en), двойное аудио (🔊 TTS + 📖 Sentence), картинки
- 🗣️ **TTS**: Deepgram Aura + Google fallback, кеш на диск
- 🖼️ Картинки: WebView2 → Yandex Images, JPEG сжатие q=75, автоочистка
- 🌍 I18n: EN/RU словари, меню Language, User Guide с иконками
- 📝 Логи: `%APPDATA%/RepeatSegment/repeat_segment.log`
- 🔒 Pre-commit хук против утечек ключей, `.env`/`config.ini` в `.gitignore`

## Ключевые файлы

| Файл | За что |
|------|--------|
| `AnkiBuilder.cs` | Сборка .apkg (ZIP+SQLite+модель) |
| `AnkiExportManager.cs` | AddMedia/AddNote/Finalize + merge |
| `TranscriptionProvider.cs` | Deepgram/AssemblyAI, чанки, smart границы |
| `TranslationProvider.cs` | Google/Yandex перевод |
| `TtsProvider.cs` | Deepgram/Google TTS |
| `AudioEngine.cs` | NAudio: загрузка, SaveSnippetMp3, логи |
| `Strings.cs` | EN/RU словари + User Guide |

## ⚠️ Критические правила (не нарушать)

### Anki
1. **mid и name модели — всегда уникальные** при изменении шаблонов (Anki кеширует)
2. `[sound:...]` на отдельной строке, без HTML перед ним. `<audio>` не работает
3. `bury:false` в dconf, иначе sibling-карточки скрыты
4. `req: "any"` (не `"all"`), иначе карточка не генерируется без картинки
5. **en→ru лицо:** word + transcription + sound + `<br>` + image. **Оборот:** FrontSide + `<hr>` + translation + context
6. **ru→en лицо:** translation + image. **Оборот:** FrontSide + `<hr>` + word + transcription + sound + context

### Запуск
7. StartupUri обязателен. Не использовать ShowDialog в OnStartup
8. ShutdownMode = OnMainWindowClose при ручном MainWindow

### Остальное
9. Язык транскрипции через `_cfg.TranscriptionLanguage`, не хардкодить
10. Кэш: `..._chunk0000_deepgram_{lang}.json`
11. Картинки: JPEG q=75, ресайз ≤600px, маленькие не трогать
