using System.Text;
using Efmig.Core.Utils;

namespace Efmig.Core.Actions;

public class OptimizeAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        var stringBuilder = new StringBuilder();

        await ctx.DotNetEfTool.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "optmize dbcontext",
            DataCallback = line => { stringBuilder.AppendLine(line); },
            DotnetEfArgs =
            [
                "dbcontext",
                "optimize"
            ]
        });

    }
}