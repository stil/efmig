using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;

namespace Efmig.Migrations.Actions;

public class ActionContext
{
    private readonly TextBlock _logElement;
    private readonly ScrollViewer _scrollViewer;

    public ActionContext(TextBlock logElement, ScrollViewer scrollViewer, ConfigurationProfile profile)
    {
        _logElement = logElement;
        _scrollViewer = scrollViewer;
        ConfigurationProfile = profile;
    }

    public ConfigurationProfile ConfigurationProfile { get; }
    public object Data { get; set; }

    public void ClearLog()
    {
        Dispatcher.UIThread.InvokeAsync(() => { _logElement.Inlines!.Clear(); });
    }

    private void AddLogMessage(string message, string foreground = null)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var run = new Run($"{message}\r\n");
            if (foreground != null)
            {
                run.Foreground = SolidColorBrush.Parse(foreground);
            }

            _logElement.Inlines!.Add(run);
            _scrollViewer.ScrollToEnd();
        });
    }

    public void LogInfo(string message) => AddLogMessage(message);
    public void LogError(string message) => AddLogMessage(message, "#FF0000");
    public void LogVerbose(string message) => AddLogMessage(message, "#808080");
    public void LogImportant(string message) => AddLogMessage(message, "#FFFF00");
}