using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Efmig.Core;
using NLog;

namespace Efmig.Migrations;

public static class ProfilesManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
            Logger.Warn(e, "Could not load profiles.");

            // Possibly file missing.
            return new List<ConfigurationProfile>();
        }
    }
}

public class ConfigurationRoot
{
    public List<ConfigurationProfile> Profiles { get; set; }
}