using Efmig.Core.Utils;

namespace Efmig.Core.Actions;

public class ListMigrationsAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        await ctx.DotNetCli.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "listing migrations",
            DotnetEfArgs = new[]
            {
                "migrations",
                "list"
            },
            DataCallback = null
        });
    }
}