using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Efmig.Migrations;
using NLog;

namespace Efmig;

public class App : Application
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                desktop.MainWindow = Bootstrapper.CreateMainWindow();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not create Main Window.");
                throw;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}