using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace DiagAnki;

public static class AnkiBuilder
{
    public static string BuildDeck(string deckName, List<NoteData> notes, List<MediaData> media)
    {
        string decksDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "decks");
        Directory.CreateDirectory(decksDir);

        string sanitized = deckName;
        foreach (char c in Path.GetInvalidFileNameChars()) sanitized = sanitized.Replace(c, '_');
        string apkgPath = Path.Combine(decksDir, sanitized + ".apkg");

        string workDir = Path.Combine(Path.GetTempPath(), "rs_anki_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(workDir);

        try
        {
            var allMediaBytes = new Dictionary<string, byte[]>();
            var mediaMap = new Dictionary<string, string>();

            // Extract old media
            if (File.Exists(apkgPath))
            {
                try
                {
                    using var oz = ZipFile.OpenRead(apkgPath);
                    var me = oz.GetEntry("media");
                    if (me != null) { using var sr = new StreamReader(me.Open()); var om = JsonSerializer.Deserialize<Dictionary<string, string>>(sr.ReadToEnd()) ?? new();
                        foreach (var kv in om) { var e = oz.GetEntry(kv.Key); if (e != null) { using var ms = new MemoryStream(); e.Open().CopyTo(ms); allMediaBytes[kv.Key] = ms.ToArray(); mediaMap[kv.Key] = kv.Value; } } }
                } catch { }
            }

            // Add new media
            foreach (var m in media)
            {
                string zn = m.ZipName; while (mediaMap.ContainsKey(zn) || allMediaBytes.ContainsKey(zn)) zn = (int.Parse(zn) + 1).ToString();
                allMediaBytes[zn] = m.Bytes; mediaMap[zn] = m.DescriptiveName;
            }

            foreach (var kv in allMediaBytes) File.WriteAllBytes(Path.Combine(workDir, kv.Key), kv.Value);
            File.WriteAllText(Path.Combine(workDir, "media"), JsonSerializer.Serialize(mediaMap), new UTF8Encoding(false));

            BuildDb(Path.Combine(workDir, "collection.anki2"), deckName, notes);

            SqliteConnection.ClearAllPools(); GC.Collect(); GC.WaitForPendingFinalizers(); Thread.Sleep(200);
            if (File.Exists(apkgPath)) { for (int r = 0; r < 5; r++) try { File.Delete(apkgPath); break; } catch { Thread.Sleep(500); } }
            ZipFile.CreateFromDirectory(workDir, apkgPath); Thread.Sleep(100);
            return apkgPath;
        }
        finally { for (int r = 0; r < 5; r++) try { Directory.Delete(workDir, true); break; } catch { Thread.Sleep(300); } }
    }

