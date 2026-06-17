using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using DiagAnki;
using Microsoft.Data.Sqlite;

namespace RepeatSegment.App;

public class AnkiExportManager
{
    private static string? s_lastDeck;

    private readonly string _deckName;
    private readonly List<NoteData> _notes = new();
    private readonly List<MediaData> _media = new();
    private int _nextMediaId;
    private int _nextZipId;

    public AnkiExportManager(string deckName)
    {
        _deckName = deckName;
        s_lastDeck = deckName;

        string apkgPath = GetApkgPath(deckName);
        if (!File.Exists(apkgPath)) return;

        try
        {
            string tmp = Path.GetTempFileName();
            using (var z = ZipFile.OpenRead(apkgPath))
            {
                var dbEntry = z.GetEntry("collection.anki2");
                if (dbEntry != null) dbEntry.ExtractToFile(tmp, true);
            }

            using var c = new SqliteConnection($"Data Source={tmp}");
            c.Open();
            using var cmd = c.CreateCommand();
            cmd.CommandText = "SELECT flds FROM notes ORDER BY id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var f = r.GetString(0).Split('\x1f');
                // Strip HTML/sound wrappers — BuildDb adds them fresh
                string pic = StripImgTag(Pf(f, 3));
                string aud = StripSoundTag(Pf(f, 4));
                _notes.Add(new NoteData
                {
                    En = Pf(f, 0), Transcription = Pf(f, 1), Ru = Pf(f, 2),
                    PictureMediaId = pic, AudioMediaId = aud, Context = Pf(f, 5)
                });
            }
            c.Close();
            SqliteConnection.ClearAllPools();
            try { File.Delete(tmp); } catch { }

            // Find max IDs
            foreach (var n in _notes)
            {
                foreach (var mid in new[] { n.PictureMediaId, n.AudioMediaId })
                {
                    if (string.IsNullOrEmpty(mid)) continue;
                    int dot = mid.LastIndexOf('.');
                    string numPart = dot > 0 && mid.StartsWith("m") ? mid[1..dot] : mid;
                    if (int.TryParse(numPart, out int id) && id >= _nextMediaId) _nextMediaId = id + 1;
                }
            }

            using (var z = ZipFile.OpenRead(apkgPath))
                foreach (var entry in z.Entries)
                    if (int.TryParse(entry.Name, out int zn) && zn >= _nextZipId)
                        _nextZipId = zn + 1;
        }
        catch { _notes.Clear(); _nextMediaId = 0; _nextZipId = 0; }
    }

    static string StripImgTag(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var m = Regex.Match(s, @"src\s*=\s*[""']([^""']+)[""']");
        return m.Success ? m.Groups[1].Value : s;
    }

    static string StripSoundTag(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var m = Regex.Match(s, @"\[sound:([^\]]+)\]");
        return m.Success ? m.Groups[1].Value : s;
    }

    public static string? LastDeck => s_lastDeck;

    public string AddMedia(string sourcePath)
    {
        string ext = Path.GetExtension(sourcePath);
        string name = $"m{_nextMediaId}{ext}";
        _nextMediaId++;
        _media.Add(new MediaData { Bytes = File.ReadAllBytes(sourcePath), Extension = ext, DescriptiveName = name, ZipName = (_nextZipId++).ToString() });
        return name;
    }

    public void AddNote(string en, string tr, string ru, string pic, string aud, string ctx)
        => _notes.Add(new NoteData { En = en, Transcription = tr, Ru = ru, PictureMediaId = pic, AudioMediaId = aud, Context = ctx });

    public string Finalize() => AnkiBuilder.BuildDeck(_deckName, _notes, _media);

    public static string[] ListDecks() => AnkiBuilder.ListDecks();

    static string GetApkgPath(string name)
    {
        var sanitized = name;
        foreach (char c in Path.GetInvalidFileNameChars()) sanitized = sanitized.Replace(c, '_');
        return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "decks"), sanitized + ".apkg");
    }

    static string Pf(string[] a, int i) => i < a.Length ? a[i] : "";
}
