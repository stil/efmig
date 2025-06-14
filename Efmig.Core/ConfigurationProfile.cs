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
}