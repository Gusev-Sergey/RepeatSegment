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
        BtnClose.Content = Strings.Get("sw.ok");
        TxtVersion.Text = Strings.Get("mw.dlg.about");
        TxtTech.Text = "C# WPF (.NET 8)";
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
    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    { try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true }); } catch { } e.Handled = true; }
}
