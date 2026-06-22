using System.Linq;
using System.Windows;

namespace RepeatSegment.App;

public partial class AboutWindow : Window
{
    private readonly MainWindow _mw;

    public AboutWindow(MainWindow mw)
    {
        _mw = mw;
        InjectBrushes();
        InitializeComponent();
        Owner = _mw;

        double sw = SystemParameters.WorkArea.Width, sh = SystemParameters.WorkArea.Height;
        Width = Math.Min(sw * 0.38, 500);
        Height = Math.Min(sh * 0.42, 420);
        MinWidth = Math.Max(360, sw * 0.26);
        MinHeight = Math.Max(260, sh * 0.22);

        BtnClose.Content = Strings.Get("sw.ok");
        Title = Strings.Get("mw.dlg.about_title");

        string about = Strings.Get("mw.dlg.about");
        var tagEnd = about.IndexOf('\n');
        TxtTagline.Text = tagEnd > 0 ? about[..tagEnd].Trim() : about;
        TxtVersion.Text = about;
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

    private void InjectBrushes()
    {
        foreach (var key in _mw.Resources.Keys)
            if (_mw.Resources[key] is System.Windows.Media.SolidColorBrush brush)
                Resources[key] = new System.Windows.Media.SolidColorBrush(brush.Color);
        if (Resources["WindowBackgroundBrush"] is System.Windows.Media.SolidColorBrush bg) Background = bg;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
