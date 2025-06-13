using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public interface IMigrationScriptMode;

public class IFullMigrationScriptMode : IMigrationScriptMode;

public class IUnappliedScriptMode : IMigrationScriptMode;

public class IApplyLastMigrationScriptMode : IMigrationScriptMode;

public class IRollbackLastMigrationScriptMode : IMigrationScriptMode;

public class GenerateMigrationScriptAction(IMigrationScriptMode migrationScriptMode) : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        List<MigrationJsonModel> migrations = null;
        try
        {
            if (migrationScriptMode is IApplyLastMigrationScriptMode ||
                migrationScriptMode is IRollbackLastMigrationScriptMode ||
                migrationScriptMode is IUnappliedScriptMode)
            {
                var migrationsJson = new StringBuilder();

                await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
                {
                    ActionName = "detect migrations",
                    DataCallback = line => { migrationsJson.AppendLine(line); },
                    DotnetEfArgs = new[]
                    {
                        "migrations",
                        "list",
                        "--json"
                    }
                });

                migrations = JsonSerializer.Deserialize<List<MigrationJsonModel>>(migrationsJson.ToString());
            }
        }
        catch (Exception e)
        {
            ctx.LogError(e.ToString());
            throw;
        }

        var dotnetEfArgs = new List<string>
        {
            "migrations",
            "script"
        };

        var hasMatchingMigrations = true;
        if (migrationScriptMode is IApplyLastMigrationScriptMode)
        {
            dotnetEfArgs.Add(migrations[^2].name);
            dotnetEfArgs.Add(migrations[^1].name);
        }
        else if (migrationScriptMode is IRollbackLastMigrationScriptMode)
        {
            dotnetEfArgs.Add(migrations[^1].name);
            dotnetEfArgs.Add(migrations[^2].name);
        }
        else if (migrationScriptMode is IUnappliedScriptMode)
        {
            var lastApplied = migrations
                .TakeWhile(m => m.applied == true)
                .Last();

            if (lastApplied != null)
            {
                dotnetEfArgs.Add(lastApplied.name);
            }
            else
            {
                hasMatchingMigrations = false;
            }
        }

        var stringBuilder = new StringBuilder();

        if (hasMatchingMigrations)
        {
            await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
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

        var fileName = "efmig-script-" + DateTimeOffset.Now.ToUnixTimeSeconds() + ".txt";
        await File.WriteAllTextAsync(fileName, stringBuilder.ToString());

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await EditorLauncher.LaunchAsync(EditorLauncher.NotepadEditor, fileName);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            await EditorLauncher.LaunchAsync(EditorLauncher.OpenTextEditEditorMacOS, fileName);
        }
        else
        {
            ctx.LogError("Linux is not supported.");
        }

        await Task.Delay(2000);
        File.Delete(fileName);
    }
}

public class MigrationJsonModel
{
    public string id { get; set; }
    public string name { get; set; }
    public string safeName { get; set; }
    public bool? applied { get; set; }
}