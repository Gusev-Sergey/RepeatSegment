using System.Text.Json;
using System.Text.RegularExpressions;

string src = args.Length > 0 ? args[0] : @"c:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\Strings.cs";
string langDir = args.Length > 1 ? args[1] : @"c:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\lang";
Directory.CreateDirectory(langDir);

string code = File.ReadAllText(src);

var en = ExtractDict(code, "En");
var ru = ExtractDict(code, "Ru");
WriteJson(Path.Combine(langDir, "en.json"), en);
WriteJson(Path.Combine(langDir, "ru.json"), ru);

// Create template files for DE, FR, ES (copy EN)
foreach (var lc in new[] { "de", "fr", "es" })
    WriteJson(Path.Combine(langDir, $"{lc}.json"), en);

Console.WriteLine($"en.json: {en.Count} keys");
Console.WriteLine($"ru.json: {ru.Count} keys");
Console.WriteLine("de.json, fr.json, es.json created (English template)");

static Dictionary<string, string> ExtractDict(string code, string dictName)
{
    var d = new Dictionary<string, string>();
    int start = code.IndexOf($"Dictionary<string, string> {dictName} = new()");
    if (start < 0) return d;

    // Find the dictionary initializer
    int bodyStart = code.IndexOf('{', start);
    int depth = 1, i = bodyStart + 1;
    while (depth > 0 && i < code.Length)
    {
        if (code[i] == '{') depth++;
        else if (code[i] == '}') depth--;
        i++;
    }
    string body = code[bodyStart..(i - 1)];

    // Match ["key"] = "value"
    var pattern = @"\[""([^""]+)""\]\s*=\s*""((?:[^""\\]|\\.)*)"",?";
    foreach (Match m in Regex.Matches(body, pattern, RegexOptions.Multiline))
    {
        string key = m.Groups[1].Value;
        string val = m.Groups[2].Value.Replace("\\\"", "\"").Replace("\\n", "\n");
        if (!string.IsNullOrEmpty(val))
            d[key] = val;
    }
    return d;
}

static void WriteJson(string path, Dictionary<string, string> d)
{
    var sorted = new SortedDictionary<string, string>(d);
    var opts = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    File.WriteAllText(path, JsonSerializer.Serialize(sorted, opts));
}
