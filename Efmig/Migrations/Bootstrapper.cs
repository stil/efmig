using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Platform.Storage;
using DynamicData;
using Efmig.Migrations.Actions;
using Efmig.ViewModels;
using Efmig.Views;
using ReactiveUI;

namespace Efmig.Migrations;

public class Bootstrapper
{
    public static MainWindow CreateMainWindow()
    {
        var configurationProfiles = Program.Profiles;

        var mainWindow = new MainWindow();
        var mainWindowViewModel = new MainWindowViewModel();
        mainWindowViewModel.ConfigurationProfileIds.AddRange(configurationProfiles.Select(p => p.Name));

        ConfigurationProfile PopulateConfigurationProfileFromViewModel(ProfileSetupWindowViewModel viewModel)
        {
            return new ConfigurationProfile
            {
                Name = viewModel.ProfileName,
                DbContextCsprojPath = viewModel.DbContextCsprojPath,
                RuntimeVersion = viewModel.RuntimeVersionSelected,
                DbContextConfigCode = viewModel.DbContextConfigCode,
                DbContextFullName = viewModel.DbContextFullName,
                DotnetEfVersion = viewModel.DotnetEfVersionSelected,
                EfCoreDesignVersion = viewModel.EfCoreDesignVersionSelected,
                MigrationsDir = viewModel.MigrationsDir
            };
        }

        void InitializeProfileSetupViewModel(ProfileSetupWindowViewModel setupViewModel, ProfileSetupWindow setupWindow,
            ConfigurationProfile existingProfile)
        {
            setupViewModel.WindowTitle = existingProfile != null ? "Edit profile" : "Create new profile";
            setupViewModel.ProfileName = existingProfile?.Name ?? "";
            setupViewModel.DotnetEfVersionSelected = existingProfile?.DotnetEfVersion ?? "8.0.0";
            setupViewModel.EfCoreDesignVersionSelected = existingProfile?.EfCoreDesignVersion ?? "8.0.0";
            setupViewModel.RuntimeVersionSelected = existingProfile?.RuntimeVersion ?? "net8.0";
            setupViewModel.DbContextCsprojPath = existingProfile?.DbContextCsprojPath ?? "";
            setupViewModel.DbContextFullName = existingProfile?.DbContextFullName ?? "";
            setupViewModel.DbContextConfigCode =
                existingProfile?.DbContextConfigCode ?? "optionsBuilder.UseNpgsql(\"\")";
            setupViewModel.MigrationsDir = existingProfile?.MigrationsDir ?? "";

            setupViewModel.DbContextCsprojSelect = ReactiveCommand.CreateFromTask(async () =>
            {
                var result = await setupWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
                {
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("C# Project")
                        {
                            Patterns = new[] { "*.csproj" }
                        }
                    }
                });

                if (result.Count == 1)
                {
                    setupViewModel.DbContextCsprojPath = result[0].Path.AbsolutePath;
                }
            });

