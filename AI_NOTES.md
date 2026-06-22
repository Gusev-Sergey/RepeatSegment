# Anki .apkg Export — Technical Reference

## Структура .apkg файла
`.apkg` — это ZIP-архив содержащий:
- `collection.anki2` — SQLite база данных
- `media` — JSON-файл (без расширения!) маппинга числовых ключей на имена медиафайлов
- Медиафайлы с **числовыми именами без расширений**: `"0"`, `"1"`, `"2"` (не `"m0.jpg"`!)

### Формат media.json
```json
{"0":"m0.jpg","1":"m1.mp3"}
```
Ключ — числовое имя в ZIP, значение — описательное имя с расширением.

### Формат полей в notes.flds
Поля разделены символом `\x1f` (Unit Separator):
```
hello\x1f/həˈloʊ/\x1fпривет\x1f<img src="m0.jpg">\x1f[sound:m1.mp3]\x1fcontext
```
- **image**: `<img src="filename.ext">`
- **sound**: `[sound:filename.mp3]`

## Модель карточек (Schema 11)
Скопирована 1:1 с эталонной колоды July_2015:
- Поля: `word`, `transcription`, `translation`, `image`, `sound`, `context`
- Шаблоны используют `{{FrontSide}}` для сохранения лицевой стороны на обороте
- CSS: тёмная тема `background-color:#1e1e1e`
- `req`: `[[0,"any",[0,1,4]],[1,"any",[2,3,5]]]`

## Критические ошибки и их решения

### 1. Ошибка "500: A number was invalid or out of range"
**Причины (множественные):**
- `mid` (model ID) > int.MaxValue → использовать `1728000001L`
- `csum` (CRC32) как 64-битное число → `unchecked((int)Cr32(f))`
- `scm` > int.MaxValue → использовать секунды, не миллисекунды
- Чисто числовые имена медиафайлов (например `"0.jpg"`) в ZIP — Anki 26.05 требует БЕЗ расширений в ZIP
- **Файлы в ZIP должны быть без расширений** (`"0"`, `"1"`), расширения только в `media.json` значениях

### 2. Медиафайлы не отображаются / не проигрываются
- Файлы должны быть в корне ZIP, не в подпапке
- Имена в ZIP: `"0"`, `"1"` (чистые числа, без расширений)
- `media.json`: `{"0":"m0.jpg","1":"m1.mp3"}`
- Формат аудио: **MP3 обязателен** (WAV не поддерживается Anki 26.05)
- Картинки: `<img src="m0.jpg">` в поле image
- Звук: `[sound:m1.mp3]` в поле sound (тег `[sound:...]` обязателен для воспроизведения)

### 3. Двойное оборачивание `<img>`
Модель использует `{{image}}` — если в поле уже `<img src=...>`, НЕ надо добавлять ещё один `<img>` в шаблоне. Или шаблон содержит `<img src='{{picture}}'>` и в поле кладётся голое имя, ИЛИ шаблон содержит `{{image}}` и в поле `<img src="...">`. Не смешивать.

### 4. WAV не поддерживается
Anki 26.05 требует MP3. Использовать NAudio.Lame для конвертации:
```csharp
using var writer = new LameMP3FileWriter(outputStream, waveFormat, 128);
```

### 5. UTF-8 BOM в media.json
`File.WriteAllText` добавляет BOM по умолчанию. Anki не принимает BOM:
```csharp
File.WriteAllText(path, json, new UTF8Encoding(false));
```

### 6. SQLite schema
Обязательные таблицы: `col`, `notes`, `cards`, `graves`, `revlog`.
Использовать `DROP TABLE IF EXISTS` вместо `CREATE TABLE IF NOT EXISTS` + `DELETE FROM` для чистоты.

### 7. `[sound:...]` vs `{{sound}}`
Поле `sound` должно содержать `[sound:filename.mp3]`. Шаблон использует `{{sound}}` для вывода. Неправильно: `[sound:]` (без расширения) или `{{sound}}` с голым именем файла.

### 8. Блокировка файлов
- `ZipFile.CreateFromDirectory()` вместо `File.Create + ZipArchive + File.Copy`
- `SqliteConnection.ClearAllPools()` + `GC.Collect()` + `Thread.Sleep(200)` после закрытия БД
- `PRAGMA journal_mode=DELETE;` перед транзакцией

