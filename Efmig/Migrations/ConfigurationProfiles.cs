using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Efmig.Migrations;

public static class ProfilesManager
{
    private static string ConfigurationFilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "efmig.json");

    public static async Task SaveProfiles(List<ConfigurationProfile> profiles)
    {
        await File.WriteAllTextAsync(ConfigurationFilePath, JsonSerializer.Serialize(new ConfigurationRoot
        {
            Profiles = profiles
        }, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    public static async Task<List<ConfigurationProfile>> LoadProfiles()
    {
        try
        {
            var json = await File.ReadAllTextAsync(ConfigurationFilePath);
            var configurationRoot = JsonSerializer.Deserialize<ConfigurationRoot>(json);
            return configurationRoot.Profiles;
        }
        catch (Exception e)
        {
            // Possibly file missing.
            return new List<ConfigurationProfile>();
        }
    }
}

public class ConfigurationRoot
{
    public List<ConfigurationProfile> Profiles { get; set; }
}

public class ConfigurationProfile
{
    public string Name { get; set; }
    public string DotnetEfVersion { get; set; }
    public string EfCoreDesignVersion { get; set; }
    public string RuntimeVersion { get; set; }
    public string DbContextCsprojPath { get; set; }
    public string DbContextFullName { get; set; }
    public string DbContextConfigCode { get; set; }
}