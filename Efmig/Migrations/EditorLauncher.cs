using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Efmig.Migrations;

public class EditorInfo
{
    public string Command { get; init; } = null!;
    public bool IsCommandLineApp { get; init; }
    public string Parameters { get; set; } = string.Empty;
}

public class EditorLauncher
{
    public static readonly EditorInfo NotepadEditor = new() { Command = "notepad" };
    public static readonly EditorInfo VisualStudioEditorWindows = new() { Command = "devenv.exe" };
    public static readonly EditorInfo NotepadPlusPlusEditor = new() { Command = "notepad++.exe" };
    public static readonly EditorInfo SublimeTextEditorWindows = new() { Command = "subl.exe" };

    public static readonly EditorInfo NotepadPlusPlusEditorFullPath =
        new() { Command = "C:/Program Files/Notepad++/notepad++.exe" };

    public static readonly EditorInfo SublimeTextEditorWindowsFullPath =
        new() { Command = "C:/Program Files/Sublime Text/sublime_text.exe" };

    public static readonly EditorInfo CodeEditor = new() { Command = "code" };
    public static readonly EditorInfo SublimeTextEditorUnix = new() { Command = "subl" };
    public static readonly EditorInfo NanoEditor = new() { Command = "nano", IsCommandLineApp = true };
    public static readonly EditorInfo VimEditor = new() { Command = "vim", IsCommandLineApp = true };
    public static readonly EditorInfo ViEditor = new() { Command = "vi", IsCommandLineApp = true };

    public static readonly EditorInfo OpenDefaultEditorMacOS =
        new() { Command = "open", IsCommandLineApp = true, Parameters = @"-t" };

    public static readonly EditorInfo OpenTextEditEditorMacOS =
        new() { Command = "open", IsCommandLineApp = true, Parameters = @"-e" };

    private static readonly ReadOnlyDictionary<string, EditorInfo> WindowsEditors =
        new(
            new Dictionary<string, EditorInfo>
            {
                [NotepadEditor.Command] = NotepadEditor,
                [CodeEditor.Command] = CodeEditor,
                [NotepadPlusPlusEditor.Command] = NotepadPlusPlusEditor,
                [SublimeTextEditorWindows.Command] = SublimeTextEditorWindows,
                [NotepadPlusPlusEditorFullPath.Command] = NotepadPlusPlusEditor,
                [SublimeTextEditorWindowsFullPath.Command] = SublimeTextEditorWindows,
                [VisualStudioEditorWindows.Command] = VisualStudioEditorWindows,
                [VimEditor.Command] = VimEditor
            });

    private static readonly ReadOnlyDictionary<string, EditorInfo> LinuxEditors =
        new(
            new Dictionary<string, EditorInfo>
            {
                [nameof(NanoEditor)] = NanoEditor,
                [nameof(VimEditor)] = VimEditor,
                [nameof(ViEditor)] = ViEditor,
                [nameof(CodeEditor)] = CodeEditor,
                [nameof(SublimeTextEditorUnix)] = SublimeTextEditorUnix
            });

    private static readonly ReadOnlyDictionary<string, EditorInfo> MacOSEditors =
        new(
            new Dictionary<string, EditorInfo>
            {
                [nameof(OpenDefaultEditorMacOS)] = OpenDefaultEditorMacOS,
                [nameof(OpenTextEditEditorMacOS)] = OpenTextEditEditorMacOS,
                [nameof(NanoEditor)] = NanoEditor,
                [nameof(VimEditor)] = VimEditor,
                [nameof(ViEditor)] = ViEditor,
                [nameof(CodeEditor)] = CodeEditor,
                [nameof(SublimeTextEditorUnix)] = SublimeTextEditorUnix
            });

    private static async Task OpenProcessAsync(EditorInfo editor, string textFilename, bool verbose)
    {
        if (!string.IsNullOrWhiteSpace(editor.Command))
        {
            if (verbose)
            {
                Console.WriteLine(
                    $@"Opening ""{textFilename}"" with ""{editor.Command}"" {(editor.IsCommandLineApp ? "command line " : string.Empty)}editor.");
            }

            var process = new Process();
            process.StartInfo.UseShellExecute = true;
            process.StartInfo.FileName = editor.Command;
            process.StartInfo.Arguments = @$"{editor.Parameters} ""{textFilename}""";
            process.Start();
            if (editor.IsCommandLineApp)
            {
                await process.WaitForExitAsync();
            }

            return;
        }

        Console.WriteLine(@"Invalid command.");
    }

    /// <summary>
    ///     Launches the specified editor. Using Process class, to start new process of PowerShell(pwsh) with the script as
    ///     parameter and the script required parameters. If editor specified is not found, it try to launch the default.
    /// </summary>
    /// <param name="textFilename">Path of text file.</param>
    /// <param name="editor">The editor command.</param>
    /// <param name="tryVsCode">If Visual Studio Code is available. The file is opened with VSCode. Editor value is ignored.</param>
    /// <param name="verbose">Prints parameters.</param>
    public static async Task LaunchAsync(EditorInfo editor, string textFilename, bool tryVsCode = false,
        bool verbose = false)
    {
        string command;
        if (tryVsCode)
        {
            command = CommandExists(CodeEditor.Command);
            if (!string.IsNullOrWhiteSpace(command))
            {
                await OpenProcessAsync(CodeEditor, textFilename, verbose);
                return;
            }
        }

        command = CommandExists(editor.Command);
        if (!string.IsNullOrWhiteSpace(command))
        {
            await OpenProcessAsync(editor, textFilename, verbose);
            return;
        }

        var message = @$"No editor was found for file ""{textFilename}"".";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var cmd in WindowsEditors)
            {
                command = CommandExists(cmd.Value.Command);
                if (!string.IsNullOrWhiteSpace(command))
                {
                    await OpenProcessAsync(cmd.Value, textFilename, verbose);
                    return;
                }
            }

            Console.WriteLine(message);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            foreach (var cmd in LinuxEditors)
            {
                command = CommandExists(cmd.Value.Command);
                if (!string.IsNullOrWhiteSpace(command))
                {
                    await OpenProcessAsync(cmd.Value, textFilename, verbose);
                    return;
                }
            }

            Console.WriteLine(message);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            foreach (var cmd in MacOSEditors)
            {
                command = CommandExists(cmd.Value.Command);
                if (!string.IsNullOrWhiteSpace(command))
                {
                    await OpenProcessAsync(cmd.Value, textFilename, verbose);
                    return;
                }
            }

            Console.WriteLine(message);
        }

        throw new NotSupportedException(RuntimeInformation.OSDescription);
    }


    private static string CommandExists(string filename)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var paths = new[] { Environment.CurrentDirectory }
                .Concat(Environment.GetEnvironmentVariable("PATH")!.Split(';'))
                .Where(p => !string.IsNullOrWhiteSpace(p));

            var extensions = new[] { string.Empty }
                .Concat(Environment.GetEnvironmentVariable("PATHEXT")!.Split(';')
                    .Where(e => e.StartsWith(".")));

            var combinations = paths.SelectMany(x => extensions,
                (path, extension) => Path.Combine(path, filename + extension));
            return combinations.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var paths = new[] { Environment.CurrentDirectory }
                .Concat(Environment.GetEnvironmentVariable("PATH")!.Split(':'))
                .Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => Path.Combine(p, filename));
            return paths.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var paths = new[] { Environment.CurrentDirectory }
                .Concat(Environment.GetEnvironmentVariable("PATH")!.Split(':'))
                .Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => Path.Combine(p, filename));
            return paths.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        throw new NotSupportedException(RuntimeInformation.OSDescription);
    }
}