    static void BuildDb(string path, string deckName, List<NoteData> notes)
    {
        using var c = new SqliteConnection($"Data Source={path}");
        c.Open(); c.Execute("PRAGMA journal_mode=DELETE;");
        using var tx = c.BeginTransaction();
        c.Execute("DROP TABLE IF EXISTS cards"); c.Execute("DROP TABLE IF EXISTS notes"); c.Execute("DROP TABLE IF EXISTS col"); c.Execute("DROP TABLE IF EXISTS graves"); c.Execute("DROP TABLE IF EXISTS revlog");
        c.Execute(@"CREATE TABLE col (id INTEGER PRIMARY KEY, crt INTEGER NOT NULL, mod INTEGER NOT NULL, scm INTEGER NOT NULL, ver INTEGER NOT NULL, dty INTEGER NOT NULL, usn INTEGER NOT NULL, ls INTEGER NOT NULL, conf TEXT NOT NULL, models TEXT NOT NULL, decks TEXT NOT NULL, dconf TEXT NOT NULL, tags TEXT NOT NULL)");
        c.Execute(@"CREATE TABLE notes (id INTEGER PRIMARY KEY, guid TEXT NOT NULL, mid INTEGER NOT NULL, mod INTEGER NOT NULL, usn INTEGER NOT NULL, tags TEXT NOT NULL, flds TEXT NOT NULL, sfld TEXT NOT NULL, csum INTEGER NOT NULL, flags INTEGER NOT NULL DEFAULT 0, data TEXT NOT NULL DEFAULT '')");
        c.Execute(@"CREATE TABLE cards (id INTEGER PRIMARY KEY, nid INTEGER NOT NULL, did INTEGER NOT NULL, ord INTEGER NOT NULL, mod INTEGER NOT NULL, usn INTEGER NOT NULL, type INTEGER NOT NULL DEFAULT 0, queue INTEGER NOT NULL DEFAULT 0, due INTEGER NOT NULL, ivl INTEGER NOT NULL DEFAULT 0, factor INTEGER NOT NULL DEFAULT 0, reps INTEGER NOT NULL DEFAULT 0, lapses INTEGER NOT NULL DEFAULT 0, left INTEGER NOT NULL DEFAULT 0, odue INTEGER NOT NULL DEFAULT 0, odid INTEGER NOT NULL DEFAULT 0, flags INTEGER NOT NULL DEFAULT 0, data TEXT NOT NULL DEFAULT '')");
        c.Execute("CREATE TABLE graves (usn INTEGER NOT NULL, oid INTEGER NOT NULL, type INTEGER NOT NULL)"); c.Execute("CREATE TABLE revlog (id INTEGER PRIMARY KEY, cid INTEGER NOT NULL, usn INTEGER NOT NULL, ease INTEGER NOT NULL, ivl INTEGER NOT NULL, lastIvl INTEGER NOT NULL, factor INTEGER NOT NULL, time INTEGER NOT NULL, type INTEGER NOT NULL)");

        int ts = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long did = 1234567890;
        long mid = 1728005000L + (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 1000000L);
        long dcid = 900000000 + (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 100000000);
        c.Execute("INSERT INTO col (id,crt,mod,scm,ver,dty,usn,ls,conf,models,decks,dconf,tags) VALUES (@id,@crt,@mod,@scm,@ver,@dty,@usn,@ls,@conf,@models,@decks,@dconf,@tags)", ("@id",1L),("@crt",ts),("@mod",ts),("@scm",ts),("@ver",11L),("@dty",0L),("@usn",-1L),("@ls",0L),("@conf","{}"),("@models",MJson(mid,ts)),("@decks",DJson(did,ts,deckName,dcid)),("@dconf",DCJson(dcid)),("@tags","{}"));

        long nid = 1, cid = 1;
        foreach (var n in notes)
        {
            // EXACT July_2015 format: <img src="filename"> and [sound:filename]
            string img = string.IsNullOrEmpty(n.PictureMediaId) ? "" : $"<img src=\"{n.PictureMediaId}\">";
            // TTS first (🔊), sentence second (📖) — labels so user knows which is which
            var sndParts = new List<string>();
            if (!string.IsNullOrEmpty(n.TtsAudioMediaId))
                sndParts.Add($"🔊 Word [sound:{n.TtsAudioMediaId}]");
            if (!string.IsNullOrEmpty(n.SentenceAudioMediaId))
                sndParts.Add($"📖 Sentence [sound:{n.SentenceAudioMediaId}]");
            if (sndParts.Count == 0 && !string.IsNullOrEmpty(n.AudioMediaId))
                sndParts.Add($"[sound:{n.AudioMediaId}]");
            string snd = string.Join("\n", sndParts);
            string flds = string.Join("\x1f", n.En, n.Transcription, n.Ru, img, snd, n.Context);
            int cs = unchecked((int)Cr32(flds));
            c.Execute("INSERT INTO notes (id,guid,mid,mod,usn,tags,flds,sfld,csum,flags,data) VALUES (@id,@guid,@mid,@mod,@usn,@tags,@flds,@sfld,@csum,@flags,@data)", ("@id",nid),("@guid",Guid.NewGuid().ToString("N")),("@mid",mid),("@mod",ts),("@usn",-1L),("@tags",""),("@flds",flds),("@sfld",n.En),("@csum",cs),("@flags",0L),("@data",""));
            c.Execute("INSERT INTO cards (id,nid,did,ord,mod,usn,type,queue,due,ivl,factor,reps,lapses,left,odue,odid,flags,data) VALUES (@id,@nid,@did,@ord,@mod,@usn,@type,@queue,@due,@ivl,@factor,@reps,@lapses,@left,@odue,@odid,@flags,@data)", ("@id",cid),("@nid",nid),("@did",did),("@ord",0L),("@mod",ts),("@usn",-1L),("@type",0L),("@queue",0L),("@due",nid),("@ivl",0L),("@factor",0L),("@reps",0L),("@lapses",0L),("@left",0L),("@odue",0L),("@odid",0L),("@flags",0L),("@data",""));cid++;
            c.Execute("INSERT INTO cards (id,nid,did,ord,mod,usn,type,queue,due,ivl,factor,reps,lapses,left,odue,odid,flags,data) VALUES (@id,@nid,@did,@ord,@mod,@usn,@type,@queue,@due,@ivl,@factor,@reps,@lapses,@left,@odue,@odid,@flags,@data)", ("@id",cid),("@nid",nid),("@did",did),("@ord",1L),("@mod",ts),("@usn",-1L),("@type",0L),("@queue",0L),("@due",nid),("@ivl",0L),("@factor",0L),("@reps",0L),("@lapses",0L),("@left",0L),("@odue",0L),("@odid",0L),("@flags",0L),("@data",""));cid++;nid++;
        }
        tx.Commit(); c.Close();
    }

