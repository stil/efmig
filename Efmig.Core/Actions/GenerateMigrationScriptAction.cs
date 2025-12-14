using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Efmig.Core.Utils;

namespace Efmig.Core.Actions;

public interface IMigrationScriptMode;

public class IFullMigrationScriptMode : IMigrationScriptMode;

public class IUnappliedScriptMode : IMigrationScriptMode;

public class IApplyLastMigrationScriptMode : IMigrationScriptMode;

public class IRollbackLastMigrationScriptMode : IMigrationScriptMode;

public class GenerateMigrationScriptAction(IMigrationScriptMode migrationScriptMode) : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        List<MigrationJsonModel>? migrations = null;
        try
        {
            if (migrationScriptMode is IApplyLastMigrationScriptMode ||
                migrationScriptMode is IRollbackLastMigrationScriptMode ||
                migrationScriptMode is IUnappliedScriptMode)
            {
                var migrationsJson = new StringBuilder();

                await ctx.DotNetEfTool.RunDotnetEfTool(ctx, new CommonActionOptions
                {
                    ActionName = "detect migrations",
                    DataCallback = line => { migrationsJson.AppendLine(line); },
                    DotnetEfArgs =
                    [
                        "migrations",
                        "list",
                        "--json"
                    ]
                });

                migrations = JsonSerializer.Deserialize<List<MigrationJsonModel>>(migrationsJson.ToString());
            }
        }
        catch (Exception e)
        {
            ctx.LogOutput.LogError(e.ToString());
            throw;
        }

        var dotnetEfArgs = new List<string>
        {
            "migrations",
            "script"
        };

        var hasMatchingMigrations = true;
        const string beforeTheFirstMigration = "0";

        if (migrationScriptMode is IApplyLastMigrationScriptMode)
        {
            if (migrations == null)
            {
                throw new Exception("Migration list was not initialized.");
            }

            if (migrations.Count > 0)
            {
                dotnetEfArgs.Add(migrations.Count == 1 ? beforeTheFirstMigration : migrations[^2].name);
                dotnetEfArgs.Add(migrations[^1].name);
            }
            else
            {
                hasMatchingMigrations = false;
            }
        }
        else if (migrationScriptMode is IRollbackLastMigrationScriptMode)
        {
            if (migrations == null)
            {
                throw new Exception("Migration list was not initialized.");
            }

            if (migrations.Count > 0)
            {
                dotnetEfArgs.Add(migrations[^1].name);
                dotnetEfArgs.Add(migrations.Count == 1 ? beforeTheFirstMigration : migrations[^2].name);
            }
        }
        else if (migrationScriptMode is IUnappliedScriptMode)
        {
            if (migrations == null)
            {
                throw new Exception("Migration list was not initialized.");
            }

            if (migrations.Count > 0)
            {
                var firstUnappliedIndex = migrations.FindIndex(m => m.applied == false);

                if (firstUnappliedIndex == -1)
                {
                    // No unapplied migrations found.
                    hasMatchingMigrations = false;
                }
                else if (firstUnappliedIndex == 0)
                {
                    dotnetEfArgs.Add(beforeTheFirstMigration);
                }
                else
                {
                    dotnetEfArgs.Add(migrations[firstUnappliedIndex - 1].name);
                }
            }
            else
            {
                hasMatchingMigrations = false;
            }
        }


        var stringBuilder = new StringBuilder();

        if (hasMatchingMigrations)
        {
            await ctx.DotNetEfTool.RunDotnetEfTool(ctx, new CommonActionOptions
            {
                ActionName = "generate migration script",
                DataCallback = line => { stringBuilder.AppendLine(line); },
                DotnetEfArgs = dotnetEfArgs.ToArray()
            });
        }
        else
        {
            stringBuilder.AppendLine("-- No matching migrations.");
        }

        await ctx.ScriptViewer.OpenScriptPreview(stringBuilder.ToString());
    }
}

public class MigrationJsonModel
{
    public required string id { get; set; }
    public required string name { get; set; }
    public required string safeName { get; set; }
    public bool? applied { get; set; }
}