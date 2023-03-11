using System;
using System.Threading.Tasks;
using CliWrap;

namespace Efmig.Migrations.Actions;

public class ListMigrationsAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        await CommonActionHelper.RunDotnetEfTool(ctx, new []
        {
            "migrations",
            "list"
        });
    }
}