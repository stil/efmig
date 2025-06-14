using System.Text;

namespace Efmig.Core.Actions;

public class OptimizeAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        var stringBuilder = new StringBuilder();

        await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "optmize dbcontext",
            DataCallback = line => { stringBuilder.AppendLine(line); },
            DotnetEfArgs = new[]
            {
                "dbcontext",
                "optimize"
            }
        });

    }
}