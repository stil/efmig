using System.Collections.ObjectModel;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Efmig.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public bool VerboseLogging { get; set; }
    [Reactive] public string SelectedConfigurationProfile { get; set; }
    [Reactive] public ObservableCollection<string> ConfigurationProfileIds { get; set; } = new();
    [Reactive] public string NewMigrationName { get; set; }
    [Reactive] public ICommand AddProfile { get; set; }
    [Reactive] public ICommand EditProfile { get; set; }
    [Reactive] public ICommand CreateMigration { get; set; }
    [Reactive] public ICommand ListMigrations { get; set; }
    [Reactive] public ICommand RemoveLastMigration { get; set; }
    [Reactive] public ICommand GenerateMigrationScript { get; set; }
    [Reactive] public ICommand Optimize { get; set; }
}