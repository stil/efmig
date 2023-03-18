using System.Collections.Generic;
using System.Windows.Input;
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
}