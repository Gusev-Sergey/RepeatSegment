# Agent Rules for Zoo Code

## Language and Style
- Всегда отвечай на русском языке, если пользователь явно не попросил иначе.
- Перед изменением кода объясни свой план.
- Код пиши на английском: имена переменных, классов, методов — в стиле C# (PascalCase, camelCase, _camelCase для полей).
- Следуй Microsoft .NET Coding Conventions.

## Interactions
- Перед созданием/изменением файлов перечисляй, что будешь трогать.
- Предпочитай точечные правки полной перезаписи.
- Следуй существующему стилю кода в проекте.
- При неясности задавай уточняющие вопросы.

## Safety
- Не выводи секреты (ключи, пароли).
- Не удаляй и не перезаписывай файлы без подтверждения.
- Не используй `sudo` без спроса.
- Не ставь глобальные .NET-пакеты без явной просьбы; добавляй через `dotnet add`.
- **Не анализируй изображения** — проси текстовое описание.
- **Не открывай сайты в браузере**; для получения данных используй `curl` (с подтверждением, если URL не локальный).

## Think Before Code
- Проговаривай допущения.
- Показывай альтернативы, если запрос размыт.
- При сомнениях останавливайся и спрашивай.

## Keep It Simple
- Минимум кода, только то, что попросили.
- Не создавай абстракции «на будущее».
- Не обрабатывай невозможные сценарии.
- Если 200 строк можно сократить до 50 — перепиши.

## Surgical Edits
- Трогай только то, что нужно.
- Не рефакторь несвязанный код.
- Следуй стилю, даже если он не идеален.
- Мёртвый код (не твой) только упоминай, но не удаляй без разрешения.
- Неиспользуемые `using`-директивы и переменные, оставшиеся после твоих правок, можно чистить.

## Goal-Driven Execution
- Вместо «добавь валидацию» → напиши тесты, потом заставь их пройти.
- Вместо «почини баг» → напиши тест, воспроизводящий баг, потом почини.
- Вместо «отрефактори X» → убедись, что тесты проходят до и после.

## C# / .NET Specifics
- Используй Entity Framework Core или Dapper (как принято в проекте).
- `async`/`await` с обработкой исключений.
- Внедряй зависимости через конструктор.
- Тесты — xUnit или NUnit (следуй проекту).
- Команды: `dotnet build`, `dotnet test`, `dotnet run`.

## Distribution Build (WiX Installer for non-Store use)
- **Не использовать `--self-contained`** для переноса на другие ПК — добавляет .NET Runtime (+800 МБ).
- **Добавить `<SelfContained>false</SelfContained>` в `.csproj`** — без этого `dotnet publish -r win-x64` всегда self-contained.
- Публикация: `dotnet publish -c Release -r win-x64 -o Publish/Release`.
- WiX `Product.wxs`: перечислять файлы явно через `<Component><File Source="..."/></Component>`. Wildcard `<Files Include="**\*">` нестабилен.
- `config.template.ini` должен быть в установщике (без API ключей). API ключи пользователь вводит сам.
- `lang/` JSON брать из исходной папки (не из `Publish/Release/lang/` — там их нет при framework-dependent).
- Очищать `Setup/bin/` и `Setup/obj/` перед пересборкой при изменении состава файлов.
- Размер `.msi`: ~6.5 МБ (framework-dependent) vs ~876 МБ (self-contained).

## Adaptive Sizing (Screen-relative dimensions)
- **Все размеры окон и элементов должны быть относительными** и привязаны к `SystemParameters.WorkArea.Width/Height`.
- Жёсткие пиксельные значения (например, `+100`, `MinHeight=500`, `MaxHeight=160`) недопустимы.
- Используй формулу: `Math.Max(минимальный_порог, WorkArea.Height * доля)`.
- **Доли экрана**:
  - MainWindow: ширина 0.85, высота 0.48, MinWidth 0.45
  - GrowWindowForTranslation: прирост `Math.Max(150, WorkArea.Height * 0.22)` от базы
  - AnkiCardWindow: ширина 0.80, высота 0.85
  - TranslationPanel MaxHeight: `200` (XAML), программно ограничивается GrowWindowForTranslation
  - Кнопки: `Math.Max(48, WorkArea.Width * 0.06)`, иконки: `bs * 0.85`
  - WaveformGraph: `Math.Max(60, WorkArea.Height * 0.10)`
  - VolumeWidget: `Math.Max(140, Math.Min(WorkArea.Width * 0.28, 650))`

## I18n: External Language Files
- Все строки в `lang/{code}.json`. Формат: `{"key": "value"}` (JSON).
- `Strings.cs` загружает через `JsonSerializer.Deserialize`. Fallback → `en.json`.
- Смена языка: `RestartApp()` (новый процесс + выход).
- При добавлении ключа — обновить все 5 JSON-файлов.
- User Guide жёстко в коде (GetGuideContent) — слишком большой для JSON.

## Child Windows: Universal Rules
- **Размеры**: `Math.Min(WorkArea.Width * доля, максимум)` в конструкторе.
- **XAML**: `SizeToContent="Manual"` + `Width`/`Height` дефолтные (для дизайнера).
- **TextWrapping="Wrap"** на всех TextBlock (глобальный стиль).
- **MaxWidth** на длинных описаниях (предотвращает растягивание).
- **ScrollViewer** для содержимого, которое может не влезть.
- **XAML Theme**: кнопки с кастомным `TextButtonStyle` (не системный голубой hover).
- **Тёмный title bar**: `DwmSetWindowAttribute` в `Window_Loaded`.

## Suggested Skills
- `wix-installer` — сборка WiX .msi, управление компонентами, Burn bootstrapper
- `wpf-i18n` — работа с JSON-файлами локализации, переключение языков
- `wpf-adaptive-layout` — адаптивные размеры окон и элементов
- `dotnet-publish` — профили публикации, self-contained vs framework-dependent, trimming для WPF
