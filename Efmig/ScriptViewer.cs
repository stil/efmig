using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Efmig.Core.Abstractions;
using Efmig.Migrations;

namespace Efmig;

public class ScriptViewer : IScriptViewer
{
    public async Task OpenScriptPreview(string script)
    {
        var fileName = "efmig-script-" + DateTimeOffset.Now.ToUnixTimeSeconds() + ".txt";
        await File.WriteAllTextAsync(fileName, script);

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
            throw new Exception("Script preview on Linux is not supported.");
        }

        await Task.Delay(2000);
        File.Delete(fileName);
    }
}