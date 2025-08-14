using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Efmig.Migrations.Actions;
using System;

namespace Efmig.Migrations;

public class ProfileGenerator
{
    private ActionContext _context;
    private NuGetVersionService _nugetService;

    public ProfileGenerator(ActionContext context = null)
    {
        _context = context;
        _nugetService = new NuGetVersionService(context);
    }

    public async Task<List<ConfigurationProfile>> GenerateProfilesAsync(List<DbContextInfo> dbContexts)
    {
        var profiles = new List<ConfigurationProfile>();
        
        foreach (var context in dbContexts)
        {
            _context?.LogVerbose($"Generating profile for {context.FullName}");
            
            var runtimeVersion = await DetectRuntimeVersionAsync(context.ProjectPath);
            var efVersions = await DetectEfCoreVersionsAsync(context.ProjectPath);
            var projectName = Path.GetFileNameWithoutExtension(context.ProjectPath);
            
            var profile = new ConfigurationProfile
            {
                Name = $"{context.Name} ({projectName})",
                DotnetEfVersion = efVersions.DotnetEfVersion,
                EfCoreDesignVersion = efVersions.EfCoreDesignVersion, 
                RuntimeVersion = runtimeVersion,
                DbContextCsprojPath = context.ProjectPath,
                DbContextFullName = context.FullName,
                DbContextConfigCode = "optionsBuilder.UseNpgsql(\"\")",
                MigrationsDir = Path.Combine(Path.GetDirectoryName(context.ProjectPath), "Migrations")
            };
            
            _context?.LogVerbose($"  Profile: {profile.Name}");
            _context?.LogVerbose($"  Runtime: {profile.RuntimeVersion}");
            _context?.LogVerbose($"  EF Core: {profile.EfCoreDesignVersion}");
            _context?.LogVerbose($"  dotnet-ef: {profile.DotnetEfVersion}");
            _context?.LogVerbose($"  Project: {profile.DbContextCsprojPath}");
            
            profiles.Add(profile);
        }
        
        return profiles;
    }