### 9. Слияние колод
При создании нового `AnkiExportManager("deckName")` читать существующий `.apkg`, извлекать старые заметки и добавлять новые поверх.

### 10. `CopyTo` для MP3FileWriter
`Flush()` не дописывает хвост MP3 — надо закрыть writer перед чтением MemoryStream:
```csharp
using (var writer = new LameMP3FileWriter(ms, fmt, 128)) { writer.Write(data, 0, len); }
byte[] result = ms.ToArray();
```
После `using` writer закрыт → MP3 полный.

---

# Технические находки и решения (июнь 2026)

## MP3 битрейт
- 128 kbps — избыточно для речи. 64 kbps mono — золотая середина для TTS.
- Anki 26.05 поддерживает только MP3 (не Opus).
- Добавлен выбор 64/128 kbps в Settings → General.

## Anki auto-play
- При двух аудио (sentence + TTS) Anki проигрывает все `[sound:...]` теги подряд.
- Решение: `"autoplay":false` в dconf. Осознанно отключено.

## dcid (deck configuration ID)
- Anki кеширует deck configuration на клиенте.
- Решение: `dcid = 900000000 + (timestamp % 100000000)` — всегда уникальный.
- Побочный эффект: накопление старых dconf-записей в JSON (не критично).

## Слияние колод и дублирование медиа
- `BuildDeck()` не делает content-based дедупликации.
- При повторном создании карточек с тем же аудио — новый ID, дубликат в ZIP.
- Не критично при полной замене колоды.

## Кодировка recent.txt
- `File.WriteAllLines` без `Encoding.UTF8` → ANSI (windows-1251) портит кириллицу.
- Решение: всегда `Encoding.UTF8` при чтении и записи.
- Добавлена автоочистка битых ANSI-файлов по наличию `\uFFFD`.

## Frozen Brush в ApplyTheme
- `SolidColorBrush` из XAML заморожен — менять `.Color` нельзя.
- Решение: `Resources[key] = new SolidColorBrush(value)` при каждой смене темы.
- Цвета вынесены в статические словари `DarkColors`/`LightColors`.

## RichTextBox и FlowDocument отступы
- `FlowDocument.PagePadding` по умолчанию `{5,5,5,5}`.
- `Paragraph.Margin` по умолчанию `{0,0,0,10}`.
- Решение: `PagePadding="0"`, `Paragraph Margin="0"`.

## Дочерние окна и тёмная тема
- Title bar красится через `DwmSetWindowAttribute` с `DWMWA_USE_IMMERSIVE_DARK_MODE=20`.
- Для дочерних окон нужен `Loaded` event с доступом к `MainWindow.IsDarkTheme`.
- Кисти копируются из MainWindow через `InjectBrushes()`.

## Размер окна и SizeToContent
- `SizeToContent` не может опуститься ниже `MinHeight`.
- `AdaptToScreen()` задаёт `Width`/`Height` — `SizeToContent` не переопределяет.
- Решение для кнопки ▲/▼: `Height = double.NaN; MinHeight = 0; SizeToContent = Height`.

## Скорость воспроизведения с сохранением тембра (SOLA/WSOLA)
- Линейная интерполяция меняет высоту тона (как перемотка магнитофона) — непригодно.
- Стандарт индустрии: Overlap-Add с окном и перекрёстным затуханием.
- **Окно Бартлетта (треугольное)** при 50% перекрытии даёт идеальный COLA (`w[n]+w[n+N/2]=1`).
- **WSOLA** (Waveform Similarity Overlap-Add): кросс-корреляция в зоне перекрытия выравнивает фазу.
- Без WSOLA на низких скоростях — гребёнчатая фильтрация (дребезг) из-за фазовых сдвигов.
- Корреляция Пирсона точнее сырой, но O(N²) — непригодна для длинных сегментов.
- **Итог**: frame=4096, hop=2048, Bartlett, WSOLA с сырой корреляцией на 2048 семплах, ±32.
- Позиция волны должна умножаться на `_playbackSpeed` для синхронизации с растянутым аудио.
- `_playStartTime` ставить ПОСЛЕ SOLA-обработки, иначе скачок позиции.
- Диапазон: 0.4× – 1.5×, шаг 0.1. Высокие скорости звучат отлично, низкие — приемлемо.

