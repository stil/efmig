using Efmig.Core.Utils;

namespace Efmig.Core.Actions;

public class RemoveLastMigrationAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        await ctx.DotNetEfTool.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "remove last migration",
            DotnetEfArgs =
            [
                "migrations",
                "remove",
                "--json"
            ],
            DataCallback = null
        });
    }
}