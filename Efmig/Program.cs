using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Efmig.Migrations;
using NLog;
using ReactiveUI;

namespace Efmig;

class Program
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Logger.Info("Application started.");

        SetupExceptionHandling();

        Profiles = ProfilesManager.LoadProfiles().Result;

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            Logger.Fatal(e, "Something very bad happened");
        }
        finally
        {
            LogManager.Shutdown();
        }
    }

    public static List<ConfigurationProfile> Profiles { get; set; }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();

    private static void SetupExceptionHandling()
    {
        RxApp.DefaultExceptionHandler =
            Observer.Create<Exception>(
                exception => { LogUnhandledException(exception, "RxApp.DefaultExceptionHandler"); }, () => { });

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };
    }

    private static void LogUnhandledException(Exception exception, string source)
    {
        var message = $"Unhandled exception ({source})";
        try
        {
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            message = $"Unhandled exception in {assemblyName.Name} v{assemblyName.Version}";
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Exception in LogUnhandledException");
        }
        finally
        {
            Logger.Error(exception, message);
        }
    }
}