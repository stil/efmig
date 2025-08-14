using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using Efmig.Migrations.Actions;

namespace Efmig.Migrations;

public class SolutionDiscovery
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private ActionContext _context;

    public SolutionDiscovery(ActionContext context = null)
    {
        _context = context;
    }

    public async Task<List<DbContextInfo>> DiscoverDbContextsAsync(string solutionPath)
    {
        _context?.LogInfo($"Starting DbContext discovery for solution: {solutionPath}");
        
        var dbContexts = new List<DbContextInfo>();
        
        if (!File.Exists(solutionPath))
        {
            _context?.LogError($"Solution file not found: {solutionPath}");
            return dbContexts;
        }
        
        var projectPaths = await ParseSolutionFileAsync(solutionPath);
        _context?.LogInfo($"Found {projectPaths.Count} projects in solution");
        
        foreach (var projectPath in projectPaths)
        {
            _context?.LogVerbose($"Scanning project: {Path.GetFileName(projectPath)}");
            var projectContexts = await ScanProjectForDbContextsAsync(projectPath);
            
            if (projectContexts.Any())
            {
                _context?.LogInfo($"Found {projectContexts.Count} DbContext(s) in {Path.GetFileName(projectPath)}:");
                foreach (var ctx in projectContexts)
                {
                    _context?.LogImportant($"  - {ctx.FullName}");
                }
            }
            
            dbContexts.AddRange(projectContexts);
        }
        
        _context?.LogInfo($"Discovery completed. Total DbContexts found: {dbContexts.Count}");
        return dbContexts;
    }

    private async Task<List<string>> ParseSolutionFileAsync(string solutionPath)
    {
        var projectPaths = new List<string>();
        
        try
        {
            var solutionDir = Path.GetDirectoryName(solutionPath);
            _context?.LogVerbose($"Solution directory: {solutionDir}");
            
            var content = await File.ReadAllTextAsync(solutionPath);
            
            var projectRegex = new Regex(@"Project\(""\{[^}]+\}""\)\s*=\s*""[^""]+"",\s*""([^""]+\.csproj)""");
            var matches = projectRegex.Matches(content);
            
            _context?.LogVerbose($"Found {matches.Count} project references in solution file");
            
            foreach (Match match in matches)
            {
                var relativePath = match.Groups[1].Value;
                // Normalize path separators for cross-platform compatibility
                relativePath = relativePath.Replace('\\', Path.DirectorySeparatorChar);
                
                var fullPath = Path.Combine(solutionDir, relativePath);
                var normalizedPath = Path.GetFullPath(fullPath);
                
                _context?.LogVerbose($"  Checking project: {relativePath}");
                
                if (File.Exists(normalizedPath))
                {
                    projectPaths.Add(normalizedPath);
                    _context?.LogVerbose($"    ✓ Found: {normalizedPath}");
                }
                else
                {
                    _context?.LogVerbose($"    ✗ Not found: {normalizedPath}");
                }
            }
            
            if (projectPaths.Count == 0)
            {
                _context?.LogError("No valid project files found in solution");
            }
        }
        catch (Exception ex)
        {
            _context?.LogError($"Error parsing solution file: {ex.Message}");
            Logger.Error(ex, "Error parsing solution file");
        }
        
        return projectPaths;
    }

    private async Task<List<DbContextInfo>> ScanProjectForDbContextsAsync(string projectPath)
    {
        var dbContexts = new List<DbContextInfo>();
        var projectDir = Path.GetDirectoryName(projectPath);
        
        if (!await HasEfCoreReferencesAsync(projectPath))
        {
            _context?.LogVerbose($"  No Entity Framework references found in {Path.GetFileName(projectPath)}");
            return dbContexts;
        }
        
        _context?.LogVerbose($"  Entity Framework detected, scanning source files...");
        var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);
        _context?.LogVerbose($"  Scanning {csFiles.Length} C# files");
        
        foreach (var csFile in csFiles)
        {
            var contexts = await ScanFileForDbContextsAsync(csFile, projectPath);
            dbContexts.AddRange(contexts);
        }
        
        return dbContexts;
    }

    private async Task<bool> HasEfCoreReferencesAsync(string projectPath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(projectPath);
            var hasEfCore = content.Contains("Microsoft.EntityFrameworkCore") || 
                           content.Contains("EntityFramework");
            
            if (hasEfCore)
            {
                _context?.LogVerbose($"    EF Core references detected");
            }
                           
            return hasEfCore;
        }
        catch (Exception ex)
        {
            _context?.LogError($"    Error reading project file: {ex.Message}");
            return false;
        }
    }

    private async Task<List<DbContextInfo>> ScanFileForDbContextsAsync(string filePath, string projectPath)
    {
        var dbContexts = new List<DbContextInfo>();
        
        try
        {
            var content = await File.ReadAllTextAsync(filePath);
            var lines = content.Split('\n');
            
            string currentNamespace = null;
            var usingStatements = new List<string>();
            
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                if (line.StartsWith("using "))
                {
                    usingStatements.Add(line);
                }
                else if (line.StartsWith("namespace "))
                {
                    currentNamespace = line.Replace("namespace ", "").Trim(';', ' ');
                }
                else if ((line.Contains("class ") && (line.Contains(": DbContext") || line.Contains(":DbContext"))) ||
                         (line.Contains("class ") && line.Contains("DbContext") && line.Contains("inherit")))
                {
                    var className = ExtractClassName(line);
                    if (!string.IsNullOrEmpty(className))
                    {
                        var fullName = !string.IsNullOrEmpty(currentNamespace) 
                            ? $"{currentNamespace}.{className}" 
                            : className;
                            
                        _context?.LogVerbose($"      Found DbContext: {fullName} in {Path.GetFileName(filePath)}");
                            
                        dbContexts.Add(new DbContextInfo
                        {
                            Name = className,
                            FullName = fullName,
                            ProjectPath = projectPath,
                            SourceFilePath = filePath,
                            Namespace = currentNamespace
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, $"Could not scan file {filePath} for DbContexts");
        }
        
        return dbContexts;
    }

    private string ExtractClassName(string line)
    {
        // More comprehensive regex to handle various class declaration formats
        var patterns = new[]
        {
            @"(?:public\s+|internal\s+|private\s+|protected\s+)?(?:partial\s+)?class\s+(\w+)",
            @"class\s+(\w+)",
        };
        
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }
        
        return null;
    }
}

public class DbContextInfo
{
    public string Name { get; set; }
    public string FullName { get; set; }
    public string ProjectPath { get; set; }
    public string SourceFilePath { get; set; }
    public string Namespace { get; set; }
}