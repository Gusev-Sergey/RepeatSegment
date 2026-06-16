using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace RepeatSegment.App;

public class AnkiExportManager
{
    private static string DecksDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "decks");

    private readonly string _workingDir;
    private readonly long _deckId;
    private readonly string _deckName;
    private long _nextMediaId;
    private readonly Dictionary<string, string> _mediaMap = new();
    private readonly List<PendingNote> _pendingNotes = new();

    public AnkiExportManager(string deckName)
    {
        _deckName = deckName;
        _deckId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        _workingDir = Path.Combine(DecksDir, "_work_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_workingDir);
    }

    public string AddMedia(string sourcePath)
    {
        string ext = Path.GetExtension(sourcePath);
        string destName = $"m{_nextMediaId:D6}{ext}";
        string destPath = Path.Combine(_workingDir, _nextMediaId.ToString());
        File.Copy(sourcePath, destPath, overwrite: true);
        _mediaMap[_nextMediaId.ToString()] = destName;
        return (_nextMediaId++).ToString();
    }

    public void AddNote(string en, string transcription, string ru,
                        string pictureMediaId, string audioMediaId, string context)
    {
        _pendingNotes.Add(new PendingNote
        {
            En = en, Transcription = transcription, Ru = ru,
            PictureMediaId = pictureMediaId, AudioMediaId = audioMediaId, Context = context
        });
    }

    public string Finalize()
    {
        string dbPath = Path.Combine(_workingDir, "collection.anki2");
        string mediaJsonPath = Path.Combine(_workingDir, "media");

        File.WriteAllText(mediaJsonPath, JsonSerializer.Serialize(_mediaMap));

        // Build DB with DELETE journal mode (no WAL lock files)
        BuildDatabase(dbPath);
        GC.Collect(); GC.WaitForPendingFinalizers();
        System.Threading.Thread.Sleep(100);

        string apkgPath = Path.Combine(DecksDir, SanitizeFileName(_deckName) + ".apkg");
        Directory.CreateDirectory(DecksDir);

        if (File.Exists(apkgPath))
            apkgPath = MergeWithExisting(apkgPath, dbPath);
        else
            CreateNewApkg(apkgPath, dbPath);

        GC.Collect(); GC.WaitForPendingFinalizers();

        // Cleanup with retry
        for (int retry = 0; retry < 10; retry++)
        {
            try { Directory.Delete(_workingDir, recursive: true); break; }
            catch (IOException) { System.Threading.Thread.Sleep(300); GC.Collect(); }
        }
        return apkgPath;
    }

    private void CreateNewApkg(string apkgPath, string dbPath)
    {
        using var fs = File.Create(apkgPath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        zip.CreateEntryFromFile(dbPath, "collection.anki2");
        var mediaEntry = zip.CreateEntry("media");
        using (var ms = mediaEntry.Open())
            ms.Write(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_mediaMap)));
        for (int i = 0; i < _nextMediaId; i++)
        {
            string mf = Path.Combine(_workingDir, i.ToString());
            if (File.Exists(mf)) zip.CreateEntryFromFile(mf, i.ToString());
        }
    }

    private string MergeWithExisting(string existingApkg, string newDbPath)
    {
        string tempDir = Path.Combine(DecksDir, "_merge_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            using (var zip = ZipFile.OpenRead(existingApkg))
                foreach (var entry in zip.Entries)
                    entry.ExtractToFile(Path.Combine(tempDir, entry.Name), overwrite: true);

            string existingDb = Path.Combine(tempDir, "collection.anki2");
            string existingMediaPath = Path.Combine(tempDir, "media");

            Dictionary<string, string> mergedMedia = new();
            int maxMediaKey = 0;
            if (File.Exists(existingMediaPath))
            {
                mergedMedia = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(existingMediaPath)) ?? new();
                foreach (var k in mergedMedia.Keys)
                    if (int.TryParse(k, out int ik) && ik > maxMediaKey) maxMediaKey = ik;
            }

            var keyRemap = new Dictionary<int, int>();
            foreach (var kv in _mediaMap)
            {
                int oldKey = int.Parse(kv.Key);
                int newKey = ++maxMediaKey;
                keyRemap[oldKey] = newKey;
                mergedMedia[newKey.ToString()] = kv.Value;
                string src = Path.Combine(_workingDir, oldKey.ToString());
                string dst = Path.Combine(tempDir, newKey.ToString());
                if (File.Exists(src)) File.Copy(src, dst, overwrite: true);
            }

            MergeSqlite(existingDb, newDbPath, keyRemap);
            File.WriteAllText(existingMediaPath, JsonSerializer.Serialize(mergedMedia));

            string mergedApkg = Path.Combine(DecksDir, SanitizeFileName(_deckName) + "_merged.apkg");
            if (File.Exists(mergedApkg)) File.Delete(mergedApkg);
            using (var fs = File.Create(mergedApkg))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                foreach (string fp in Directory.GetFiles(tempDir))
                    zip.CreateEntryFromFile(fp, Path.GetFileName(fp));

            // Replace original with merged
            string backup = existingApkg + ".bak";
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(existingApkg, backup);
            File.Move(mergedApkg, existingApkg);
            try { File.Delete(backup); } catch { }

            return existingApkg;
        }
        finally { try { Directory.Delete(tempDir, recursive: true); } catch { } }
    }

    private void MergeSqlite(string existingDb, string newDb, Dictionary<int, int> keyRemap)
    {
        var allNotes = new List<PendingNote>();

        if (File.Exists(existingDb))
        {
            using var conn = new SqliteConnection($"Data Source={existingDb}");
            conn.Open();
            using var pragmaCmd = conn.CreateCommand();
            pragmaCmd.CommandText = "PRAGMA journal_mode=DELETE;";
            pragmaCmd.ExecuteNonQuery();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT flds FROM notes ORDER BY id";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string[] fields = reader.GetString(0).Split('\x1f');
                allNotes.Add(new PendingNote
                {
                    En = fields.Length > 0 ? fields[0] : "",
                    Transcription = fields.Length > 1 ? fields[1] : "",
                    Ru = fields.Length > 2 ? fields[2] : "",
                    PictureMediaId = fields.Length > 3 ? fields[3] : "",
                    AudioMediaId = fields.Length > 4 ? fields[4] : "",
                    Context = fields.Length > 5 ? fields[5] : ""
                });
            }
        }

        foreach (var note in _pendingNotes)
        {
            allNotes.Add(new PendingNote
            {
                En = note.En, Transcription = note.Transcription, Ru = note.Ru,
                PictureMediaId = RemapMedia(note.PictureMediaId, keyRemap),
                AudioMediaId = RemapMedia(note.AudioMediaId, keyRemap),
                Context = note.Context
            });
        }

        _pendingNotes.Clear();
        _pendingNotes.AddRange(allNotes);
        File.Delete(existingDb);
        BuildDatabase(existingDb);
    }

    private static string RemapMedia(string id, Dictionary<int, int> map)
    {
        if (string.IsNullOrEmpty(id)) return "";
        return int.TryParse(id, out int ik) && map.TryGetValue(ik, out int nk) ? nk.ToString() : id;
    }

    private void BuildDatabase(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        // Force DELETE journal mode — no WAL files to lock
        Execute(conn, "PRAGMA journal_mode=DELETE;");
        using var tx = conn.BeginTransaction();

        Execute(conn, @"CREATE TABLE IF NOT EXISTS col (id INTEGER PRIMARY KEY, crt INTEGER NOT NULL, mod INTEGER NOT NULL, scm INTEGER NOT NULL, ver INTEGER NOT NULL, dty INTEGER NOT NULL, usn INTEGER NOT NULL, ls INTEGER NOT NULL, conf TEXT NOT NULL, models TEXT NOT NULL, decks TEXT NOT NULL, dconf TEXT NOT NULL, tags TEXT NOT NULL)");
        Execute(conn, @"CREATE TABLE IF NOT EXISTS cards (id INTEGER PRIMARY KEY, nid INTEGER NOT NULL, did INTEGER NOT NULL, ord INTEGER NOT NULL, mod INTEGER NOT NULL, usn INTEGER NOT NULL, type INTEGER NOT NULL DEFAULT 0, queue INTEGER NOT NULL DEFAULT 0, due INTEGER NOT NULL, ivl INTEGER NOT NULL DEFAULT 0, factor INTEGER NOT NULL DEFAULT 0, reps INTEGER NOT NULL DEFAULT 0, lapses INTEGER NOT NULL DEFAULT 0, left INTEGER NOT NULL DEFAULT 0, odue INTEGER NOT NULL DEFAULT 0, odid INTEGER NOT NULL DEFAULT 0, flags INTEGER NOT NULL DEFAULT 0, data TEXT NOT NULL DEFAULT '')");
        Execute(conn, @"CREATE TABLE IF NOT EXISTS notes (id INTEGER PRIMARY KEY, guid TEXT NOT NULL, mid INTEGER NOT NULL, mod INTEGER NOT NULL, usn INTEGER NOT NULL, tags TEXT NOT NULL, flds TEXT NOT NULL, sfld TEXT NOT NULL, csum INTEGER NOT NULL, flags INTEGER NOT NULL DEFAULT 0, data TEXT NOT NULL DEFAULT '')");
        Execute(conn, @"CREATE TABLE IF NOT EXISTS graves (usn INTEGER NOT NULL, oid INTEGER NOT NULL, type INTEGER NOT NULL)");
        Execute(conn, @"CREATE TABLE IF NOT EXISTS revlog (id INTEGER PRIMARY KEY, cid INTEGER NOT NULL, usn INTEGER NOT NULL, ease INTEGER NOT NULL, ivl INTEGER NOT NULL, lastIvl INTEGER NOT NULL, factor INTEGER NOT NULL, time INTEGER NOT NULL, type INTEGER NOT NULL)");

        Execute(conn, "DELETE FROM col");

        long nowSec = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        Execute(conn, "INSERT INTO col (id, crt, mod, scm, ver, dty, usn, ls, conf, models, decks, dconf, tags) VALUES (@id, @crt, @mod, @scm, @ver, @dty, @usn, @ls, @conf, @models, @decks, @dconf, @tags)",
            ("@id", 1L), ("@crt", nowSec), ("@mod", nowSec), ("@scm", nowMs), ("@ver", 11L),
            ("@dty", 0L), ("@usn", -1L), ("@ls", 0L),
            ("@conf", "{}"), ("@models", BuildModelsJson()),
            ("@decks", BuildDecksJson(nowSec)), ("@dconf", BuildDconfJson()), ("@tags", "{}"));

        long noteId = 1, cardId = 1;
        foreach (var note in _pendingNotes)
        {
            string flds = string.Join("\x1f", new[] {
                note.En, note.Transcription, note.Ru,
                note.PictureMediaId, note.AudioMediaId, note.Context
            });
            string guid = Guid.NewGuid().ToString("N");
            long csum = unchecked((long)(uint)ComputeCrc32(flds));

            Execute(conn, "INSERT INTO notes (id, guid, mid, mod, usn, tags, flds, sfld, csum, flags, data) VALUES (@id, @guid, @mid, @mod, @usn, @tags, @flds, @sfld, @csum, @flags, @data)",
                ("@id", noteId), ("@guid", guid), ("@mid", 1728000000001L),
                ("@mod", nowSec), ("@usn", -1L), ("@tags", ""),
                ("@flds", flds), ("@sfld", note.En), ("@csum", csum),
                ("@flags", 0L), ("@data", ""));

            // Card 1: en→ru
            Execute(conn, "INSERT INTO cards (id, nid, did, ord, mod, usn, type, queue, due, ivl, factor, reps, lapses, left, odue, odid, flags, data) VALUES (@id, @nid, @did, @ord, @mod, @usn, @type, @queue, @due, @ivl, @factor, @reps, @lapses, @left, @odue, @odid, @flags, @data)",
                ("@id", cardId), ("@nid", noteId), ("@did", _deckId), ("@ord", 0L),
                ("@mod", nowSec), ("@usn", -1L), ("@type", 0L), ("@queue", 0L), ("@due", noteId),
                ("@ivl", 0L), ("@factor", 0L), ("@reps", 0L), ("@lapses", 0L), ("@left", 0L),
                ("@odue", 0L), ("@odid", 0L), ("@flags", 0L), ("@data", ""));
            cardId++;

            // Card 2: ru→en
            Execute(conn, "INSERT INTO cards (id, nid, did, ord, mod, usn, type, queue, due, ivl, factor, reps, lapses, left, odue, odid, flags, data) VALUES (@id, @nid, @did, @ord, @mod, @usn, @type, @queue, @due, @ivl, @factor, @reps, @lapses, @left, @odue, @odid, @flags, @data)",
                ("@id", cardId), ("@nid", noteId), ("@did", _deckId), ("@ord", 1L),
                ("@mod", nowSec), ("@usn", -1L), ("@type", 0L), ("@queue", 0L), ("@due", noteId),
                ("@ivl", 0L), ("@factor", 0L), ("@reps", 0L), ("@lapses", 0L), ("@left", 0L),
                ("@odue", 0L), ("@odid", 0L), ("@flags", 0L), ("@data", ""));
            cardId++;

            noteId++;
        }

        tx.Commit();
    }

    private static void Execute(SqliteConnection conn, string sql, params (string, object)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }
        cmd.ExecuteNonQuery();
    }

    private static string BuildModelsJson()
    {
        string frontEnRu = @"{{en}}<br/><br/>{{#audio}}{{audio}}{{/audio}}{{^audio}}&nbsp;{{/audio}}<br/>{{#picture}}{{picture}}{{/picture}}";
        string backEnRu = @"<hr id=answer>{{ru}}<br/><br/><i>{{transcription}}</i><br/><br/><small>{{context}}</small>";
        string frontRuEn = @"{{ru}}";
        string backRuEn = @"<hr id=answer>{{en}}<br/><br/>{{#audio}}{{audio}}{{/audio}}<br/><i>{{transcription}}</i><br/><br/><small>{{context}}</small>";

        using var doc = JsonDocument.Parse("{}");
        var json = $@"{{
    ""1728000000001"": {{
        ""id"": 1728000000001,
        ""name"": ""RepeatSegment"",
        ""type"": 0,
        ""mod"": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
        ""usn"": -1,
        ""sortf"": 0,
        ""did"": null,
        ""tmpls"": [
            {{""name"": ""en → ru"", ""ord"": 0, ""qfmt"": {System.Text.Json.JsonSerializer.Serialize(frontEnRu)}, ""afmt"": {System.Text.Json.JsonSerializer.Serialize(backEnRu)}, ""bqfmt"": """", ""bafmt"": """", ""did"": null, ""bfont"": """", ""bsize"": 0}},
            {{""name"": ""ru → en"", ""ord"": 1, ""qfmt"": {System.Text.Json.JsonSerializer.Serialize(frontRuEn)}, ""afmt"": {System.Text.Json.JsonSerializer.Serialize(backRuEn)}, ""bqfmt"": """", ""bafmt"": """", ""did"": null, ""bfont"": """", ""bsize"": 0}}
        ],
        ""flds"": [
            {{""name"": ""en"", ""ord"": 0}},
            {{""name"": ""transcription"", ""ord"": 1}},
            {{""name"": ""ru"", ""ord"": 2}},
            {{""name"": ""picture"", ""ord"": 3}},
            {{""name"": ""audio"", ""ord"": 4}},
            {{""name"": ""context"", ""ord"": 5}}
        ],
        ""css"": "".card {{ font-family: 'Segoe UI', Arial, sans-serif; font-size: 20px; text-align: center; color: black; background-color: white; }} img {{ max-width: 500px; max-height: 400px; }}"",
        ""latexPre"": """",
        ""latexPost"": """",
        ""req"": [[0, ""any"", [0]], [1, ""any"", [2]]],
        ""vers"": []
    }}
}}";
        return json;
    }

    private string BuildDecksJson(long now)
    {
        return $@"{{
    ""1"": {{""id"": 1, ""name"": ""Default"", ""mod"": {now}, ""usn"": -1, ""lrnToday"": [0, 0], ""revToday"": [0, 0], ""newToday"": [20, 0], ""timeToday"": [0, 0], ""collapsed"": false, ""dyn"": 0, ""desc"": """", ""extendNew"": 10, ""extendRev"": 50, ""browserCollapsed"": false}},
    ""{_deckId}"": {{""id"": {_deckId}, ""name"": {System.Text.Json.JsonSerializer.Serialize(_deckName)}, ""mod"": {now}, ""usn"": -1, ""lrnToday"": [0, 0], ""revToday"": [0, 0], ""newToday"": [20, 0], ""timeToday"": [0, 0], ""collapsed"": false, ""dyn"": 0, ""desc"": """", ""extendNew"": 10, ""extendRev"": 50, ""browserCollapsed"": false}}
}}";
    }

    private static string BuildDconfJson()
    {
        return @"{""1"": {""id"": 1, ""name"": ""Default"", ""mod"": 0, ""usn"": -1, ""maxTaken"": 60, ""autoplay"": true, ""timer"": 0, ""replayq"": true, ""new"": {""delays"": [1.0, 10.0], ""ints"": [1, 4, 7], ""initialFactor"": 2500, ""bury"": true, ""order"": 1, ""perDay"": 20}, ""rev"": {""perDay"": 200, ""ease4"": 1.3, ""fuzz"": 0.05, ""minSpace"": 1, ""ivlFct"": 1.0, ""maxIvl"": 36500, ""bury"": true, ""hardFactor"": 1.2}, ""lapse"": {""delays"": [10.0], ""minInt"": 1, ""leechFails"": 8, ""leechAction"": 0}, ""dyn"": false}}";
    }

    private static string SanitizeFileName(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name;
    }

    private static uint ComputeCrc32(string input)
    {
        uint crc = 0xFFFFFFFF;
        foreach (char c in input)
        {
            crc ^= (byte)c;
            for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320 : 0);
            if (c > 255)
            {
                crc ^= (byte)(c >> 8);
                for (int i = 0; i < 8; i++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320 : 0);
            }
        }
        return crc ^ 0xFFFFFFFF;
    }

    public static string[] ListDecks()
    {
        Directory.CreateDirectory(DecksDir);
        var files = Directory.GetFiles(DecksDir, "*.apkg");
        var names = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
            names[i] = Path.GetFileNameWithoutExtension(files[i]);
        return names;
    }

    public static bool DeckExists(string name) =>
        File.Exists(Path.Combine(DecksDir, SanitizeFileName(name) + ".apkg"));

    private class PendingNote
    {
        public string En { get; set; } = "";
        public string Transcription { get; set; } = "";
        public string Ru { get; set; } = "";
        public string PictureMediaId { get; set; } = "";
        public string AudioMediaId { get; set; } = "";
        public string Context { get; set; } = "";
    }
}
