using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RepeatSegment.App;

/// <summary>
/// Localized strings for UI. Language set via Strings.SetLanguage("en"/"ru"/"de"/"fr"/"es").
/// Strings are loaded from lang/{code}.json files. Falls back to en.json if file missing.
/// </summary>
public static class Strings
{
    public static string CurrentLang { get; private set; } = "en";
    public static string[] SupportedLanguages => new[] { "en", "ru", "de", "fr", "es" };

    private static Dictionary<string, string> _current = new();
    private static Dictionary<string, string> _enFallback = new();

    private static string LangDir
    {
        get
        {
            // Try relative to exe first, then relative to current directory
            string d = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang");
            if (Directory.Exists(d)) return d;
            d = Path.Combine(Directory.GetCurrentDirectory(), "lang");
            if (Directory.Exists(d)) return d;
            // Fallback: project source during dev
            d = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "lang");
            if (Directory.Exists(d)) return d;
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang");
        }
    }

    public static void SetLanguage(string lang)
    {
        lang = lang.ToLowerInvariant();
        if (lang != "en" && lang != "ru" && lang != "de" && lang != "fr" && lang != "es")
            lang = "en";
        CurrentLang = lang;
        _current = LoadJson(lang);
        if (lang != "en")
            _enFallback = LoadJson("en");
        else
            _enFallback = _current;
    }

    /// <summary>Get localized string by key. Returns key itself if not found.</summary>
    public static string Get(string key)
    {
        if (_current.TryGetValue(key, out var val) && val != null)
            return val;
        if (CurrentLang != "en" && _enFallback.TryGetValue(key, out var en) && en != null)
            return en;
        return key;
    }

    /// <summary>Get localized string with string.Format arguments.</summary>
    public static string Get(string key, params object[] args)
        => string.Format(Get(key), args);

    // ── User Guide (large text, loaded on demand) ──

    public static string GetUserGuideIntro()
    {
        return CurrentLang == "ru"
            ? "Программа для изучения английского языка через аудиокниги. Разбивает аудио на речевые сегменты (по паузам), позволяет повторять каждый сегмент, отображает текст транскрипции с синхронной подсветкой произносимых слов."
            : "Program for learning English through audiobooks. Splits audio into speech segments (by pauses), allows repeating each segment, and displays transcription text with synchronized word highlighting.";
    }

    public static string GetGuideSection(int num) => (CurrentLang, num) switch
    {
        (_, 1) => CurrentLang == "ru" ? "1. Загрузка аудио" : "1. Loading Audio",
        (_, 2) => CurrentLang == "ru" ? "2. Управление воспроизведением" : "2. Playback Controls",
        (_, 3) => CurrentLang == "ru" ? "3. Транскрипция" : "3. Transcription",
        (_, 4) => CurrentLang == "ru" ? "4. Перевод" : "4. Translation",
        (_, 5) => CurrentLang == "ru" ? "5. Настройка сегментов" : "5. Segment Duration",
        (_, 6) => CurrentLang == "ru" ? "6. Экспорт в Anki" : "6. Anki Export",
        (_, 7) => CurrentLang == "ru" ? "7. TTS и аудио предложений" : "7. TTS & Sentence Audio",
        (_, 8) => CurrentLang == "ru" ? "8. Поиск картинок" : "8. Image Search",
        (_, 9) => CurrentLang == "ru" ? "9. Настройки" : "9. Settings",
        (_, 10) => CurrentLang == "ru" ? "10. Тема и язык" : "10. Theme & Language",
        (_, 11) => CurrentLang == "ru" ? "11. Горячие клавиши" : "11. Hotkeys",
        _ => ""
    };

    public static string GetGuideContent(int num)
    {
        bool ru = CurrentLang == "ru";
        return num switch
        {
            1 => ru
                ? "Файл → Загрузить (Ctrl+O) — выберите MP3 или WAV файл. Программа автоматически найдёт паузы между фразами и разобьёт аудио на сегменты."
                : "File → Load (Ctrl+O) — select an MP3 or WAV file. The program automatically finds pauses between phrases and splits the audio into segments.",
            2 => ru
                ? "Кнопки: Назад, Играть/Пауза, Вперёд, Повторить, Играть и дальше, К началу, К концу. Слайдер — позиция в сегменте. Громкость — справа. Кнопка «Скорость» (1×) слева от навигации — выбор от 0,4× до 1,5× с шагом 0,1. Тембр голоса сохраняется (как на YouTube)."
                : "Buttons: Back, Play/Pause, Forward, Repeat, Play and Go, First, Last. Slider — segment position. Volume — right side. Speed button (1×) left of navigation — choose 0.4×–1.5× in 0.1 steps. Voice pitch preserved (like YouTube).",
            3 => ru
                ? "Перед первым использованием откройте Настройки → API ключи и введите API-ключ хотя бы одного провайдера (Deepgram или AssemblyAI).\nЗапросить через API (Ctrl+T) — отправить аудио на расшифровку. Результат кэшируется.\nЗагрузить из кэша (Ctrl+L) — загрузить ранее полученную транскрипцию.\nВо время воспроизведения произносимое слово подсвечивается жёлтым. Текст автоматически прокручивается.\nВнимание: AssemblyAI заблокирован на территории РФ — требуется VPN."
                : "Before first use, open Settings → API keys and enter the API key of at least one provider (Deepgram or AssemblyAI).\nRequest from API (Ctrl+T) — send audio for transcription. The result is cached.\nLoad from cache (Ctrl+L) — load previously obtained transcription.\nDuring playback, the spoken word is highlighted in yellow. Text auto-scrolls.\nNote: AssemblyAI is blocked in Russia — VPN required.",
            4 => ru
                ? "Выделите слово или фразу в тексте — она будет автоматически переведена. Результат появляется в нижней панели.\nПо умолчанию используется Google Translate (бесплатно). При наличии ключа Яндекс можно переключиться на Yandex.Translate в Настройках.\nНажмите «В Anki» чтобы создать карточки из выделенного текста."
                : "Select a word or phrase in the text — it will be automatically translated. The result appears in the bottom panel.\nBy default, Google Translate is used (free). If you have a Yandex API key, you can switch to Yandex.Translate in Settings.\nClick «Add to Anki» to create flashcards from the selected text.",
            5 => ru
               ? "Меню 'Длительность сегмента' (2, 5, 10, 20 с или Своя... от 1 до 300 c). Аудио нарезается на отрезки выбранной длины. Границы сдвигаются к ближайшей тишине (паузе). Так сегменты не обрывают слова на полуслове. Чем больше длительность — тем крупнее сегменты."
               : "The 'Segment duration' menu (2, 5, 10, 20 sec or Custom... from 1 to 300 sec). Audio is cut into segments of the chosen duration. Boundaries snap to the nearest silence (pause). This way segments don't cut words mid-speech. Longer duration = larger segments.",
            6 => ru
                ? "Нажмите «В Anki» после перевода слова/фразы, чтобы открыть окно создания карточки.\n• Выберите или создайте колоду\n• Проверьте английское слово, транскрипцию, русский перевод\n• Контекст — предложение из книги (можно редактировать)\n• Sentence — извлечь аудио целого предложения из книги\n• TTS — скачать точное произнесение слова через Deepgram/Google\n• Картинка — поиск изображений\n• Запись — записать свой голос с микрофона\nНажмите «Создать карточки» — создаются две карточки: en→ru и ru→en. Каждая содержит две кнопки аудио: 🔊 Word (TTS) и 📖 Sentence."
                : "Click «Add to Anki» after translating a word/phrase to open the card creation window.\n• Select or create a deck\n• Check the English word, transcription, Russian translation\n• Context — the sentence from the book (editable)\n• Sentence — extract the full sentence audio from the book\n• TTS — download precise word pronunciation from Deepgram/Google\n• Picture — search for images\n• Record — record your own voice via microphone\nClick «Create Cards» — two cards are generated: en→ru and ru→en. Each contains two audio buttons: 🔊 Word (TTS) and 📖 Sentence.",
            7 => ru
                ? "TTS (озвучка) скачивает произнесение слова/фразы через Deepgram Aura или Google Translate (запасной). Аудио предложения извлекается из оригинальной аудиокниги — полное предложение, содержащее выбранное слово. Оба аудиофайла встраиваются в карточки Anki как отдельные кнопки воспроизведения."
                : "TTS (Text-to-Speech) downloads pronunciation of a word/phrase via Deepgram Aura or Google Translate (backup). Sentence audio is extracted from the original audiobook — the full sentence containing the selected word. Both audio files are embedded into Anki cards as separate play buttons.",
            8 => ru
                ? "В окне карточки встроен браузер WebView2, который ищет картинки в Google или Яндексе по выбранному слову. Нажмите на любую картинку → ✓ Взять чтобы сохранить её в карточку."
                : "The card window has a built-in WebView2 browser that searches Google or Yandex Images by the selected word. Click on any image → ✓ Use to save it to the card.",
            9 => ru
                ? "Настройки разделены на три окна:\n• API ключи — провайдеры и API-ключи (Deepgram, AssemblyAI)\n• Перевод — выбор сервиса перевода (Google или Yandex) с API-ключом\n• Основные — язык интерфейса, язык транскрипции, провайдер картинок, битрейт MP3, минут в чанке, задержка подсветки"
                : "Settings are split into three windows:\n• API Keys — providers and API keys (Deepgram, AssemblyAI)\n• Translation — translation service (Google or Yandex) and API key\n• General — UI language, transcription language, image provider, MP3 bitrate, chunk minutes, highlight latency",
            10 => ru
                ? "Меню Тема → Светлая / Тёмная — переключение темы оформления. Язык интерфейса выбирается в Настройки → Основные. Выбор сохраняется."
                : "Menu Theme → Light / Dark — switch between themes. UI language is set in Settings → General. The choice is saved.",
            11 => ru
                ? "Ctrl+O — Загрузить аудиофайл\nSpace — Играть/Пауза\nCtrl+Space — Играть и дальше\nM — Повторить сегмент\n← → — Предыдущий/Следующий сегмент\nHome/End — Первый/Последний сегмент\nEsc — Остановить\nCtrl+T — Запросить транскрипцию через API\nCtrl+L — Загрузить транскрипцию из кэша\nCtrl+F — Перевести выделенный текст\n▲ — Скрыть/показать транскрипцию"
                : "Ctrl+O — Load audio file\nSpace — Play/Pause\nCtrl+Space — Play and Go\nM — Repeat segment\n← → — Previous/Next segment\nHome/End — First/Last segment\nEsc — Stop\nCtrl+T — Request transcription from API\nCtrl+L — Load transcription from cache\nCtrl+F — Translate selected text\n▲ — Collapse/expand transcription",
            _ => ""
        };
    }

    // ── JSON file loading ──

    private static Dictionary<string, string> LoadJson(string lang)
    {
        try
        {
            string path = Path.Combine(LangDir, $"{lang}.json");
            if (!File.Exists(path)) return new Dictionary<string, string>();
            string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch { return new Dictionary<string, string>(); }
    }
}
