using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public class CreateMigrationAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        var migrationName = (string)ctx.Data;

        await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = $"creating migration '{migrationName}'",
            DotnetEfArgs = new[]
            {
                "migrations",
                "add",
                migrationName,
                "--json"
            }
        });
    }
}