﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
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
            };
        }

        void InitializeProfileSetupViewModel(ProfileSetupWindowViewModel setupViewModel, ProfileSetupWindow setupWindow,
            ConfigurationProfile existingProfile)
        {
            setupViewModel.ProfileName = existingProfile?.Name ?? "";
            setupViewModel.DotnetEfVersionSelected = existingProfile?.DotnetEfVersion ?? "";
            setupViewModel.EfCoreDesignVersionSelected = existingProfile?.EfCoreDesignVersion ?? "";
            setupViewModel.RuntimeVersionSelected = existingProfile?.RuntimeVersion ?? "";
            setupViewModel.DbContextCsprojPath = existingProfile?.DbContextCsprojPath ?? "";
            setupViewModel.DbContextFullName = existingProfile?.DbContextFullName ?? "";
            setupViewModel.DbContextConfigCode =
                existingProfile?.DbContextConfigCode ?? "optionsBuilder.UseNpgsql(\"\")";

            setupViewModel.DotnetEfVersionOptions = new List<string>
            {
                "7.0.3",
                "6.0.14"
            };

            setupViewModel.EfCoreDesignVersionOptions = new List<string>
            {
                "7.0.3",
                "6.0.14"
            };

            setupViewModel.RuntimeVersionOptions = new List<string>
            {
                "net7.0",
                "net6.0"
            };

            setupViewModel.DbContextCsprojSelect = ReactiveCommand.CreateFromTask(async () =>
            {
                var dialog = new OpenFileDialog
                {
                    Filters = new List<FileDialogFilter>
                    {
                        new()
                        {
                            Extensions = new List<string> { "csproj" }
                        }
                    }
                };
                var result = await dialog.ShowAsync(setupWindow);
                if (result is { Length: 1 })
                {
                    setupViewModel.DbContextCsprojPath = result[0];
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

        mainWindowViewModel.RemoveLastMigration = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            var action = new RemoveLastMigrationAction();
            await action.ExecuteAsync(context);
        }, profileSelected);

        mainWindowViewModel.GenerateMigrationScript = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            var action = new GenerateMigrationScriptAction();
            await action.ExecuteAsync(context);
        }, profileSelected);

        var profileSelectedAndEnteredMigrationName = mainWindowViewModel.WhenAnyValue(
            vm => vm.SelectedConfigurationProfile, vm => vm.NewMigrationName,
            (profile, newMigrationName) => profile != null && !string.IsNullOrWhiteSpace(newMigrationName));

        mainWindowViewModel.CreateMigration = ReactiveCommand.CreateFromTask(async () =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            context.Data = mainWindowViewModel.NewMigrationName;
            var action = new CreateMigrationAction();
            await action.ExecuteAsync(context);
        }, profileSelectedAndEnteredMigrationName);

        mainWindow.DataContext = mainWindowViewModel;
        return mainWindow;
    }
}