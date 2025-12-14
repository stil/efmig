using Efmig.Core.Utils;

namespace Efmig.Core.Actions;

public class CreateMigrationAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx.Data);
        
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

        await ctx.DotNetEfTool.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = $"creating migration '{migrationName}'",
            DotnetEfArgs = args.ToArray(),
            DataCallback = null
        });
    }
}