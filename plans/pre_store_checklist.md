# План доработок перед публикацией в Microsoft Store

## 1. App Capabilities (права приложения)

### Где указывать
В файле `Package.appxmanifest` (будет создан при переходе на MSIX). Секция `<Capabilities>`:

```xml
<Capabilities>
  <Capability Name="internetClient" />
  <capability Name="microphone" />
</Capabilities>
```

### Какие права нужны

| Право | Зачем | Обязательно? |
|-------|-------|-------------|
| `internetClient` | Deepgram, AssemblyAI, Google/Yandex Translate, TTS, поиск картинок | Да |
| `microphone` | Запись аудио в AnkiCardWindow (🎤 Rec) | Да |

**Не нужны**: `location`, `contacts`, `camera`, `fileSystem` (приложение работает через стандартные диалоги). Это хороший privacy-friendly профиль.

### Privacy Policy
Microsoft **требует** действующий URL политики конфиденциальности. Нужно создать страницу (например, GitHub Pages) с описанием:
- Какие данные собираются: **никакие персональные**
- Что уходит в интернет: аудио-фрагменты (транскрипция), текст (перевод/TTS)
- Как удалить: удалить папку `%APPDATA%/RepeatSegment`
- Контакты для связи

---

## 2. Лицензия и распространение

### Модель
- **Бесплатное**, closed-source (код не публичный)
- Лицензия: можно указать `Freeware` или `Proprietary`
- В Store: выбрать "Free" как ценовую модель
- В `Package.appxmanifest`: не требуется указывать лицензию для бесплатных приложений

### Что добавить в проект
- Файл `LICENSE.txt` в корне: "All Rights Reserved. Free for non-commercial use."
- В About окне: дополнить информацией о лицензии

---

## 3. Интернационализация (i18n)

### Текущее состояние
Всё хранится в [`Strings.cs`](RepeatSegment.App/Strings.cs) как два статических `Dictionary<string,string>` (EN + RU). Это неудобно для добавления языков.

### Предлагаемая архитектура: RESX-файлы

Стандарт .NET для локализации — `.resx` файлы. Visual Studio и dotnet CLI умеют с ними работать:

```
RepeatSegment.App/
├── Resources/
│   ├── Strings.resx          ← English (default)
│   ├── Strings.ru.resx       ← Russian
│   ├── Strings.de.resx       ← German
│   ├── Strings.es.resx       ← Spanish
│   ├── Strings.fr.resx       ← French
│   ├── Strings.zh-Hans.resx  ← Chinese Simplified
│   └── Strings.ja.resx       ← Japanese
```

**Преимущества RESX:**
- Стандартный механизм .NET (`System.Resources.ResourceManager`)
- Автоматическая загрузка нужного языка по `Thread.CurrentThread.CurrentUICulture`
- Не нужно менять код при добавлении языка — только добавить файл
- Инструменты перевода (ResX Resource Manager, Poedit) понимают формат
- Store ожидает RESX-локализацию

### Загрузка по требованию
`ResourceManager` сам загружает нужную сборку/файл по `CultureInfo`. Никакого кода переключения — просто:
```csharp
var rm = new ResourceManager("RepeatSegment.App.Resources.Strings", typeof(App).Assembly);
string text = rm.GetString("mw.menu.file"); // автоматически на языке пользователя
```

### Какие языки добавить

**Минимальный набор для Store (6 языков):**

| Язык | Culture | Рынок | Приоритет |
|------|---------|-------|-----------|
| Английский (default) | en | Весь мир | 🔴 |
| Русский | ru | РФ, СНГ | 🔴 |
| Немецкий | de | Германия, Австрия, Швейцария | 🟡 |
| Испанский | es | Испания, Латинская Америка, США | 🟡 |
| Французский | fr | Франция, Канада, Бельгия | 🟢 |
| Китайский (упр.) | zh-Hans | Китай, Сингапур | 🟢 |

**Опционально (ещё 2):**
| Японский | ja | Япония | 🔵 |
| Португальский | pt | Бразилия, Португалия | 🔵 |

**Обоснование**: это 6 крупнейших рынков Windows + те, кто активно учит английский. Суммарно покрывают ~80% аудитории Store.

### План миграции с Dictionary на RESX
1. Извлечь все ключи из `Strings.cs` → CSV
2. Создать `Strings.resx` (EN) и `Strings.ru.resx` (RU) с существующими переводами
3. Добавить пустые RESX для de, es, fr, zh-Hans
4. Заменить `Strings.Get(key)` на `Resources.Strings.ResourceManager.GetString(key)`
5. Язык интерфейса по умолчанию — English (fallback в RESX)
6. Меню Language переключает `Thread.CurrentThread.CurrentUICulture`

---

## 4. Что ещё нужно улучшить

### 4.1. Версионирование в csproj
Сейчас версия только в WiX. Нужно добавить в [`RepeatSegment.App.csproj`](RepeatSegment.App/RepeatSegment.App.csproj):
```xml
<PropertyGroup>
  <Version>1.1.0</Version>
  <Company>RepeatSegment</Company>
  <Copyright>© 2026 RepeatSegment</Copyright>
  <Description>Study English through audiobooks — transcription, translation, Anki flashcards</Description>
</PropertyGroup>
```

### 4.2. Self-contained публикация
Сейчас `dotnet publish` даёт framework-dependent сборку. Для Store нужен self-contained:
```bash
dotnet publish -c Release --self-contained -r win-x64 -p:PublishSingleFile=true
```

### 4.3. WebView2 Runtime
Приложение использует WebView2 (поиск картинок). Нужно:
- Указать в описании Store: "Requires WebView2 Runtime (pre-installed on Windows 11)"
- Или включить Evergreen Bootstrapper в установщик

### 4.4. Иконки для Store
Требуются PNG разных размеров (см. [`store_publication_roadmap.md`](plans/store_publication_roadmap.md)):
- 44×44, 50×50, 150×150, 310×150 (wide), 310×310
- Плюс StoreLogo и Badge

### 4.5. Privacy Policy URL
Создать GitHub Pages: `https://gusev-sergey.github.io/RepeatSegment/privacy.html`

### 4.6. About Window
Добавить: версию из assembly, ссылку на Privacy Policy, лицензию.

### 4.7. First-run Experience
При первом запуске показывать приветственный экран с кратким руководством (сейчас `FirstRunWindow` не используется).

### 4.8. Обработка отсутствия интернета
Сейчас API-вызовы падают с исключением. Нужно показывать понятное сообщение: "No internet connection. Transcription requires internet."

### 4.9. Адаптация под высокий DPI
Проверить `app.manifest` на наличие `<dpiAware>true</dpiAware>`.

### 4.10. Подпись сборки
Для MSIX нужен сертификат. Для тестирования — самоподписанный. Для Store — Microsoft подписывает автоматически.
