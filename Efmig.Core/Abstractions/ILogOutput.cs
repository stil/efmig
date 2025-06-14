namespace Efmig.Core.Abstractions;

public interface ILogOutput
{
    void ClearLog();
    void ScrollToEnd();
    void AddLogMessage(string message, string? foreground = null, string? background = null);
    void LogInfo(string message);
    void LogError(string message);
    void LogVerbose(string message);
    void LogImportant(string message);
    void LogData(string message);
}