namespace Efmig.Core.Actions;

public class RemoveLastMigrationAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "remove last migration",
            DotnetEfArgs = new[]
            {
                "migrations",
                "remove",
                "--json"
            },
            DataCallback = null
        });
    }
}