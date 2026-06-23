using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;

namespace RepeatSegment.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (s, args) =>
        {
            Log.Error($"[FATAL] Unhandled exception: {args.Exception}");
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            Log.Error($"[FATAL] AppDomain unhandled: {args.ExceptionObject}");
        };

        // Warm up WebView2 environment so AnkiCardWindow opens instantly
        Task.Run(async () =>
        {
            try
            {
                string userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RepeatSegment", "WebView2");
                Directory.CreateDirectory(userData);
                var env = await CoreWebView2Environment.CreateAsync(null, userData);
                // Browser process auto-exits when unused — no cleanup needed
                Log.Info("[APP] WebView2 environment ready.");
            }
            catch (Exception ex)
            {
                Log.Error($"[APP] WebView2 warm-up failed: {ex.Message}. " +
                           "Install WebView2 Runtime: https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            }
        });
    }
}
