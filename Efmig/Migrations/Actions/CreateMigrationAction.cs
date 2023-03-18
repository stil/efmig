using System.Collections.Generic;
using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public class CreateMigrationAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        var migrationName = (string)ctx.Data;

        var args = new List<string>
        {
            "migrations",
            "add",
            migrationName,
            "--json"
        };

        if (!string.IsNullOrWhiteSpace(ctx.ConfigurationProfile.MigrationsDir))
        {
            args.Add("--output-dir");
            args.Add(ctx.ConfigurationProfile.MigrationsDir);
        }

        await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = $"creating migration '{migrationName}'",
            DotnetEfArgs = args.ToArray()
        });
    }
}