## Google Images — извлечение URL картинок через WebView2

### Проблема
Google Images использует сложный DOM (Shadow DOM, lazy-loading, `encrypted-tbn` URL) и боковую панель с AJAX-загрузкой полноразмерного изображения. Прямые HTTP-запросы к Google Images из HttpClient получают 429 (Too Many Requests) или таймаут.

### Решение (v074-v076)
1. **Извлечение URL**: обработчик клика в capture-фазе ищет `<a href="/imgres?imgurl=НАСТОЯЩИЙ_URL">` — извлекает `imgurl` параметр через `decodeURIComponent`. Альтернативно проверяет `data-ou` атрибут.
2. **Загрузка**: основной метод — JavaScript `fetch(url, {credentials:'include'})` внутри WebView2 (использует cookie-сессию браузера, не блокируется Google). Fallback — HttpClient с полными браузерными заголовками (User-Agent Chrome 125, Sec-Fetch-*).
3. **Анти-бот меры**: `navigator.webdriver=false`, `window.chrome={runtime:{}}`, CONSENT/NID cookies через `CoreWebView2.CookieManager`.
4. **Таймаут**: основной метод — не ограничен (WebView2), fallback — 10-12 секунд.
5. **Авто-декомпрессия**: `HttpClientHandler { AutomaticDecompression = All }`.

### Ключевые находки
- `document.elementsFromPoint()` — единственный API, пробивающий Shadow DOM Google (`.closest()` не работает)
- Google НЕ хранит полноразмерное изображение в `src` тега `<img>` — URL в `href` родительского `<a>` или в `data-ou`
- `encrypted-tbn` URL — зашифрованные миниатюры, HTTP-запрос к ним возвращает 429
- `MutationObserver` и `setInterval` ненадёжны — боковая панель Google грузится асинхронно с непредсказуемым timing'ом
- **Лучший подход**: клик проходит естественно → боковая панель открывается → "Use" сканирует DOM через `querySelectorAll('img[src^="http"]')` с фильтром `naturalWidth > 150`

## StatusBar и GrowWindowForTranslation — эпопея с высотой окна

### Проблема
При открытии панели перевода (`TranslationPanel`) StatusBar уходил за нижнюю границу окна. Множество подходов не работали из-за циклической зависимости: `*`-ряд (транскрипция) сжимается при появлении панели перевода, а `ActualHeight` не отражает желаемую высоту.

### Что НЕ сработало (v062-v078, 15+ итераций):
- `Height = _baseWindowHeight` — окно прыгало
- `TranslatePoint` относительно окна — неправильная система координат (включает заголовок)
- `TranslatePoint` относительно `LayoutRoot` — координаты зависят от текущего размера окна
- `Measure` с бесконечной высотой — `*`-ряд сообщал бесконечную желаемую высоту → окно вырастало до 92% экрана
- Фиксация `*`-ряда перед `Measure` — сложно и ненадёжно
- `SizeToContent = Height` — `*`-ряд растягивался на весь экран
- `GrowWindowForTranslation = ActualHeight + N` — кумулятивный рост при повторных выделениях

### Финальное решение (v081):
```csharp
private double _baseWindowH;
private void GrowWindowForTranslation() {
    if (_baseWindowH <= 0) _baseWindowH = ActualHeight;  // фиксируем базу ОДИН раз
    double maxH = SystemParameters.WorkArea.Height * 0.85;
    double newH = _baseWindowH + 100;  // всегда +100px от исходной высоты
    if (newH > maxH) newH = maxH;
    if (newH > ActualHeight) Height = newH;
}
```

### Ключевые находки:
- Нельзя использовать `ActualHeight` для расчёта прироста — он уже изменён предыдущим вызовом
- `_baseWindowH` запоминается один раз при ПЕРВОМ вызове, все последующие вызовы используют ту же базу
- Статусбар: заменён со `<StatusBar>` (сложный встроенный шаблон WPF) на простой `<Border>` + `<TextBlock>`
- Строка панели перевода `TxtTranslationProvider` показывает информацию о провайдере (вместо StatusBar)
- `MaxHeight=160` на панели перевода + `ScrollViewer` предотвращает чрезмерное расширение

## WPF StatusBar — внутренние отступы

