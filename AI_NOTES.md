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
