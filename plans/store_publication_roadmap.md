# Roadmap: Microsoft Store Publication

## Что уже есть
- Приложение собирается `dotnet publish`, есть папка `Publish/`
- WiX инсталлятор в `Setup/` (но Store требует MSIX, а не WiX)
- Иконка `app.ico` есть (но нужны дополнительные размеры)

## Что необходимо сделать

### Этап 1: Техническая подготовка приложения (безопасность, стабильность)

| Задача | Описание | Статус |
|--------|----------|--------|
| 1. Privacy Policy | Добавить URL политики конфиденциальности (можно GitHub Pages) | ❌ |
| 2. App capabilities | Проверить, что приложение не запрашивает лишних разрешений (интернет — да, микрофон — да) | ❌ |
| 3. Сборка Release x64 | `dotnet publish -c Release --self-contained -r win-x64` | ❌ |
| 4. Версионирование | Сейчас `1.1.0.0` в WiX, в csproj нет версии | ❌ |
| 5. Иконки | Нужны: 44×44, 50×50, 150×150, 310×150 (StoreTile), 310×310 | ❌ |

### Этап 2: MSIX-пакет (замена WiX)

| Задача | Описание | Статус |
|--------|----------|--------|
| 6. Package Identity | Зарегистрироваться в Partner Center, получить Package/Store ID | ❌ |
| 7. Package.appxmanifest | Создать манифест с identity, capabilities, visual assets | ❌ |
| 8. Самоподписанный сертификат | Для локального тестирования MSIX | ❌ |
| 9. MSIX сборка | Через `dotnet publish` + Windows Application Packaging Project или MSIX Packaging Tool | ❌ |
| 10. Тестирование установки | Убедиться, что MSIX устанавливается и приложение работает | ❌ |

### Этап 3: Store Listing

| Задача | Описание | Статус |
|--------|----------|--------|
| 11. Partner Center | Создать аккаунт разработчика (разово $19 для private) | ❌ |
| 12. Store listing | Название, описание (EN/RU), скриншоты, иконки | ❌ |
| 13. Age rating | Пройти опросник IARC | ❌ |
| 14. Submission | Загрузить MSIX, заполнить все поля, отправить на проверку | ❌ |

## Текущие проблемы проекта перед публикацией

1. **WiX не подходит для Store** — MSIX обязателен. WiX можно оставить для offline-установщика.
2. **Один exe-файл** — MSIX требует структуру папок с манифестом
3. **Нет Privacy Policy URL** — Microsoft требует действующую ссылку
4. **WebView2** — нужно убедиться, что WebView2 Runtime включён в зависимости или указан как prerequisite
5. **Internet (Client)** — нужен в capabilities для Deepgram/AssemblyAI/Yandex
6. **Microphone** — нужен в capabilities для записи в AnkiCardWindow
