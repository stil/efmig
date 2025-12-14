using System;
using System.Collections.Generic;
using System.Windows.Input;
using Efmig.Core;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Efmig.ViewModels;

public class ProfileSetupWindowViewModel : ViewModelBase
{
    [Reactive] public string WindowTitle { get; set; }
    [Reactive] public string ProfileName { get; set; }
    [Reactive] public string DotnetEfVersionSelected { get; set; }
    [Reactive] public string EfCoreDesignVersionSelected { get; set; }
    [Reactive] public string RuntimeVersionSelected { get; set; }
    [Reactive] public string DbContextCsprojPath { get; set; }
    [Reactive] public ICommand DbContextCsprojSelect { get; set; }
    [Reactive] public string DbContextFullName { get; set; }
    [Reactive] public string DbContextConfigCode { get; set; }
    [Reactive] public string MigrationsDir { get; set; }
    [Reactive] public ICommand Save { get; set; }
    [Reactive] public DeleteProfileViewModel DeleteProfile { get; set; }

    // Visual Builder properties
    public List<string> DatabaseProviders { get; } = new() { "PostgreSQL", "MySQL", "SqlServer", "SQLite" };
    [Reactive] public bool UseVisualBuilder { get; set; } = true;
    [Reactive] public string SelectedDatabaseProvider { get; set; } = "PostgreSQL";
    [Reactive] public bool UseConnectionStringDirectly { get; set; }
    [Reactive] public string ConnectionString { get; set; }
    [Reactive] public string Server { get; set; }
    [Reactive] public string Port { get; set; }
    [Reactive] public string Database { get; set; }
    [Reactive] public string Username { get; set; }
    [Reactive] public string Password { get; set; }

    public ProfileSetupWindowViewModel()
    {
        // React to changes in Visual Builder fields to update GeneratedCode
        this.WhenAnyValue(x => x.UseVisualBuilder)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.SelectedDatabaseProvider)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.UseConnectionStringDirectly)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.ConnectionString)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.Server)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.Port)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.Database)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.Username)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
        this.WhenAnyValue(x => x.Password)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(GeneratedCode)));
    }

    // Computed property that generates code preview
    public string GeneratedCode
    {
        get
        {
            if (!UseVisualBuilder)
                return DbContextConfigCode ?? string.Empty;

            return DbContextCodeGenerator.GenerateCode(
                SelectedDatabaseProvider,
                UseConnectionStringDirectly,
                ConnectionString,
                Server,
                Port,
                Database,
                Username,
                Password);
        }
    }
}