    static string MJson(long mid, int ts)
    {
        string t0qf = "<div class=word>{{word}}</div>\n<div class=transc>[{{transcription}}]</div>\n{{sound}}\n<br>\n{{image}}";
        string t0af = "{{FrontSide}}\n\n<hr id=answer>\n<div class=transl>{{translation}}</div>\n<div class=cont>{{context}}</div>";
        string t1qf = "<div class=transl>{{translation}}</div>\n{{image}}";
        string t1af = "{{FrontSide}}\n\n<hr id=answer>\n<div class=word>{{word}}</div>\n<div class=transc>[{{transcription}}]</div>\n{{sound}}\n<div class=cont>{{context}}</div>";
        var m = new Dictionary<string,object>{[mid.ToString()]=new{id=mid,name="RepeatSegment "+mid,type=0,mod=ts,usn=-1,sortf=0,did=(long?)null,
            tmpls=new[]{new{name="en→ru",ord=0,qfmt=t0qf,afmt=t0af,bqfmt="",bafmt="",did=(long?)null,bfont="",bsize=0},new{name="ru→en",ord=1,qfmt=t1qf,afmt=t1af,bqfmt="",bafmt="",did=(long?)null,bfont="",bsize=0}},
            flds=new[]{new{name="word",ord=0,sticky=false,rtl=false,font="Arial",size=20,description="",plainText=false,collapsed=false,excludeFromSearch=false,tag=(string?)null,preventDeletion=false},new{name="transcription",ord=1,sticky=false,rtl=false,font="Arial",size=20,description="",plainText=false,collapsed=false,excludeFromSearch=false,tag=(string?)null,preventDeletion=false},new{name="translation",ord=2,sticky=false,rtl=false,font="Arial",size=20,description="",plainText=false,collapsed=false,excludeFromSearch=false,tag=(string?)null,preventDeletion=false},new{name="image",ord=3,sticky=false,rtl=false,font="Arial",size=20,description="",plainText=false,collapsed=false,excludeFromSearch=false,tag=(string?)null,preventDeletion=false},new{name="sound",ord=4,sticky=false,rtl=false,font="Arial",size=20,description="",plainText=false,collapsed=false,excludeFromSearch=false,tag=(string?)null,preventDeletion=false},new{name="context",ord=5,sticky=false,rtl=false,font="Arial",size=20,description="",plainText=false,collapsed=false,excludeFromSearch=false,tag=(string?)null,preventDeletion=false}},
            css=".card{font-family:Arial,sans-serif;text-align:center;background-color:#1e1e1e;color:#ddd;padding:10px}.word{font-size:28px;color:#f0f0f0}.transc{font-size:16px;color:#6cb4ee;font-family:'Arial Unicode MS',sans-serif}.transl{font-size:22px;color:#d4a373;margin:0 0 10px}.cont{font-size:15px;font-style:italic;color:#aaa;margin:10px 0 0}img{max-width:500px;max-height:350px}hr{border-color:#555;margin:12px 0}",
            latexPre="",latexPost="", req=new[]{new object[]{0,"any",new[]{0,1,4}},new object[]{1,"any",new[]{2,3,5}}},vers=Array.Empty<object>()}};
        return JsonSerializer.Serialize(m,new JsonSerializerOptions{WriteIndented=false});
    }
    static string DJson(long did, int ts, string dn, long dcid) => $@"{{""1"":{{""id"":1,""name"":""Default"",""mod"":{ts},""usn"":-1,""lrnToday"":[0,0],""revToday"":[0,0],""newToday"":[20,0],""timeToday"":[0,0],""collapsed"":false,""dyn"":0,""desc"":"""",""extendNew"":10,""extendRev"":50,""browserCollapsed"":false,""conf"":1}},""{did}"":{{""id"":{did},""name"":{JsonSerializer.Serialize(dn)},""mod"":{ts},""usn"":-1,""lrnToday"":[0,0],""revToday"":[0,0],""newToday"":[20,0],""timeToday"":[0,0],""collapsed"":false,""dyn"":0,""desc"":"""",""extendNew"":10,""extendRev"":50,""browserCollapsed"":false,""conf"":{dcid}}}}}";
    static string DCJson(long dcid) => $@"{{""1"":{{""id"":1,""name"":""Default"",""mod"":0,""usn"":-1,""maxTaken"":60,""autoplay"":false,""timer"":0,""replayq"":false,""new"":{{""delays"":[1.0,10.0],""ints"":[1,4,7],""initialFactor"":2500,""bury"":false,""order"":1,""perDay"":20}},""rev"":{{""perDay"":200,""ease4"":1.3,""fuzz"":0.05,""minSpace"":1,""ivlFct"":1.0,""maxIvl"":36500,""bury"":false,""hardFactor"":1.2}},""lapse"":{{""delays"":[10.0],""minInt"":1,""leechFails"":8,""leechAction"":0}},""dyn"":false}},""{dcid}"":{{""id"":{dcid},""name"":""RepeatSegment"",""mod"":0,""usn"":-1,""maxTaken"":60,""autoplay"":false,""timer"":0,""replayq"":false,""new"":{{""delays"":[1.0,10.0],""ints"":[1,4,7],""initialFactor"":2500,""bury"":false,""order"":1,""perDay"":20}},""rev"":{{""perDay"":200,""ease4"":1.3,""fuzz"":0.05,""minSpace"":1,""ivlFct"":1.0,""maxIvl"":36500,""bury"":false,""hardFactor"":1.2}},""lapse"":{{""delays"":[10.0],""minInt"":1,""leechFails"":8,""leechAction"":0}},""dyn"":false}}}}";
    static uint Cr32(string s){uint c=0xFFFFFFFF;foreach(char ch in s){c^=(byte)ch;for(int i=0;i<8;i++)c=(c>>1)^((c&1)!=0?0xEDB88320:0);if(ch>255){c^=(byte)(ch>>8);for(int i=0;i<8;i++)c=(c>>1)^((c&1)!=0?0xEDB88320:0);}}return c^0xFFFFFFFF;}
    public static string[] ListDecks(){string d=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"RepeatSegment","decks");Directory.CreateDirectory(d);return Directory.GetFiles(d,"*.apkg").Select(f=>Path.GetFileNameWithoutExtension(f)??"").Where(f=>f!="").Distinct().ToArray();}
}
public class NoteData{public string En="",Transcription="",Ru="",PictureMediaId="",AudioMediaId="",SentenceAudioMediaId="",TtsAudioMediaId="",Context="";}
public class MediaData{public byte[] Bytes=Array.Empty<byte>();public string Extension="",DescriptiveName="",ZipName="";}
static class SqE{public static void Execute(this SqliteConnection c,string sql,params(string,object)[]p){using var cmd=c.CreateCommand();cmd.CommandText=sql;foreach(var(n,v)in p){var px=cmd.CreateParameter();px.ParameterName=n;px.Value=v;cmd.Parameters.Add(px);}cmd.ExecuteNonQuery();}}
