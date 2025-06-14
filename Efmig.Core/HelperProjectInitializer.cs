namespace Efmig.Core;

public class HelperProjectInitializer
{
    public static async Task<DirectoryInfo> CreateHelperProject(ConfigurationProfile profile)
    {
        var projectName = $"HelperProject{DateTimeOffset.Now.ToUnixTimeSeconds()}";

        var targetDirectory = new DirectoryInfo(projectName);

        var programCs = $$"""
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Efmig
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
    
    public class DesignTimeFactory : IDesignTimeDbContextFactory<{{profile.DbContextFullName}}>
    {
        public {{profile.DbContextFullName}} CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<{{profile.DbContextFullName}}>();
            {{profile.DbContextConfigCode}}
            return new {{profile.DbContextFullName}}(optionsBuilder.Options);
        }
    }
}
""";

        var csproj = $"""
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>exe</OutputType>
        <TargetFramework>{profile.RuntimeVersion}</TargetFramework>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="{profile.DbContextCsprojPath}"/>
        <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="{profile.EfCoreDesignVersion}">
            <PrivateAssets>all</PrivateAssets>
            <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        </PackageReference>
    </ItemGroup>
</Project>
""";

        var dotnetToolsJson = $$"""
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-ef": {
      "version": "{{profile.DotnetEfVersion}}",
      "commands": [
        "dotnet-ef"
      ]
    }
  }
}
""";

        targetDirectory.Create();

        await File.WriteAllTextAsync(Path.Combine(targetDirectory.FullName, projectName + ".csproj"), csproj);
        await File.WriteAllTextAsync(Path.Combine(targetDirectory.FullName, "Program.cs"), programCs);


        var configSubdir = targetDirectory.CreateSubdirectory(".config");

        await File.WriteAllTextAsync(Path.Combine(configSubdir.FullName, "dotnet-tools.json"), dotnetToolsJson);

        return targetDirectory;
    }
}