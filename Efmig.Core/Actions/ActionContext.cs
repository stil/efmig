using Efmig.Core.Abstractions;

namespace Efmig.Core.Actions;

public class ActionContext(
    ILogOutput logOutput,
    IScriptViewer scriptViewer,
    IDotNetEfTool dotNetEfTool,
    ConfigurationProfile profile)
{
    public ILogOutput LogOutput { get; } = logOutput;
    public IScriptViewer ScriptViewer { get; } = scriptViewer;
    public IDotNetEfTool DotNetEfTool { get; } = dotNetEfTool;
    public ConfigurationProfile ConfigurationProfile { get; } = profile;
    public object? Data { get; set; }
}