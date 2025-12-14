using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using Efmig.Core.Abstractions;

namespace Efmig.Migrations;

public class LogOutput : ILogOutput
{
    private readonly DispatcherTimer _flushTimer;
    private readonly TextBlock _logElement;
    private readonly ConcurrentQueue<(string message, string foreground, string background)> _messageQueue = new();
    private readonly DispatcherTimer _scrollTimer;
    private bool _needsScroll;

    public LogOutput(TextBlock logElement, ScrollViewer scrollViewer)
    {
        _logElement = logElement;

        // Flush queued messages every 16ms (~60 FPS)
        _flushTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _flushTimer.Tick += (s, e) => FlushMessages();
        _flushTimer.Start();

        // Debounce scrolling to reduce scroll operations
        _scrollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _scrollTimer.Tick += (s, e) =>
        {
            if (_needsScroll)
            {
                scrollViewer.ScrollToEnd();
                _needsScroll = false;
            }

            _scrollTimer.Stop();
        };
    }

    public void ClearLog()
    {
        Dispatcher.UIThread.InvokeAsync(() => { _logElement.Inlines!.Clear(); });
    }

    public void ScrollToEnd()
    {
        RequestScroll();
    }

    public void LogInfo(string message)
    {
        AddLogMessage(message);
    }

    public void LogError(string message)
    {
        AddLogMessage(message, "#FF0000");
    }

    public void LogVerbose(string message)
    {
        AddLogMessage(message, "#808080");
    }

    public void LogImportant(string message)
    {
        AddLogMessage(message, "#FFFF00");
    }

    public void LogData(string message)
    {
        AddLogMessage(message, "#000000", "#CCCCCC");
    }

    private void FlushMessages()
    {
        if (_messageQueue.IsEmpty)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            var batch = new List<Run>();
            while (_messageQueue.TryDequeue(out var item))
            {
                var run = new Run($"{item.message}\r\n");

                if (item.foreground != null)
                {
                    run.Foreground = SolidColorBrush.Parse(item.foreground);
                }

                if (item.background != null)
                {
                    run.Background = SolidColorBrush.Parse(item.background);
                }

                batch.Add(run);
            }

            // Add all runs at once instead of one-by-one
            foreach (var run in batch)
            {
                _logElement.Inlines!.Add(run);
            }

            if (batch.Count > 0)
            {
                RequestScroll();
            }
        }, DispatcherPriority.Background);
    }

    private void RequestScroll()
    {
        _needsScroll = true;
        _scrollTimer.Stop();
        _scrollTimer.Start();
    }

    public void AddLogMessage(string message, string foreground = null, string background = null)
    {
        _messageQueue.Enqueue((message, foreground, background));
    }
}