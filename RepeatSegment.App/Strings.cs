using System.Collections.Generic;

namespace RepeatSegment.App;

/// <summary>
/// Localized strings for UI. Language set via Strings.SetLanguage("en"/"ru"), persisted in config.ini.
/// All user-facing text goes through Strings.Get(key).
/// </summary>
public static class Strings
{
    public static string CurrentLang { get; private set; } = "en";

    private static readonly Dictionary<string, string> En = new()
    {
        // ── MainWindow ──
        ["mw.title"] = "RepeatSegment",
        ["mw.menu.file"] = "File",
        ["mw.menu.file.load"] = "Load",
        ["mw.menu.file.exit"] = "Exit",
        ["mw.menu.split"] = "Split interval",
        ["mw.menu.split.custom"] = "Custom...",
        ["mw.menu.theme"] = "Theme",
        ["mw.menu.theme.light"] = "Light",
        ["mw.menu.theme.dark"] = "Dark",
        ["mw.menu.lang"] = "Language",
        ["mw.menu.lang.en"] = "English",
        ["mw.menu.lang.ru"] = "Русский",
        ["mw.menu.transcription"] = "Transcription",
        ["mw.menu.transcription.cache"] = "Load from cache",
        ["mw.menu.transcription.api"] = "Request from API",
        ["mw.menu.settings"] = "Settings",
        ["mw.menu.settings.apikeys"] = "API keys...",
        ["mw.menu.help"] = "Help",
        ["mw.menu.help.guide"] = "User Guide",
        ["mw.menu.help.about"] = "About...",
        ["mw.status.ready"] = "Ready",
        ["mw.status.use_file_load"] = "Use File > Load",
        ["mw.status.transcribing"] = "Transcribing...",
        ["mw.status.loading_cache"] = "Loading from cache...",
        ["mw.status.no_cache"] = "No cache",
        ["mw.status.no_cache_detail"] = "No cached data. Use Request from API.",
        ["mw.status.loaded_words"] = "Loaded — {0} words",
        ["mw.status.done_words"] = "Done — {0} words",
        ["mw.status.calling_api"] = "Calling API...",
        ["mw.status.api_zero"] = "API returned 0 words.",
        ["mw.status.transcription"] = "Transcription not loaded. Use Transcription menu.",
        ["mw.translate.original"] = "Translating...",
        ["mw.translate.ready"] = "Translation ready",
        ["mw.translate.ready_via"] = "Translation ready (via {0})",
        ["mw.translate.add_anki"] = "Add to Anki",
        ["mw.translate.add_anki_tip"] = "Create Anki flashcards from the selected word/phrase",
        ["mw.dlg.load_title"] = "Select audio file",
        ["mw.dlg.load_filter"] = "Audio files (*.mp3;*.wav)|*.mp3;*.wav|All files (*.*)|*.*",
        ["mw.dlg.error"] = "Error",
        ["mw.dlg.failed_load"] = "Failed to load",
        ["mw.dlg.load_first"] = "Load audio file first.",
        ["mw.dlg.custom_silence"] = "Enter silence length (ms):",
        ["mw.dlg.custom_title"] = "Custom",
        ["mw.dlg.invalid"] = "Invalid",
        ["mw.dlg.invalid_range"] = "Value 50-5000 ms.",
        ["mw.dlg.about"] = "RepeatSegment — Study English with Audio Books\n\nVersion 0.4, 2026\nC# WPF (.NET 8)\nOriginally developed in Python\nGitHub: github.com/Gusev-Sergey/RepeatSegment",
        ["mw.dlg.about_title"] = "About RepeatSegment",
        ["mw.tip.first"] = "First segment (Home)",
        ["mw.tip.prev"] = "Previous / Rewind (Left)",
        ["mw.tip.repeat"] = "Repeat segment (M)",
        ["mw.tip.playgo"] = "Play and Go to next (Ctrl+Space)",
        ["mw.tip.play"] = "Play/Pause (Space)",
        ["mw.tip.next"] = "Next / Forward (Right)",
        ["mw.tip.last"] = "Last segment (End)",
        ["mw.overlay.transcribing"] = "Transcribing...",
        ["mw.overlay.loading"] = "Loading from cache...",
        ["mw.open_file"] = "Select audio file",

        // ── AnkiCardWindow ──
        ["acw.title"] = "Anki Card",
        ["acw.deck"] = "Deck:",
        ["acw.deck.new"] = "+ New",
        ["acw.deck.open"] = "Open",
        ["acw.deck.open_tip"] = "Open selected .apkg file",
        ["acw.en"] = "EN:",
        ["acw.transcription"] = "Transcription:",
        ["acw.ru"] = "RU:",
        ["acw.context"] = "Context:",
        ["acw.sentence"] = "Sentence:",
        ["acw.sentence_play"] = "▶ Play",
        ["acw.tts"] = "TTS/Rec:",
        ["acw.tts_play"] = "▶ TTS",
        ["acw.rec"] = "🎤 Rec",
        ["acw.rec_tip"] = "Record from microphone",
        ["acw.picture"] = "Picture:",
        ["acw.picture_search"] = "Search",
        ["acw.picture_use"] = "✓ Use",
        ["acw.picture_again"] = "🔍 Search again",
        ["acw.cancel"] = "Cancel",
        ["acw.create"] = "Create Cards",
        ["acw.status.webview_init"] = "Click desired image, then ✓ Use",
        ["acw.status.webview_fail"] = "WebView2 init failed.",
        ["acw.status.webview_notready"] = "WebView2 not ready...",
        ["acw.status.click_first"] = "Click on an image first, then ✓ Use",
        ["acw.status.downloading"] = "Downloading...",
        ["acw.status.image_saved"] = "Image saved ✓",
        ["acw.status.sentence_ok"] = "Sentence extracted ✓",
        ["acw.status.sentence_no"] = "No audio loaded",
        ["acw.status.sentence_bounds"] = "Could not find sentence boundaries; using word bounds",
        ["acw.status.tts_no"] = "TTS not available",
        ["acw.status.tts_downloading"] = "Downloading TTS...",
        ["acw.status.tts_fail"] = "TTS download failed",
        ["acw.status.tts_ok"] = "TTS ready ✓",
        ["acw.status.cards_created"] = "Cards created!",
        ["acw.status.select_deck"] = "Select or create a deck first",
        ["acw.status.no_decks"] = "(no decks — create new)",
        ["acw.status.recording"] = "Recording...",
        ["acw.status.recording_stop"] = "Recording... press Stop when done",
        ["acw.status.recording_saved"] = "Recording saved ✓",
        ["acw.status.file_not_found"] = "Deck file not found",
        ["acw.status.cant_open"] = "Could not open folder",
        ["acw.msg.success"] = "Success",
        ["acw.msg.cards_added"] = "Cards added to deck:\n{0}",
        ["acw.msg.failed"] = "Failed: {0}",
        ["acw.dlg.new_deck"] = "Deck name:",
        ["acw.dlg.new_deck_title"] = "New Anki Deck",
        ["acw.tts_label"] = "TTS: {0}",
        ["acw.sentence_label"] = "sentence {0:F1}s–{1:F1}s",
        ["acw.tts_click"] = "(click Play to download TTS)",
        ["acw.sentence_click"] = "(click Play to extract sentence)",
        ["acw.recorded"] = "Recorded: {0}",
        ["acw.msg.error"] = "Error",
        ["acw.msg.error_detail"] = "Failed: {0}",

        // ── SettingsWindow ──
        ["sw.title"] = "Transcription Settings",
        ["sw.providers"] = "Active providers (checked = try in order)",
        ["sw.assemblyai"] = "AssemblyAI (VPN required from Russia)",
        ["sw.deepgram"] = "Deepgram",
        ["sw.apikeys_header"] = "───── API Keys ─────",
        ["sw.assemblyai_header"] = "───── AssemblyAI ─────",
        ["sw.assemblyai_key"] = "API key:",
        ["sw.assemblyai_warn"] = "⚠ AssemblyAI blocked in Russia — use VPN",
        ["sw.deepgram_header"] = "───── Deepgram ─────",
        ["sw.deepgram_key"] = "API key:",
        ["sw.translation_header"] = "───── Translation ─────",
        ["sw.translation_google"] = "Google Translate (translate.googleapis.com — free, no key needed)",
        ["sw.translation_yandex"] = "Yandex.Translate (requires API key + Folder ID)",
        ["sw.translation_yandex_key"] = "API key:",
        ["sw.translation_yandex_folder"] = "Folder ID:",
        ["sw.general_header"] = "───── General ─────",
        ["sw.chunk_minutes"] = "Chunk minutes:",
        ["sw.highlight_latency"] = "Highlight latency (s):",
        ["sw.ok"] = "OK",
        ["sw.cancel"] = "Cancel",

        // ── ManualWindow ──
        ["manual.title"] = "RepeatSegment — User Guide",
        ["manual.close"] = "Close",
        ["manual.guide"] = "", // large text, handled separately

        // ── FirstRunWindow ──
        ["firstrun.title"] = "RepeatSegment — Setup",
        ["firstrun.prompt"] = "Choose language / Выберите язык",
        ["firstrun.en"] = "🇬🇧 English",
        ["firstrun.ru"] = "🇷🇺 Русский",
    };

