namespace Efmig.Core.Actions;

public interface IAction
{
    public Task ExecuteAsync(ActionContext ctx);
}