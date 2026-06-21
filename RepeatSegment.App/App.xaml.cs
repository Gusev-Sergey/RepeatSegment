using System;
using System.Windows;
using System.Windows.Threading;

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
    }
}
