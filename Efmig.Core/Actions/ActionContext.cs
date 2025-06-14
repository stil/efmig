using Efmig.Core.Abstractions;

namespace Efmig.Core.Actions;

public class ActionContext(ILogOutput logOutput, IScriptViewer scriptViewer, ConfigurationProfile profile)
{
    public ILogOutput LogOutput { get; } = logOutput;
    public IScriptViewer ScriptViewer { get; } = scriptViewer;
    public ConfigurationProfile ConfigurationProfile { get; } = profile;
    public object? Data { get; set; }
}