            if (existingProfile != null)
            {
                setupViewModel.DeleteProfile = new DeleteProfileViewModel()
                {
                    DeleteCommand = ReactiveCommand.CreateFromTask(async () =>
                    {
                        configurationProfiles.Remove(existingProfile);
                        mainWindowViewModel.SelectedConfigurationProfile = null;
                        mainWindowViewModel.ConfigurationProfileIds.Remove(existingProfile.Name);
                        await ProfilesManager.SaveProfiles(configurationProfiles);
                        setupWindow.Close();
                    })
                };
            }
        }

        mainWindowViewModel.AddProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            var setupWindow = new ProfileSetupWindow();
            var setupViewModel = new ProfileSetupWindowViewModel();
            InitializeProfileSetupViewModel(setupViewModel, setupWindow, null);

            setupViewModel.Save = ReactiveCommand.CreateFromTask(async () =>
            {
                // Maybe add validation.
                var profile = PopulateConfigurationProfileFromViewModel(setupViewModel);
                configurationProfiles.Add(profile);
                configurationProfiles.Sort((a, b) =>
                    string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));
                await ProfilesManager.SaveProfiles(configurationProfiles);

                mainWindowViewModel.ConfigurationProfileIds =
                    new ObservableCollection<string>(configurationProfiles.Select(p => p.Name));
                mainWindowViewModel.SelectedConfigurationProfile = profile.Name;

                setupWindow.Close();
            });
            setupWindow.DataContext = setupViewModel;
            await setupWindow.ShowDialog(mainWindow);
        });

        var profileSelected =
            mainWindowViewModel.WhenAnyValue(vm => vm.SelectedConfigurationProfile, (string s) => s != null);

        var profileSelectedAndEnteredMigrationName = mainWindowViewModel.WhenAnyValue(
            vm => vm.SelectedConfigurationProfile, vm => vm.NewMigrationName,
            (profile, newMigrationName) => profile != null && !string.IsNullOrWhiteSpace(newMigrationName));

        mainWindowViewModel.EditProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            var setupWindow = new ProfileSetupWindow();
            var setupViewModel = new ProfileSetupWindowViewModel();
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);
            InitializeProfileSetupViewModel(setupViewModel, setupWindow, selectedProfile);

            setupViewModel.Save = ReactiveCommand.CreateFromTask(async () =>
            {
                // Maybe add validation.
                var profile = PopulateConfigurationProfileFromViewModel(setupViewModel);
                configurationProfiles.RemoveAll(p => p.Name == selectedProfile.Name);
                configurationProfiles.Add(profile);
                configurationProfiles.Sort((a, b) =>
                    string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));
                await ProfilesManager.SaveProfiles(configurationProfiles);

                mainWindowViewModel.ConfigurationProfileIds =
                    new ObservableCollection<string>(configurationProfiles.Select(p => p.Name));
                mainWindowViewModel.SelectedConfigurationProfile = profile.Name;

                setupWindow.Close();
            });
            setupWindow.DataContext = setupViewModel;
            await setupWindow.ShowDialog(mainWindow);
        }, profileSelected);

        mainWindowViewModel.ListMigrations = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            var action = new ListMigrationsAction();
            await action.ExecuteAsync(context);
        }, profileSelected);

        mainWindowViewModel.Optimize = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            var action = new OptimizeAction();
            await action.ExecuteAsync(context);
        }, profileSelected);
        
        mainWindowViewModel.RemoveLastMigration = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            var action = new RemoveLastMigrationAction();
            await action.ExecuteAsync(context);
        }, profileSelected);
        
        mainWindowViewModel.RecreateLastAndGenerateScript = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            var action1 = new RemoveLastMigrationAction();
            await action1.ExecuteAsync(context);
            
            context.Data = mainWindowViewModel.NewMigrationName;
            var action2 = new CreateMigrationAction();
            await action2.ExecuteAsync(context);
            context.Data = null;
            
            var action3 = new GenerateMigrationScriptAction(new IApplyLastMigrationScriptMode());
            await action3.ExecuteAsync(context);
        }, profileSelectedAndEnteredMigrationName);
        
        mainWindowViewModel.GenerateApplyScriptForLastMigration = ReactiveCommand.CreateFromTask(() =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            var action = new GenerateMigrationScriptAction(new IApplyLastMigrationScriptMode());
            return action.ExecuteAsync(context);
        }, profileSelected);
        
        mainWindowViewModel.GenerateRollbackScriptForLastMigration = ReactiveCommand.CreateFromTask(() =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            var action = new GenerateMigrationScriptAction(new IRollbackLastMigrationScriptMode());
            return action.ExecuteAsync(context);
        }, profileSelected);

        mainWindowViewModel.GenerateMigrationScript = ReactiveCommand.CreateFromTask(() =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            var action = new GenerateMigrationScriptAction(new IFullMigrationScriptMode());
            return action.ExecuteAsync(context);
        }, profileSelected);

        mainWindowViewModel.GenerateUnappliedMigrationScript = ReactiveCommand.CreateFromTask(() =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            var action = new GenerateMigrationScriptAction(new IUnappliedScriptMode());
            return action.ExecuteAsync(context);
        }, profileSelected);

        mainWindowViewModel.CreateMigration = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.ClearLog();

            context.Data = mainWindowViewModel.NewMigrationName;
            var action1 = new CreateMigrationAction();
            await action1.ExecuteAsync(context);
            context.Data = null;
            
            var action2 = new GenerateMigrationScriptAction(new IApplyLastMigrationScriptMode());
            await action2.ExecuteAsync(context);
        }, profileSelectedAndEnteredMigrationName);
        
        mainWindow.LogViewer.Inlines!.Add(new Run("Command result will appear here."));

        mainWindow.DataContext = mainWindowViewModel;
        return mainWindow;
    }
}