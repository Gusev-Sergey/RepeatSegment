using System.Text.Json;

string projectDir = args.Length > 0 ? args[0] : @"c:\ProjectsCSharp\RepeatSegment\RepeatSegment.App";
string sourcePath = Path.Combine(projectDir, "Strings.cs");
string langDir = Path.Combine(projectDir, "lang");
Directory.CreateDirectory(langDir);

// Read entire Strings.cs
string code = File.ReadAllText(sourcePath);

// Extract EN dictionary
var en = ExtractDict(code, "En");
var ru = ExtractDict(code, "Ru");

// Write JSON files
WriteJson(Path.Combine(langDir, "en.json"), en);
WriteJson(Path.Combine(langDir, "ru.json"), ru);

// Create minimal DE, FR, ES files with just key UI strings + guide/intro (English fallback for rest)
CreatePartialLang(Path.Combine(langDir, "de.json"), en, "de");
CreatePartialLang(Path.Combine(langDir, "fr.json"), en, "fr");
CreatePartialLang(Path.Combine(langDir, "es.json"), en, "es");

Console.WriteLine($"en.json: {en.Count} keys");
Console.WriteLine($"ru.json: {ru.Count} keys");
Console.WriteLine("de.json, fr.json, es.json created with UI strings");
return 0;

static Dictionary<string,string> ExtractDict(string code, string dictName)
{
    var d = new Dictionary<string, string>();
    int start = code.IndexOf($"Dictionary<string, string> {dictName} = new()");
    if (start < 0) { Console.Error.WriteLine($"{dictName} not found!"); return d; }
    int bodyStart = code.IndexOf('{', start);
    int depth = 1, i = bodyStart + 1;
    while (depth > 0 && i < code.Length)
    {
        if (code[i] == '{') depth++;
        else if (code[i] == '}') depth--;
        i++;
    }
    string body = code.Substring(bodyStart + 1, i - bodyStart - 2);
    
    var matches = System.Text.RegularExpressions.Regex.Matches(body, @"\[""([^""]+)""\]\s*=\s*""((?:[^""\\]|\\.)*)"",?");
    foreach (System.Text.RegularExpressions.Match m in matches)
    {
        string key = m.Groups[1].Value;
        string val = m.Groups[2].Value.Replace("\\\"", "\"").Replace("\\n", "\n");
        if (string.IsNullOrEmpty(val)) continue;
        d[key] = val;
    }
    return d;
}

static void WriteJson(string path, Dictionary<string,string> d)
{
    var sorted = new SortedDictionary<string,string>(d);
    string json = JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    File.WriteAllText(path, json);
}

static void CreatePartialLang(string path, Dictionary<string,string> en, string code)
{
    // UI-only keys that need translation — use En as template, user will translate
    var ui = new SortedDictionary<string,string>(en);
    string json = JsonSerializer.Serialize(ui, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    File.WriteAllText(path, json);
    Console.WriteLine($"  {code}.json: {ui.Count} keys (English template)");
}
