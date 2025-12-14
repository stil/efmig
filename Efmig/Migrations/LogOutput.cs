using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using Efmig.Core.Abstractions;

namespace Efmig.Migrations;

public class LogOutput(TextBlock logElement, ScrollViewer scrollViewer) : ILogOutput
{
    public void ClearLog()
    {
        Dispatcher.UIThread.InvokeAsync(() => { logElement.Inlines!.Clear(); });
    }

    public void ScrollToEnd()
    {
        Dispatcher.UIThread.InvokeAsync(() => { scrollViewer.ScrollToEnd(); });
    }

    public void AddLogMessage(string message, string foreground = null, string background = null)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var run = new Run($"{message}\r\n");
            if (foreground != null)
            {
                run.Foreground = SolidColorBrush.Parse(foreground);
            }

            if (background != null)
            {
                run.Background = SolidColorBrush.Parse(background);
            }

            logElement.Inlines!.Add(run);
            scrollViewer.ScrollToEnd();
        });
    }

    public void LogInfo(string message) => AddLogMessage(message);
    public void LogError(string message) => AddLogMessage(message, "#FF0000");
    public void LogVerbose(string message) => AddLogMessage(message, "#808080");
    public void LogImportant(string message) => AddLogMessage(message, "#FFFF00");
    public void LogData(string message) => AddLogMessage(message, "#000000", "#CCCCCC");
}