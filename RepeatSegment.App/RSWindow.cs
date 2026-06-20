using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace RepeatSegment.App;

/// <summary>Base window with dark title bar support.</summary>
public class RSWindow : Window
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    public void SetDarkTitleBar(bool dark)
    {
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        int useDark = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDark, sizeof(int));
    }
}
