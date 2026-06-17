using System;
using System.IO;
using NAudio.Wave;

string deckDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RepeatSegment", "decks");
foreach (var f in Directory.GetFiles(deckDir, "*.apkg")) File.Delete(f);

// Create a REAL beep MP3 (same as BEEP_TEST which worked)
byte[] CreateBeepMp3()
{
    int sampleRate = 44100; int samples = (int)(sampleRate * 0.3);
    var shorts = new short[samples];
    for (int i = 0; i < samples; i++) shorts[i] = (short)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 32767 * 0.7);
    var bytes = new byte[samples * 2]; Buffer.BlockCopy(shorts, 0, bytes, 0, bytes.Length);
    using var ms = new MemoryStream();
    using var writer = new NAudio.Lame.LameMP3FileWriter(ms, new WaveFormat(sampleRate, 16, 1), 128);
    writer.Write(bytes, 0, bytes.Length); writer.Flush();
    return ms.ToArray();
}

byte[] tinyJpg = new byte[]{0xFF,0xD8,0xFF,0xE0,0x00,0x10,0x4A,0x46,0x49,0x46,0x00,0x01,0x01,0x00,0x00,0x01,0x00,0x01,0x00,0x00,0xFF,0xDB,0x00,0x43,0x00,0x08,0x06,0x06,0x07,0x06,0x05,0x08,0x07,0x07,0x07,0x09,0x09,0x08,0x0A,0x0C,0x14,0x0D,0x0C,0x0B,0x0B,0x0C,0x19,0x12,0x13,0x0F,0x14,0x1D,0x1A,0x1F,0x1E,0x1D,0x1A,0x1C,0x1C,0x20,0x24,0x2E,0x27,0x20,0x22,0x2C,0x23,0x1C,0x1C,0x28,0x37,0x29,0x2C,0x30,0x31,0x34,0x34,0x34,0x1F,0x27,0x39,0x3D,0x38,0x32,0x3C,0x2E,0x33,0x34,0x32,0xFF,0xC0,0x00,0x0B,0x08,0x00,0x01,0x00,0x01,0x01,0x01,0x11,0x00,0xFF,0xC4,0x00,0x1F,0x00,0x00,0x01,0x05,0x01,0x01,0x01,0x01,0x01,0x01,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x01,0x02,0x03,0x04,0x05,0x06,0x07,0x08,0x09,0x0A,0x0B,0xFF,0xDA,0x00,0x08,0x01,0x01,0x00,0x00,0x3F,0x00,0x7F,0xD9};

string tmp = Path.Combine(Path.GetTempPath(), "ui_full_sim");
Directory.CreateDirectory(tmp);
File.WriteAllBytes(Path.Combine(tmp, "img.jpg"), tinyJpg);
File.WriteAllBytes(Path.Combine(tmp, "audio.mp3"), CreateBeepMp3());

// SIMULATE EXACTLY what AnkiCardWindow does: copy to temp, add media, delete temp
var mgr = new RepeatSegment.App.AnkiExportManager("FULL_SIM");

// Simulate image: copy to temp, add, delete temp
string tmpImg = Path.GetTempFileName() + ".jpg";
File.Copy(Path.Combine(tmp, "img.jpg"), tmpImg, true);
string imgId = mgr.AddMedia(tmpImg);
File.Delete(tmpImg);

// Simulate audio: copy to temp, add, delete temp
string tmpAud = Path.GetTempFileName() + ".mp3";
File.Copy(Path.Combine(tmp, "audio.mp3"), tmpAud, true);
string audId = mgr.AddMedia(tmpAud);
File.Delete(tmpAud);

Console.WriteLine($"Added: img={imgId}, aud={audId}");

mgr.AddNote("hello", "/həˈloʊ/", "привет", imgId, audId, "test context");
string path = mgr.Finalize();

// Verify
string vt = Path.Combine(Path.GetTempPath(), "vfy_full_sim");
if (Directory.Exists(vt)) Directory.Delete(vt, true);
Directory.CreateDirectory(vt);
System.IO.Compression.ZipFile.ExtractToDirectory(path, vt);

Console.WriteLine($"\nmedia: {File.ReadAllText(Path.Combine(vt, "media"))}");
Console.WriteLine($"ZIP files:");
foreach (var f in Directory.GetFiles(vt)) Console.WriteLine($"  {Path.GetFileName(f)}: {new FileInfo(f).Length}");

using var c = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={Path.Combine(vt, "collection.anki2")}");
c.Open();
using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT flds FROM notes";
Console.WriteLine($"\nNote: {string.Join(" | ", ((string)cmd.ExecuteScalar()!).Split('\x1f'))}");

using var cmd2 = c.CreateCommand(); cmd2.CommandText = "SELECT models FROM col";
string models = (string)cmd2.ExecuteScalar()!;
Console.WriteLine($"Models: {models.Length} chars");

// Check model template names
using var jd = System.Text.Json.JsonDocument.Parse(models);
foreach (var kv in jd.RootElement.EnumerateObject())
{
    var m = kv.Value;
    Console.WriteLine($"  Model: {m.GetProperty("name")}, id={m.GetProperty("id")}");
}
c.Close();

Console.WriteLine($"\nFULL_SIM.apkg ready at: {path}");
Console.WriteLine("Import and test — this simulates EXACTLY what the UI does.");

try { Directory.Delete(tmp, true); } catch { }
try { Directory.Delete(vt, true); } catch { }