### Проблема
`<StatusBar>` в WPF имеет встроенный сложный шаблон с `StatusBarItem`, который добавляет неубираемые внутренние `Padding`/`Margin`. Даже с `Margin="0" Padding="0"` остаётся ~6px мёртвого пространства.

### Решение (v073)
Заменить `<StatusBar>` на `<Border>` + `<TextBlock>`:
```xml
<Border x:Name="MainStatusBar" Background="..." MinHeight="20" Padding="4,2,4,2"
        BorderBrush="..." BorderThickness="0,1,0,0">
    <TextBlock x:Name="TxtStatus" FontSize="12" TextTrimming="CharacterEllipsis"
               VerticalAlignment="Center"/>
</Border>
```
- `BorderThickness="0,1,0,0"` — тонкая разделительная линия сверху
- `TextTrimming="CharacterEllipsis"` — длинный текст обрезается с многоточием
- `VerticalAlignment="Center"` — текст всегда по центру

## WiX Installer — сборка дистрибутива (не для Store)

### Проблема
`dotnet publish -r win-x64` по умолчанию включает `--self-contained` (870+ МБ). Для распространения вне Store нужен framework-dependent режим.

### Решение (v093)
1. **В csproj**: `<SelfContained>false</SelfContained>` — критично! Без этого флаг `-r win-x64` всегда включает self-contained, даже с `--self-contained false`.
2. **Публикация**: `dotnet publish -c Release -r win-x64 -o Publish/Release`
3. **WiX Product.wxs**: перечислять ВСЕ файлы явно (`<Component><File Source="..."/></Component>`). WiX v4 `Files Include` с wildcard нестабилен — может не подхватить или дать дубликаты.
4. **Языковые файлы**: брать из исходной папки `lang/` (не из `Publish/Release/lang/` — там их нет).
5. **config.template.ini**: копировать отдельным компонентом (не через wildcard).
6. **Размер**: ~6.5 МБ против 876 МБ с self-contained.

### Ключевые находки
- `-r win-x64` = self-contained по умолчанию, нужно `-p:SelfContained=false` или `<SelfContained>false</SelfContained>` в csproj
- WPF не поддерживает trimming — `PublishTrimmed=true` даёт ошибку
- WiX v4 `Files Include="**\*"` работает нестабильно — лучше явно перечислить файлы
- Очистка `bin/` и `obj/` перед пересборкой WiX предотвращает залипание старых `.msi`
- `#` в пути WiX (из `obj\#RepeatSegment.cab`) — норма, символ для временных файлов

## I18n: вынос строк в JSON-файлы

### Решение (v085-v087)
1. **Внешние файлы**: [`lang/en.json`](RepeatSegment.App/lang/en.json), `ru.json`, `de.json`, `fr.json`, `es.json`
2. **Загрузка**: `Strings.cs` → `JsonSerializer.Deserialize<Dictionary<string,string>>()`
3. **Fallback**: отсутствующие ключи берутся из `en.json`
4. **Автопоиск**: папка `lang/` ищется относительно exe, затем cwd, затем project source
5. **csproj**: `<Content Include="lang\**"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>`
6. **Смена языка**: через GeneralSettingsWindow → автоперезапуск приложения (`RestartApp()`)

### Ключевые находки
- User Guide (GetGuideContent) жёстко зашит в код — слишком большой для JSON
- При смене языка нужен перезапуск: `Process.Start(exe) + Environment.Exit(0)`
- `SortedDictionary` в JSON сохраняет порядок ключей для читаемости

## About Window — адаптация под все языки

### Решение (v088-v089)
1. **Текст**: одна длинная строка `mw.dlg.about` с `\n` в JSON
2. **XAML**: `ScrollViewer` + `TextWrapping="Wrap"` — текст переносится, не вылезает
3. **Размер**: `SizeToContent="WidthAndHeight"` → заменён на `Manual` + `MaxWidth` (иначе однострочный текст растягивал окно)
4. **Адаптивность**: `Width = Math.Min(sw * 0.38, 500)` в code-behind

## Theme Switch — перестроение параграфа транскрипции

### Проблема
При смене темы `ApplyTheme()` обновлял `Resources["TextBrush"]`, но `Run`-элементы уже ссылались на старую кисть.

### Решение (v090)
В `ApplyTheme()`: `if (_wordRuns != null) { BuildWordsParagraph(); _lastHlIdx = -1; }` — пересоздать параграф с новым цветом.
