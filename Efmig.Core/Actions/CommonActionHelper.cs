using CliWrap;

namespace Efmig.Core.Actions;

public class CommonActionOptions
{
    public required string ActionName { get; set; }
    public required string[] DotnetEfArgs { get; set; }
    public required Action<string>? DataCallback { get; set; }
}

public static class CommonActionHelper
{
    public static async Task RunDotnetEfTool(ActionContext ctx, CommonActionOptions options)
    {
        ctx.LogOutput.LogInfo($"Started operation: {options.ActionName}.\r\n");

        if (string.IsNullOrWhiteSpace(ctx.ConfigurationProfile.DbContextFullName))
        {
            throw new Exception("DbContext full name cannot be null or empty.");
        }

        if (string.IsNullOrWhiteSpace(ctx.ConfigurationProfile.DbContextCsprojPath))
        {
            throw new Exception("DbContext .csproj path cannot be null or empty.");
        }

        var dbContextFullName = ctx.ConfigurationProfile.DbContextFullName;
        var dbContextCsprojPath = ctx.ConfigurationProfile.DbContextCsprojPath;

        var targetDir = await HelperProjectInitializer.CreateHelperProject(ctx.ConfigurationProfile);

        try
        {
            ctx.LogOutput.LogInfo("Running 'dotnet restore'...");

            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments("restore")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(ctx.LogOutput.LogInfo))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogOutput.LogError))
                .ExecuteAsync();

            ctx.LogOutput.LogInfo("Running 'dotnet tool restore'...");
            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments("tool restore")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(ctx.LogOutput.LogInfo))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogOutput.LogError))
                .ExecuteAsync();

            ctx.LogOutput.LogInfo("Running 'dotnet build'...");
            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments("build")
                .WithStandardOutputPipe(PipeTarget.ToDelegate(ctx.LogOutput.LogInfo))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogOutput.LogError))
                .ExecuteAsync();


            ctx.LogOutput.LogInfo("Running dotnet-ef command...");
            await Cli.Wrap("dotnet")
                .WithWorkingDirectory(targetDir.FullName)
                .WithArguments(args =>
                {
                    args.Add("tool");
                    args.Add("run");
                    args.Add("dotnet-ef");

                    args.Add(options.DotnetEfArgs);

                    args.Add("-v");
                    args.Add(["--context", dbContextFullName]);
                    args.Add(["--project", dbContextCsprojPath]);
                    args.Add(["--startup-project", targetDir.FullName]);
                    args.Add("--prefix-output");
                    args.Add("--no-build");
                })
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
                {
                    var markerLength = 9;
                    var errorMarker = "error:".PadRight(markerLength, ' ');
                    var verboseMarker = "verbose:".PadRight(markerLength, ' ');
                    var infoMarker = "info:".PadRight(markerLength, ' ');
                    var dataMarker = "data:".PadRight(markerLength, ' ');

                    if (line.StartsWith(errorMarker))
                    {
                        ctx.LogOutput.LogError(line[markerLength..]);
                    }
                    else if (line.StartsWith(dataMarker))
                    {
                        if (options.DataCallback != null)
                        {
                            options.DataCallback(line[markerLength..]);
                        }
                        else
                        {
                            ctx.LogOutput.LogData(line[markerLength..]);
                        }
                    }
                    else if (line.StartsWith(verboseMarker))
                    {
                        ctx.LogOutput.LogVerbose(line[markerLength..]);
                    }
                    else if (line.StartsWith(infoMarker))
                    {
                        ctx.LogOutput.LogImportant(line[markerLength..]);
                    }
                    else
                    {
                        ctx.LogOutput.LogInfo(line);
                    }
                }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(ctx.LogOutput.LogError))
                .ExecuteAsync();

            targetDir.Delete(true);
        }
        catch (Exception e)
        {
            ctx.LogOutput.LogError(e.ToString());
        }

        ctx.LogOutput.LogInfo("\r\nFinished operation.");
        ctx.LogOutput.ScrollToEnd();
    }
}