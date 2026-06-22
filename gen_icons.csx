using System.Drawing;
using System.Drawing.Imaging;

string iconsDir = args.Length > 0 ? args[0] : @"c:\ProjectsCSharp\RepeatSegment\RepeatSegment.App\Icons";
string srcIcon = Path.Combine(iconsDir, "app.ico");

if (!File.Exists(srcIcon)) { Console.WriteLine("app.ico not found!"); return 1; }

using var icon = Icon.ExtractAssociatedIcon(srcIcon)!;
using var srcBmp = icon.ToBitmap();

var sizes = new (int w, int h, string name)[] {
    (44, 44, "Square44x44Logo"),
    (50, 50, "StoreLogo"),
    (71, 71, "SmallTile"),
    (150, 150, "Square150x150Logo"),
    (310, 150, "Wide310x150Logo"),
    (310, 310, "LargeTile"),
};

foreach (var (w, h, name) in sizes)
{
    using var resized = new Bitmap(w, h);
    using var g = Graphics.FromImage(resized);
    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
    g.DrawImage(srcBmp, 0, 0, w, h);
    string path = Path.Combine(iconsDir, name + ".png");
    resized.Save(path, ImageFormat.Png);
    Console.WriteLine($"  {name}.png ({w}×{h}) ✓");
}

Console.WriteLine($"Generated {sizes.Length} icons.");
return 0;