    private static readonly Dictionary<string, string> Ru = new()
    {
        ["mw.title"] = "RepeatSegment",
        ["mw.menu.file"] = "Файл",
        ["mw.menu.file.load"] = "Загрузить",
        ["mw.menu.file.exit"] = "Выход",
        ["mw.menu.split"] = "Интервал тишины",
        ["mw.menu.split.custom"] = "Свой...",
        ["mw.menu.theme"] = "Тема",
        ["mw.menu.theme.light"] = "Светлая",
        ["mw.menu.theme.dark"] = "Тёмная",
        ["mw.menu.lang"] = "Язык",
        ["mw.menu.lang.en"] = "English",
        ["mw.menu.lang.ru"] = "Русский",
        ["mw.menu.transcription"] = "Транскрипция",
        ["mw.menu.transcription.cache"] = "Загрузить из кэша",
        ["mw.menu.transcription.api"] = "Запросить через API",
        ["mw.menu.settings"] = "Настройки",
        ["mw.menu.settings.apikeys"] = "API ключи...",
        ["mw.menu.help"] = "Справка",
        ["mw.menu.help.guide"] = "Руководство",
        ["mw.menu.help.about"] = "О программе...",
        ["mw.status.ready"] = "Готов",
        ["mw.status.use_file_load"] = "Файл > Загрузить",
        ["mw.status.transcribing"] = "Транскрибирование...",
        ["mw.status.loading_cache"] = "Загрузка из кэша...",
        ["mw.status.no_cache"] = "Нет кэша",
        ["mw.status.no_cache_detail"] = "Нет данных в кэше. Используйте Запросить через API.",
        ["mw.status.loaded_words"] = "Загружено — {0} слов",
        ["mw.status.done_words"] = "Готово — {0} слов",
        ["mw.status.calling_api"] = "Вызов API...",
        ["mw.status.api_zero"] = "API вернул 0 слов.",
        ["mw.status.transcription"] = "Транскрипция не загружена. Используйте меню Транскрипция.",
        ["mw.translate.original"] = "Переводим...",
        ["mw.translate.ready"] = "Перевод готов",
        ["mw.translate.ready_via"] = "Перевод готов (через {0})",
        ["mw.translate.add_anki"] = "В Anki",
        ["mw.translate.add_anki_tip"] = "Создать карточки Anki из выделенного слова/фразы",
        ["mw.dlg.load_title"] = "Выберите аудиофайл",
        ["mw.dlg.load_filter"] = "Аудио (*.mp3;*.wav)|*.mp3;*.wav|Все файлы (*.*)|*.*",
        ["mw.dlg.error"] = "Ошибка",
        ["mw.dlg.failed_load"] = "Не удалось загрузить",
        ["mw.dlg.load_first"] = "Сначала загрузите аудиофайл.",
        ["mw.dlg.custom_silence"] = "Введите длину тишины (мс):",
        ["mw.dlg.custom_title"] = "Своё значение",
        ["mw.dlg.invalid"] = "Недопустимо",
        ["mw.dlg.invalid_range"] = "Значение 50-5000 мс.",
        ["mw.dlg.about"] = "RepeatSegment — Изучение английского по аудиокнигам\n\nВерсия 0.4, 2026\nC# WPF (.NET 8)\nGitHub: github.com/Gusev-Sergey/RepeatSegment",
        ["mw.dlg.about_title"] = "О программе",
        ["mw.tip.first"] = "Первый сегмент (Home)",
        ["mw.tip.prev"] = "Предыдущий / Назад (Left)",
        ["mw.tip.repeat"] = "Повторить сегмент (M)",
        ["mw.tip.playgo"] = "Играть и дальше (Ctrl+Space)",
        ["mw.tip.play"] = "Играть/Пауза (Space)",
        ["mw.tip.next"] = "Следующий / Вперёд (Right)",
        ["mw.tip.last"] = "Последний сегмент (End)",
        ["mw.overlay.transcribing"] = "Транскрибирование...",
        ["mw.overlay.loading"] = "Загрузка из кэша...",
        ["mw.open_file"] = "Выберите аудиофайл",

        // ── AnkiCardWindow ──
        ["acw.title"] = "Карточка Anki",
        ["acw.deck"] = "Колода:",
        ["acw.deck.new"] = "+ Нов.",
        ["acw.deck.open"] = "Откр.",
        ["acw.deck.open_tip"] = "Открыть выбранный .apkg файл",
        ["acw.en"] = "EN:",
        ["acw.transcription"] = "Транскрипция:",
        ["acw.ru"] = "RU:",
        ["acw.context"] = "Контекст:",
        ["acw.sentence"] = "Предложение:",
        ["acw.sentence_play"] = "▶ Играть",
        ["acw.tts"] = "TTS/Запись:",
        ["acw.tts_play"] = "▶ TTS",
        ["acw.rec"] = "🎤 Зап.",
        ["acw.rec_tip"] = "Запись с микрофона",
        ["acw.picture"] = "Картинка:",
        ["acw.picture_search"] = "Поиск",
        ["acw.picture_use"] = "✓ Взять",
        ["acw.picture_again"] = "🔍 Искать ещё",
        ["acw.cancel"] = "Отмена",
        ["acw.create"] = "Создать карточки",
        ["acw.status.webview_init"] = "Нажмите на картинку, затем ✓ Взять",
        ["acw.status.webview_fail"] = "Ошибка инициализации WebView2.",
        ["acw.status.webview_notready"] = "WebView2 не готов...",
        ["acw.status.click_first"] = "Сначала нажмите на картинку, затем ✓ Взять",
        ["acw.status.downloading"] = "Скачивание...",
        ["acw.status.image_saved"] = "Картинка сохранена ✓",
        ["acw.status.sentence_ok"] = "Предложение извлечено ✓",
        ["acw.status.sentence_no"] = "Аудио не загружено",
        ["acw.status.sentence_bounds"] = "Границы предложения не найдены; используется слово",
        ["acw.status.tts_no"] = "TTS недоступен",
        ["acw.status.tts_downloading"] = "Скачивание TTS...",
        ["acw.status.tts_fail"] = "Ошибка скачивания TTS",
        ["acw.status.tts_ok"] = "TTS готов ✓",
        ["acw.status.cards_created"] = "Карточки созданы!",
        ["acw.status.select_deck"] = "Сначала выберите или создайте колоду",
        ["acw.status.no_decks"] = "(нет колод — создайте новую)",
        ["acw.status.recording"] = "Запись...",
        ["acw.status.recording_stop"] = "Запись... нажмите Stop когда готово",
        ["acw.status.recording_saved"] = "Запись сохранена ✓",
        ["acw.status.file_not_found"] = "Файл колоды не найден",
        ["acw.status.cant_open"] = "Не удалось открыть папку",
        ["acw.msg.success"] = "Успех",
        ["acw.msg.cards_added"] = "Карточки добавлены в колоду:\n{0}",
        ["acw.msg.failed"] = "Ошибка: {0}",
        ["acw.dlg.new_deck"] = "Имя колоды:",
        ["acw.dlg.new_deck_title"] = "Новая колода Anki",
        ["acw.tts_label"] = "TTS: {0}",
        ["acw.sentence_label"] = "предложение {0:F1}с–{1:F1}с",
        ["acw.tts_click"] = "(нажмите Play для скачивания TTS)",
        ["acw.sentence_click"] = "(нажмите Play для извлечения предложения)",
        ["acw.recorded"] = "Записано: {0}",
        ["acw.msg.error"] = "Ошибка",
        ["acw.msg.error_detail"] = "Ошибка: {0}",

        // ── SettingsWindow ──
        ["sw.title"] = "Настройки транскрипции",
        ["sw.providers"] = "Активные провайдеры (галочка = пробовать по порядку)",
        ["sw.assemblyai"] = "AssemblyAI (требуется VPN из России)",
        ["sw.deepgram"] = "Deepgram",
        ["sw.apikeys_header"] = "───── API Ключи ─────",
        ["sw.assemblyai_header"] = "───── AssemblyAI ─────",
        ["sw.assemblyai_key"] = "API ключ:",
        ["sw.assemblyai_warn"] = "⚠ AssemblyAI заблокирован в РФ — используйте VPN",
        ["sw.deepgram_header"] = "───── Deepgram ─────",
        ["sw.deepgram_key"] = "API ключ:",
        ["sw.translation_header"] = "───── Перевод ─────",
        ["sw.translation_google"] = "Google Translate (translate.googleapis.com — бесплатно, без ключа)",
        ["sw.translation_yandex"] = "Yandex.Translate (требуется API ключ + Folder ID)",
        ["sw.translation_yandex_key"] = "API ключ:",
        ["sw.translation_yandex_folder"] = "Folder ID:",
        ["sw.general_header"] = "───── Общие ─────",
        ["sw.chunk_minutes"] = "Минут в чанке:",
        ["sw.highlight_latency"] = "Задержка подсветки (с):",
        ["sw.ok"] = "OK",
        ["sw.cancel"] = "Отмена",

        // ── ManualWindow ──
        ["manual.title"] = "RepeatSegment — Руководство",
        ["manual.close"] = "Закрыть",

        // ── FirstRunWindow ──
        ["firstrun.title"] = "RepeatSegment — Настройка",
        ["firstrun.prompt"] = "Choose language / Выберите язык",
        ["firstrun.en"] = "🇬🇧 English",
        ["firstrun.ru"] = "🇷🇺 Русский",
    };

