# Повторение пройденного для продолжения портирования SEAB на C# (RepeatSegment)

## Общая идея
SEAB (Study English with Audio Books) – это Python-приложение для изучения иностранных языков через повторение аудиофрагментов.
Оно загружает аудио, разбивает на фрагменты по паузам, позволяет настраивать повтор каждого фрагмента (начало/конец/скорость/громкость),
а также переводит в текст (на выбор локальный whisper или облачный AssemblyAI или локальный VOSK, а также Yandex SpeechKit, VK, SaluteSpeech, Deepgram).

Мы портируем приложение на C# (WPF, .NET) в проект RepeatSegment.

Исходный проект на Python находится в c:\PycharmProjects\SEAB
Портированный – c:\ProjectsCSharp\RepeatSegment

## Что уже сделано в портированном C# проекте
- Аудио-движок (AudioEngine) с изменением скорости (SoundTouch), громкости и позиционированием (NAudio).
- Детектор тишины (SilenceDetector) – все параметры, включая настройку через конфиг.
- Транскрипция (локальный whisper, AssemblyAI, VOSK).
- Менеджер настроек (ConfigManager) – иерархия INI с приведением типов, сохранение/загрузка значений.
- GUI (WPF) набросанный в первом приближении – есть окно, кнопки, меню, виджет громкости.

## Нерешённые задачи (список проблем)
1. Перенос UI:
   - Кнопки должны быть картинками (иконками), как в оригинале. Иконки лежат в c:\PycharmProjects\SEAB\sources.
   - У приложения должна быть иконка окна.
   - Layout controls (Play/Pause/Stop, таймкоды, слайдеры скорости/громкости, перемотка) должны быть расположены и работать как в оригинале.
   - Слайдер позиции должен быть осмысленным (сейчас выглядит как placeholder).
2. При выборе API (меню выбора транскрипции): выпадающий список имеет тёмный фон, текст не виден.
3. Контраст шрифта с фоном во всех меню: тема оформления не соответствует оригиналу, шрифты плохо читаются.
4. Вертикальные линии-разделители сегментов аудио на слайдере позиции отсутствуют.
5. Проигрывание не останавливается при нажатии Stop.
6. Управление не соответствует логике изначального проекта SEAB (Python).

## Приоритеты (что делать сейчас)
1. Изучить оригинальный Python UI (ui/main_window.py).
2. Изучить текущий C# UI (MainWindow.xaml, MainWindow.xaml.cs, SettingsWindow.xaml, SettingsWindow.xaml.cs).
3. Изучить текущий C# audio engine (AudioEngine.cs), особенно логику Stop.
4. Поправить C# UI:
   - Иконки для кнопок из папки sources (Python проект).
   - Иконка приложения.
   - Исправить тему оформления (контраст, фон).
   - Исправить выпадающие списки (фон/текст).
   - Вертикальные линии сегментов на слайдере позиции.
5. Исправить C# логику управления воспроизведением (Stop, Play/Pause, петля).
6. Протестировать и убедиться что всё работает как в оригинальном Python SEAB.

## Структура исходного SEAB (Python)
```
c:\PycharmProjects\SEAB\
  main.py                     – точка входа
  run_seab.bat                – bat для запуска
  requirements.txt            – зависимости
  config.ini                  – базовый конфиг
  player/
    audio_engine.py           – AudioEngine (AudioSegment, pydub)
    silence_detector.py       – SilenceDetector
    transcription.py          – TranscriptionProvider (Whisper, AssemblyAI, VOSK, Yandex, VK, Salute, Deepgram)
  settings/
    config_manager.py         – ConfigManager
  tests/
    test_config_manager.py    – тесты для ConfigManager
  ui/
    main_window.py            – главное окно (PySide6/PyQt6)
    metrics.py                – метрики для UI
  output/                     – результаты транскрипции
  sources/                    – иконки и изображения
    play.png, stop_play.png, first.png, last.png, repeat.png, repeat_pressed.png,
    next_play.png, pre_play.png, next_play_flash.png, pre_play_flash.png,
    play_go.png, btn_play.png, free-icon-audiobook-3145783.png,
    audiobook-7688709.png
```

## Структура портированного RepeatSegment (C#)
```
c:\ProjectsCSharp\RepeatSegment\
  RepeatSegment.App/
    AudioEngine.cs            – AudioEngine (NAudio + SoundTouch)
    SilenceDetector.cs        – SilenceDetector
    TranscriptionProvider.cs  – TranscriptionProvider (Whisper, AssemblyAI, VOSK)
    ConfigManager.cs          – ConfigManager
    App.xaml / App.xaml.cs    – приложение WPF
    MainWindow.xaml / .cs     – главное окно
    SettingsWindow.xaml / .cs – окно настроек
    VolumeWidget.cs           – виджет громкости
    Icons/                    – иконки (из Python проекта sources/)
```

## Структура иконок (в Python проекте)
```
c:\PycharmProjects\SEAB\sources\
   play.png
   stop_play.png
   first.png / last.png
   repeat.png / repeat_pressed.png
   next_play.png / pre_play.png
   next_play_flash.png / pre_play_flash.png
   play_go.png / btn_play.png
   free-icon-audiobook-3145783.png / audiobook-7688709.png
```

## Текущее поведение C# AudioEngine.cs (важно для фикса Stop)
Stop должен останавливать воспроизведение и сбрасывать позицию на 0 (или на начало текущего сегмента).
Сейчас есть проблемы:
  - После Stop звук может продолжать играть
  - Нет корректной обработки состояния "остановлен"

## Дополнительные заметки
- В оригинальном Python приложении используется петля (loop) – возможность зацикливать фрагмент, она должна работать с индикацией на кнопке.
- Транскрипция должна вызываться из меню и работать асинхронно (не вешать UI).
- Слайдер позиции должен отображать разбивку на сегменты вертикальными линиями (как в оригинале).
- В Python проекте используются PySide6/PyQt6. В C# проекте используется WPF (.NET).
- Python-проект проверен: все импорты работают (audio_engine, silence_detector, transcription, config_manager).
- Python-проект не сломан, изменения в git отражают текущие доработки UI.
- Для восстановления Python проекта из git: `git checkout` нужных файлов.

## Команды для запуска
Python (оригинал):
```
cd c:\PycharmProjects\SEAB
python main.py
```

C# (порт):
```
cd c:\ProjectsCSharp\RepeatSegment
dotnet run --project RepeatSegment.App