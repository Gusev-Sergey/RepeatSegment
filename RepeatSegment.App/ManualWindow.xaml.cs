using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RepeatSegment.App;

public partial class ManualWindow : Window
{
    private readonly MainWindow _mw;

    public ManualWindow(MainWindow mainWindow)
    {
        _mw = mainWindow;
        Owner = mainWindow;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        foreach (var key in mainWindow.Resources.Keys)
        {
            if (mainWindow.Resources[key] is SolidColorBrush brush)
                Resources[key] = new SolidColorBrush(brush.Color);
        }
        if (Resources["WindowBackgroundBrush"] is SolidColorBrush bg)
            Background = bg;

        InitializeComponent();
        Title = Strings.Get("manual.title");
        BuildGuide();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_mw.IsDarkTheme)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).EnsureHandle();
            int useDark = 1;
            DwmSetWindowAttribute(hwnd, 20, ref useDark, sizeof(int));
        }
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(System.IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private void BuildGuide()
    {
        var doc = GuideDocument;
        doc.Blocks.Clear();

        string iconsDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Icons");

        System.Windows.Controls.Image? LoadIcon(string name)
        {
            string path = Path.Combine(iconsDir, name);
            if (!File.Exists(path)) return null;
            var img = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new System.Uri(path)),
                Width = 20, Height = 20,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            return img;
        }

        AddHeading("RepeatSegment", 22, true);
        AddPara(Strings.GetUserGuideIntro());

        // 1. Loading
        AddHeading(Strings.GetGuideSection(1), 16, true);
        AddPara(Strings.GetGuideContent(1));

        // 2. Playback Controls — with icons
        AddHeading(Strings.GetGuideSection(2), 16, true);
        AddInlinePara(
            LoadIcon("first.png"), " First", " | ",
            LoadIcon("pre_play.png"), " Prev (Left)", " | ",
            LoadIcon("repeat.png"), " Repeat (M)", " | ",
            LoadIcon("play_go.png"), " Play&Go (Ctrl+Space)", " | ",
            LoadIcon("play.png"), " Play/Pause (Space)", " | ",
            LoadIcon("next_play.png"), " Next (Right)", " | ",
            LoadIcon("last.png"), " Last");
        AddPara(Strings.GetGuideContent(2));

        // 3-11
        for (int i = 3; i <= 11; i++)
        {
            AddHeading(Strings.GetGuideSection(i), 16, true);
            AddPara(Strings.GetGuideContent(i));
            // Section 6 has extra icon row
            if (i == 6)
                AddInlinePara(
                    LoadIcon("play.png"), " Preview sentence", " | ",
                    LoadIcon("play.png"), " Preview TTS", " | ",
                    "Search → click image → ✓ Use → Create Cards");
        }
    }

    private void AddHeading(string text, double size, bool bold)
    {
        GuideDocument.Blocks.Add(new Paragraph(new Run(text)
        {
            FontSize = size,
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Foreground = (Brush?)Resources["TextBrush"] ?? Brushes.Black
        })
        { Margin = new Thickness(0, 14, 0, 4) });
    }

    private void AddPara(string text)
    {
        GuideDocument.Blocks.Add(new Paragraph(new Run(text)
        {
            FontSize = 13,
            Foreground = (Brush?)Resources["TextBrush"] ?? Brushes.Black
        })
        { Margin = new Thickness(0, 0, 0, 8), TextAlignment = TextAlignment.Left });
    }

    private void AddInlinePara(params object?[] items)
    {
        var p = new Paragraph { Margin = new Thickness(0, 2, 0, 8) };
        var fg = (Brush?)Resources["TextBrush"] ?? Brushes.Black;
        foreach (var item in items)
        {
            if (item is System.Windows.Controls.Image img)
                p.Inlines.Add(new InlineUIContainer(img) { BaselineAlignment = BaselineAlignment.Center });
            else if (item is string s)
                p.Inlines.Add(new Run(s) { FontSize = 13, Foreground = fg });
            else if (item != null)
                p.Inlines.Add(new Run(item.ToString()) { FontSize = 13, Foreground = fg });
        }
        GuideDocument.Blocks.Add(p);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