    public static void SetLanguage(string lang)
    {
        CurrentLang = (lang == "ru") ? "ru" : "en";
    }

    /// <summary>Get localized string by key. Returns key itself if not found.</summary>
    public static string Get(string key)
    {
        if (CurrentLang == "ru" && Ru.TryGetValue(key, out var ru) && ru != null)
            return ru;
        return En.TryGetValue(key, out var en) && en != null ? en : key;
    }

    /// <summary>Get localized string with string.Format arguments.</summary>
    public static string Get(string key, params object[] args)
        => string.Format(Get(key), args);

    // ── User Guide (large text, loaded on demand) ──

    public static string GetUserGuide()
    {
        if (CurrentLang == "ru")
            return RuGuide;
        return EnGuide;
    }

    private const string EnGuide = @"RepeatSegment — Study English with Audio Books

Program for learning English through audiobooks. Splits audio into speech segments (by pauses), allows repeating each segment, and displays transcription text with synchronized word highlighting.

1. Loading Audio
File → Load (Ctrl+O) — select an MP3 or WAV file. The program automatically finds pauses between phrases and splits the audio into segments.

2. Playback Controls
Play — play from current segment to end (Space)
Repeat — repeat segment and stop (M)
Play and Go — play and move to next (Ctrl+Space)
Previous/Next segment — Left / Right arrows
First/Last segment — Home / End
Stop — stop playback (Esc)
Volume is adjusted using the slider to the right of the buttons.

3. Transcription
Before first use, open Settings → API keys and enter the API key of at least one provider (Deepgram or AssemblyAI).
Request from API (Ctrl+T) — send audio for transcription. The result is cached.
Load from cache (Ctrl+L) — load previously obtained transcription.
During playback, the spoken word is highlighted in yellow. Text auto-scrolls.
Note: AssemblyAI is blocked in Russia — VPN required.

4. Translation
Select a word or phrase in the text — it will be automatically translated. The result appears in the bottom panel.
By default, Google Translate is used (free). If you have a Yandex API key, you can switch to Yandex.Translate in Settings.
Click ""Add to Anki"" to create flashcards from the selected text.

5. Silence Interval
The 'Split interval' menu allows selecting audio fragment length: 100, 200, 300, 500, 800 ms or Custom... (50–5000 ms). The smaller the value, the shorter the segments.

6. Anki Export
Click ""Add to Anki"" after translating a word/phrase to open the card creation window.
• Select or create a deck
• Check the English word, transcription, Russian translation
• Context — the sentence from the book (editable)
• Sentence — extract the full sentence audio from the book (▶ Play)
• TTS — download precise word pronunciation from Deepgram/Google (▶ TTS)
• Picture — search for images in Yandex (Search → click image → ✓ Use)
• Record — record your own voice via microphone (🎤 Rec)
Click ""Create Cards"" — two cards are generated: en→ru and ru→en.
Each card contains two audio buttons: 🔊 Word (TTS) and 📖 Sentence (full sentence from the book).

7. TTS & Sentence Audio
TTS (Text-to-Speech) downloads pronunciation of a word/phrase via Deepgram Aura or Google Translate (backup).
Sentence audio is extracted from the original audiobook — the full sentence containing the selected word.
Both audio files are embedded into Anki cards as separate play buttons.

8. Image Search
The card window has a built-in WebView2 browser that searches Yandex Images by the selected word.
Click on any image → ✓ Use to save it to the card.

9. Settings
File → Settings → API keys... allows you to:
• Select active transcription providers (Deepgram, AssemblyAI)
• Enter API keys for Deepgram and AssemblyAI
• Select translation provider (Google Translate or Yandex.Translate)
• Enter Yandex.Translate API key and Folder ID
• Set chunk size for API transcription (minutes)
• Set highlight latency for word tracking

10. Theme & Language
Menu Theme → Light / Dark — switch between themes.
Menu Language → English / Русский — switch interface language. The choice is saved.

11. Hotkeys
Ctrl+O — Load audio file
Space — Play/Pause
Ctrl+Space — Play and Go
M — Repeat segment
← → — Previous/Next segment
Home/End — First/Last segment
Esc — Stop
Ctrl+T — Request transcription from API
Ctrl+L — Load transcription from cache
Ctrl+F — Translate selected text";

