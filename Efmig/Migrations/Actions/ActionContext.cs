using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;

namespace Efmig.Migrations.Actions;

public class ActionContext
{
    private readonly TextBlock _logElement;
    private readonly ScrollViewer _scrollViewer;
    private readonly DispatcherTimer _flush;
    private readonly ObservableCollection<Run> _logBuffer = new();
    
    public ActionContext(TextBlock logElement, ScrollViewer scrollViewer, ConfigurationProfile profile)
    {
        _logElement = logElement;
        _scrollViewer = scrollViewer;
        ConfigurationProfile = profile;
        _flush = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _flush.Tick += Buffer;
        _flush.Start();
    }

    private void Buffer(object sender, EventArgs e)
    {
        while (_logBuffer.Count > 0)
        {
            var run = _logBuffer[0];
            _logElement.Inlines!.Add(run);
            _logBuffer.RemoveAt(0);
        }
        _scrollViewer.ScrollToEnd();
    }

    public ConfigurationProfile ConfigurationProfile { get; }
    public object Data { get; set; }

    public void ClearLog()
    {
        Dispatcher.UIThread.InvokeAsync(() => { _logElement.Inlines!.Clear(); });
    }

    public void ScrollToEnd()
    {
        Dispatcher.UIThread.InvokeAsync(() => { _scrollViewer.ScrollToEnd(); });
    }

    private void AddLogMessage(string message, string foreground = null, string background = null)
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
            _logBuffer.Add(run);
           
        });
    }

    public void LogInfo(string message) => AddLogMessage(message);
    public void LogError(string message) => AddLogMessage(message, "#FF0000");
    public void LogVerbose(string message) => AddLogMessage(message, "#808080");
    public void LogImportant(string message) => AddLogMessage(message, "#FFFF00");
    public void LogData(string message) => AddLogMessage(message, "#000000", "#CCCCCC");
}