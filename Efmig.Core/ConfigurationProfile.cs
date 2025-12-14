namespace Efmig.Core;

public class ConfigurationProfile
{
    public string? Name { get; set; }
    public string? DotnetEfVersion { get; set; }
    public string? EfCoreDesignVersion { get; set; }
    public string? RuntimeVersion { get; set; }
    public string? DbContextCsprojPath { get; set; }
    public string? DbContextFullName { get; set; }
    public string? DbContextConfigCode { get; set; }
    public string? MigrationsDir { get; set; }

    // Visual Builder properties
    public bool UseVisualBuilder { get; set; }
    public string? DatabaseProvider { get; set; }
    public bool UseConnectionStringDirectly { get; set; }
    public string? ConnectionString { get; set; }
    public string? Server { get; set; }
    public int? Port { get; set; }
    public string? Database { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}