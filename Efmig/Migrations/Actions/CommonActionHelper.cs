using System;
using System.Threading.Tasks;
using CliWrap;

namespace Efmig.Migrations.Actions;

public static class CommonActionHelper
{
    public static async Task RunDotnetEfTool(ActionContext ctx, string[] dotnetEfArgs)
    {
        ctx.ClearLog();
        ctx.LogInfo("Started operation: listing migrations.\r\n");

        var targetDir = await HelperProjectInitializer.CreateHelperProject(ctx.ConfigurationProfile);

        try
        {
            ctx.LogInfo("Running 'dotnet restore'...");

            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments("restore")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(ctx.LogInfo))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogError))
                .ExecuteAsync();

            ctx.LogInfo("Running 'dotnet tool restore'...");
            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments("tool restore")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(ctx.LogInfo))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogError))
                .ExecuteAsync();

            ctx.LogInfo("Running 'dotnet build'...");
            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments("build")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(ctx.LogInfo))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogError))
                .ExecuteAsync();


            ctx.LogInfo("Running dotnet-ef command...");
            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments(args =>
                {
                    args.Add("tool");
                    args.Add("run");
                    args.Add("dotnet-ef");

                    args.Add(dotnetEfArgs);

                    args.Add("-v");
                    args.Add(new[] { "--context", ctx.ConfigurationProfile.DbContextFullName });
                    args.Add(new[] { "--project", ctx.ConfigurationProfile.DbContextCsprojPath });
                    args.Add(new[] { "--startup-project", targetDir.FullName });
                    args.Add("--prefix-output");
                    args.Add("--no-build");
                })
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                {
                    var markerLength = 9;
                    var errorMarker = "error:".PadRight(markerLength, ' ');
                    var verboseMarker = "verbose:".PadRight(markerLength, ' ');
                    var infoMarker = "info:".PadRight(markerLength, ' ');

                    if (line.StartsWith(errorMarker))
                    {
                        ctx.LogError(line[markerLength..]);
                    }
                    else if (line.StartsWith(verboseMarker))
                    {
                        ctx.LogVerbose(line[markerLength..]);
                    }
                    else if (line.StartsWith(infoMarker))
                    {
                        ctx.LogImportant(line[markerLength..]);
                    }
                    else
                    {
                        ctx.LogInfo(line);
                    }
                }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogError))
                .ExecuteAsync();

            targetDir.Delete(true);
        }
        catch (Exception e)
        {
            ctx.LogError(e.ToString());
        }

        ctx.LogInfo("Finished operation.");
    }
}