    private const string RuGuide = @"RepeatSegment — Изучение английского по аудиокнигам

Программа для изучения английского языка через аудиокниги. Разбивает аудио на речевые сегменты (по паузам), позволяет повторять каждый сегмент, отображает текст транскрипции с синхронной подсветкой произносимых слов.

1. Загрузка аудио
Файл → Загрузить (Ctrl+O) — выберите MP3 или WAV файл. Программа автоматически найдёт паузы между фразами и разобьёт аудио на сегменты.

2. Управление воспроизведением
Play — воспроизведение с текущего сегмента до конца (Space)
Repeat — повторить сегмент и остановиться (M)
Play and Go — проиграть и перейти к следующему (Ctrl+Space)
Предыдущий/Следующий сегмент — стрелки Left / Right
Первый/Последний сегмент — Home / End
Stop — остановить (Esc)
Громкость регулируется ползунком справа от кнопок.

3. Транскрипция
Перед первым использованием откройте Настройки → API ключи и введите API-ключ хотя бы одного провайдера (Deepgram или AssemblyAI).
Запросить через API (Ctrl+T) — отправить аудио на расшифровку. Результат кэшируется.
Загрузить из кэша (Ctrl+L) — загрузить ранее полученную транскрипцию.
При воспроизведении произносимое слово подсвечивается жёлтым. Текст автоматически прокручивается.
Внимание: AssemblyAI заблокирован на территории РФ — требуется VPN.

4. Перевод
Выделите слово или фразу в тексте — она будет автоматически переведена. Результат появляется в нижней панели.
По умолчанию используется Google Translate (бесплатно). При наличии ключа Яндекс можно переключиться на Yandex.Translate в Настройках.
Нажмите ""В Anki"" чтобы создать карточки из выделенного текста.

5. Интервал тишины
Меню 'Интервал тишины' позволяет выбрать длину аудиофрагмента: 100, 200, 300, 500, 800 мс или Свой... (50–5000 мс). Чем меньше значение — тем короче сегменты.

6. Экспорт в Anki
Нажмите ""В Anki"" после перевода слова/фразы, чтобы открыть окно создания карточки.
• Выберите или создайте колоду
• Проверьте английское слово, транскрипцию, русский перевод
• Контекст — предложение из книги (можно редактировать)
• Предложение — извлечь аудио целого предложения из книги (▶ Играть)
• TTS — скачать точное произнесение слова через Deepgram/Google (▶ TTS)
• Картинка — поиск изображений в Яндексе (Поиск → клик по картинке → ✓ Взять)
• Запись — записать свой голос с микрофона (🎤 Зап.)
Нажмите ""Создать карточки"" — создаются две карточки: en→ru и ru→en.
Каждая карточка содержит две кнопки аудио: 🔊 Word (TTS) и 📖 Sentence (полное предложение из книги).

7. TTS и аудио предложений
TTS (озвучка) скачивает произнесение слова/фразы через Deepgram Aura или Google Translate (запасной).
Аудио предложения извлекается из оригинальной аудиокниги — полное предложение, содержащее выбранное слово.
Оба аудиофайла встраиваются в карточки Anki как отдельные кнопки воспроизведения.

8. Поиск картинок
В окне карточки встроен браузер WebView2, который ищет картинки в Яндекс.Картинках по выбранному слову.
Нажмите на любую картинку → ✓ Взять чтобы сохранить её в карточку.

9. Настройки
Файл → Настройки → API ключи... позволяет:
• Выбрать активных провайдеров транскрипции (Deepgram, AssemblyAI)
• Ввести API-ключи для Deepgram и AssemblyAI
• Выбрать провайдера перевода (Google Translate или Yandex.Translate)
• Ввести API-ключ и Folder ID для Yandex.Translate
• Настроить размер чанка для API транскрипции (минут)
• Настроить задержку подсветки слов

10. Тема и язык
Меню Тема → Светлая / Тёмная — переключение темы оформления.
Меню Язык → English / Русский — переключение языка интерфейса. Выбор сохраняется.

11. Горячие клавиши
Ctrl+O — Загрузить аудиофайл
Space — Играть/Пауза
Ctrl+Space — Играть и дальше
M — Повторить сегмент
← → — Предыдущий/Следующий сегмент
Home/End — Первый/Последний сегмент
Esc — Остановить
Ctrl+T — Запросить транскрипцию через API
Ctrl+L — Загрузить транскрипцию из кэша
Ctrl+F — Перевести выделенный текст";
}
