using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Efmig.Migrations.Actions;
using NLog;

namespace Efmig.Migrations;

public class NuGetVersionService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private static readonly HttpClient HttpClient = new HttpClient();
    private readonly ActionContext _context;

    public NuGetVersionService(ActionContext context = null)
    {
        _context = context;
    }

    public async Task<List<string>> GetPackageVersionsAsync(string packageName)
    {
        try
        {
            _context?.LogVerbose($"Fetching NuGet versions for {packageName}...");
            
            var url = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLowerInvariant()}/index.json";
            var response = await HttpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _context?.LogError($"Failed to fetch NuGet versions: {response.StatusCode}");
                return GetFallbackVersions();
            }

            var jsonBytes = await response.Content.ReadAsByteArrayAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var versionsResponse = JsonSerializer.Deserialize<NuGetVersionsResponse>(jsonBytes, options);

            if (versionsResponse?.Versions == null)
            {
                _context?.LogError("Invalid response format from NuGet API");
                return GetFallbackVersions();
            }

            // Filter and sort versions (newest first)
            var filteredVersions = versionsResponse.Versions
                .Where(v => IsValidVersion(v))
                .OrderByDescending(v => ParseVersion(v))
                .ToList();

            _context?.LogVerbose($"Found {filteredVersions.Count} versions for {packageName}");
            return filteredVersions;
        }
        catch (Exception ex)
        {
            _context?.LogError($"Error fetching NuGet versions: {ex.Message}");
            Logger.Error(ex, $"Error fetching NuGet versions for {packageName}");
            return GetFallbackVersions();
        }
    }

    public async Task<EfCoreVersions> GetEfCoreVersionsAsync()
    {
        _context?.LogVerbose("Fetching EF Core package versions...");
        
        var coreVersions = await GetPackageVersionsAsync("Microsoft.EntityFrameworkCore");
        var toolVersions = await GetPackageVersionsAsync("Microsoft.EntityFrameworkCore.Tools");
        var designVersions = await GetPackageVersionsAsync("Microsoft.EntityFrameworkCore.Design");

        _context?.LogVerbose($"Retrieved versions - Core: {coreVersions.Count}, Tools: {toolVersions.Count}, Design: {designVersions.Count}");

        return new EfCoreVersions
        {
            CoreVersions = coreVersions,
            ToolVersions = toolVersions,
            DesignVersions = designVersions
        };
    }

    public string FindBestMatchVersion(List<string> availableVersions, string detectedVersion)
    {
        if (string.IsNullOrEmpty(detectedVersion))
            return availableVersions.FirstOrDefault();

        // Try exact match first
        var exactMatch = availableVersions.FirstOrDefault(v => 
            string.Equals(v, detectedVersion, StringComparison.OrdinalIgnoreCase));
        
        if (exactMatch != null)
            return exactMatch;

        // Try major.minor match
        var detectedParsed = ParseVersion(detectedVersion);
        if (detectedParsed != null)
        {
            var majorMinorMatch = availableVersions.FirstOrDefault(v =>
            {
                var parsed = ParseVersion(v);
                return parsed != null && 
                       parsed.Major == detectedParsed.Major && 
                       parsed.Minor == detectedParsed.Minor;
            });

            if (majorMinorMatch != null)
                return majorMinorMatch;
        }

        // Return the latest version as fallback
        return availableVersions.FirstOrDefault();
    }

    private bool IsValidVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return false;
            
        // Only exclude obvious preview/beta versions, but keep stable versions
        var lowerVersion = version.ToLowerInvariant();
        if (lowerVersion.Contains("-preview") || 
            lowerVersion.Contains("-beta") || 
            lowerVersion.Contains("-alpha") ||
            lowerVersion.Contains("-rc"))
        {
            return false;
        }
        
        // Accept versions that can be parsed as valid versions
        var parsed = ParseVersion(version);
        return parsed != null;
    }

    private Version ParseVersion(string versionString)
    {
        try
        {
            // Handle semantic versioning (e.g., "1.0.0-preview" -> "1.0.0")
            var cleanVersion = versionString.Split('-')[0];
            return Version.Parse(cleanVersion);
        }
        catch
        {
            return null;
        }
    }

    private List<string> GetFallbackVersions()
    {
        return new List<string>
        {
            "6.6.6"
        };
    }
}

public class NuGetVersionsResponse
{
    public string[] Versions { get; set; }
}

public class EfCoreVersions
{
    public List<string> CoreVersions { get; set; } = new();
    public List<string> ToolVersions { get; set; } = new();
    public List<string> DesignVersions { get; set; } = new();
}