# Skill: WPF i18n (Internationalization)

## Purpose
Add multi-language support to WPF applications using external JSON files.

## Key Rules

### 1. File Structure
```
lang/
  en.json   (base language)
  ru.json   (Russian)
  de.json   (German)
  fr.json   (French)
  es.json   (Spanish)
```

### 2. JSON Format
```json
{
  "mw.menu.file": "File",
  "mw.menu.file.load": "Load",
  "acw.title": "Anki Card"
}
```
- Flat key-value, no nesting.
- Keys use dot notation: `window.section.element`.
- All UI text goes through `Strings.Get(key)`.

### 3. Strings.cs Loader
```csharp
public static class Strings
{
    public static string CurrentLang { get; private set; } = "en";
    private static Dictionary<string, string> _current = new();
    private static Dictionary<string, string> _enFallback = new();

    public static void SetLanguage(string lang)
    {
        CurrentLang = lang;
        _current = LoadJson(lang);
        if (lang != "en") _enFallback = LoadJson("en");
        else _enFallback = _current;
    }

    public static string Get(string key)
    {
        if (_current.TryGetValue(key, out var val)) return val;
        if (CurrentLang != "en" && _enFallback.TryGetValue(key, out var en)) return en;
        return key; // fallback: show key itself
    }

    private static Dictionary<string, string> LoadJson(string lang)
    {
        string path = Path.Combine(LangDir, $"{lang}.json");
        string json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<Dictionary<string,string>>(json) ?? new();
    }
}
```

### 4. LangDir Resolution
```csharp
private static string LangDir
{
    get
    {
        // Try relative to exe first
        string d = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang");
        if (Directory.Exists(d)) return d;
        // Then current directory
        d = Path.Combine(Directory.GetCurrentDirectory(), "lang");
        if (Directory.Exists(d)) return d;
        // Fallback: project source during dev
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "lang");
    }
}
```

### 5. csproj Copy
```xml
<Content Include="lang\**">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  <Link>lang\%(RecursiveDir)%(Filename)%(Extension)</Link>
</Content>
```

### 6. Language Switch → Restart
```csharp
if (langChanged) RestartApp();

static void RestartApp()
{
    var exe = Environment.ProcessPath ?? "";
    if (!string.IsNullOrEmpty(exe) && File.Exists(exe))
    {
        Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = true });
        Environment.Exit(0);
    }
}
```

### 7. When Adding New Keys
- Add to all 5 JSON files.
- Use `Strings.Get("key")` in code-behind.
- Apply in constructor: `Title = Strings.Get("window.title")`.
- For XAML elements: set in `ApplyStrings()` method.

### 8. What NOT to put in JSON
- User Guide (large text) — keep in code (`GetGuideContent()`).
- Auto-generated strings (file names, paths).
- One-time setup strings that never change.
