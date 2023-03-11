using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Efmig.Migrations.Actions;

public class GenerateMigrationScriptAction : IAction
{
    public async Task ExecuteAsync(ActionContext ctx)
    {
        var stringBuilder = new StringBuilder();

        await CommonActionHelper.RunDotnetEfTool(ctx, new CommonActionOptions
        {
            ActionName = "remove last migration",
            DataCallback = line => { stringBuilder.AppendLine(line); },
            DotnetEfArgs = new[]
            {
                "migrations",
                "script"
            }
        });

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