    private async Task<EfVersions> DetectEfCoreVersionsAsync(string projectPath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(projectPath);
            
            // Look for EF Core package references
            var efCoreVersion = ExtractEfCoreVersion(content);
            
            if (efCoreVersion != null)
            {
                _context?.LogVerbose($"    Detected EF Core version: {efCoreVersion}");
                
                // Try to get actual NuGet versions for better matching
                try
                {
                    _context?.LogVerbose($"    Fetching NuGet versions for validation...");
                    var nugetVersions = await _nugetService.GetEfCoreVersionsAsync();
                    
                    var toolVersion = _nugetService.FindBestMatchVersion(nugetVersions.ToolVersions, efCoreVersion);
                    var designVersion = _nugetService.FindBestMatchVersion(nugetVersions.DesignVersions, efCoreVersion);
                    
                    _context?.LogVerbose($"    Best match - Tool: {toolVersion}, Design: {designVersion}");
                    
                    return new EfVersions 
                    { 
                        DotnetEfVersion = toolVersion ?? efCoreVersion, 
                        EfCoreDesignVersion = designVersion ?? efCoreVersion 
                    };
                }
                catch (Exception ex)
                {
                    _context?.LogVerbose($"    NuGet lookup failed, using local mapping: {ex.Message}");
                    var mapped = MapEfCoreVersionToToolVersions(efCoreVersion);
                    return mapped;
                }
            }
            else
            {
                _context?.LogVerbose($"    No EF Core version detected, using defaults");
                return new EfVersions { DotnetEfVersion = "8.0.0", EfCoreDesignVersion = "8.0.0" };
            }
        }
        catch (Exception ex)
        {
            _context?.LogVerbose($"    Error detecting EF Core version: {ex.Message}");
            return new EfVersions { DotnetEfVersion = "8.0.0", EfCoreDesignVersion = "8.0.0" };
        }
    }

    private string ExtractEfCoreVersion(string projectContent)
    {
        // Look for EF Core package references in order of preference
        var patterns = new[]
        {
            @"<PackageReference\s+Include=""Microsoft\.EntityFrameworkCore\.SqlServer""\s+Version=""([^""]+)""",
            @"<PackageReference\s+Include=""Microsoft\.EntityFrameworkCore\.Tools""\s+Version=""([^""]+)""",
            @"<PackageReference\s+Include=""Microsoft\.EntityFrameworkCore\.Design""\s+Version=""([^""]+)""",
            @"<PackageReference\s+Include=""Microsoft\.EntityFrameworkCore""\s+Version=""([^""]+)""",
            @"<PackageReference\s+Include=""Microsoft\.EntityFrameworkCore\..*""\s+Version=""([^""]+)""",
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(projectContent, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private EfVersions MapEfCoreVersionToToolVersions(string efCoreVersion)
    {
        // Parse version to get major.minor
        var versionMatch = Regex.Match(efCoreVersion, @"(\d+)\.(\d+)");
        if (!versionMatch.Success)
        {
            return new EfVersions { DotnetEfVersion = "8.0.0", EfCoreDesignVersion = "8.0.0" };
        }

        var major = int.Parse(versionMatch.Groups[1].Value);
        var minor = int.Parse(versionMatch.Groups[2].Value);

        // Map EF Core versions to dotnet-ef tool versions
        // Rule: tool version should match the major.minor of EF Core
        return (major, minor) switch
        {
            (9, 0) => new EfVersions { DotnetEfVersion = "9.0.0", EfCoreDesignVersion = "9.0.0" },
            (8, 0) => new EfVersions { DotnetEfVersion = "8.0.0", EfCoreDesignVersion = "8.0.0" },
            (7, 0) => new EfVersions { DotnetEfVersion = "7.0.0", EfCoreDesignVersion = "7.0.0" },
            (6, 0) => new EfVersions { DotnetEfVersion = "6.0.0", EfCoreDesignVersion = "6.0.0" },
            (5, 0) => new EfVersions { DotnetEfVersion = "5.0.0", EfCoreDesignVersion = "5.0.0" },
            (3, 1) => new EfVersions { DotnetEfVersion = "3.1.0", EfCoreDesignVersion = "3.1.0" },
            (3, 0) => new EfVersions { DotnetEfVersion = "3.0.0", EfCoreDesignVersion = "3.0.0" },
            (2, 2) => new EfVersions { DotnetEfVersion = "2.2.0", EfCoreDesignVersion = "2.2.0" },
            (2, 1) => new EfVersions { DotnetEfVersion = "2.1.0", EfCoreDesignVersion = "2.1.0" },
            (2, 0) => new EfVersions { DotnetEfVersion = "2.0.0", EfCoreDesignVersion = "2.0.0" },
            (1, 1) => new EfVersions { DotnetEfVersion = "1.1.0", EfCoreDesignVersion = "1.1.0" },
            (1, 0) => new EfVersions { DotnetEfVersion = "1.0.0", EfCoreDesignVersion = "1.0.0" },
            _ => new EfVersions { DotnetEfVersion = efCoreVersion, EfCoreDesignVersion = efCoreVersion }
        };
    }
    
    private async Task<string> DetectRuntimeVersionAsync(string projectPath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(projectPath);
            
            if (content.Contains("<TargetFramework>net8.0</TargetFramework>"))
                return "net8.0";
            if (content.Contains("<TargetFramework>net7.0</TargetFramework>"))
                return "net7.0";
            if (content.Contains("<TargetFramework>net6.0</TargetFramework>"))
                return "net6.0";
                
            return "net8.0";
        }
        catch
        {
            return "net8.0";
        }
    }
}

public class EfVersions
{
    public string DotnetEfVersion { get; set; }
    public string EfCoreDesignVersion { get; set; }
}