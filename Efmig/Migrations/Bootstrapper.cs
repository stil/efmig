using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Platform.Storage;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using DynamicData;
using Efmig.Migrations.Actions;
using Efmig.ViewModels;
using Efmig.Views;
using ReactiveUI;
using NLog;
using System.Threading.Tasks;

namespace Efmig.Migrations;

public class Bootstrapper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static MainWindow CreateMainWindow()
    {
        var configurationProfiles = Program.Profiles;

        var mainWindow = new MainWindow();
        var mainWindowViewModel = new MainWindowViewModel();
        mainWindowViewModel.ConfigurationProfileIds.AddRange(configurationProfiles.Select(p => p.Name));

        mainWindowViewModel.SelectSolution = ReactiveCommand.CreateFromTask(async () =>
        {
            var result = await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
            {
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Solution File")
                    {
                        Patterns = new[] { "*.sln" }
                    }
                }
            });

            if (result.Count == 1)
            {
                mainWindowViewModel.SolutionPath = result[0].Path.AbsolutePath;
            }
        });

        mainWindowViewModel.DiscoverFromSolution = ReactiveCommand.CreateFromTask(async () =>
        {
            if (string.IsNullOrEmpty(mainWindowViewModel.SolutionPath))
                return;

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, null);
            context.ClearLog();
            context.LogInfo("Starting DbContext discovery...");
                
            try
            {
                var discovery = new SolutionDiscovery(context);
                var generator = new ProfileGenerator(context);
                
                var dbContexts = await discovery.DiscoverDbContextsAsync(mainWindowViewModel.SolutionPath);
                
                if (dbContexts.Count == 0)
                {
                    context.LogImportant("No DbContexts found in the solution.");
                    return;
                }
                
                context.LogInfo("Generating profiles...");
                var newProfiles = await generator.GenerateProfilesAsync(dbContexts);
                
                int addedCount = 0;
                foreach (var profile in newProfiles)
                {
                    if (!configurationProfiles.Any(p => p.Name == profile.Name))
                    {
                        configurationProfiles.Add(profile);
                        addedCount++;
                        context.LogInfo($"Added profile: {profile.Name}");
                    }
                    else
                    {
                        context.LogVerbose($"Profile already exists: {profile.Name}");
                    }
                }
                
                if (addedCount > 0)
                {
                    configurationProfiles.Sort((a, b) =>
                        string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));
                    await ProfilesManager.SaveProfiles(configurationProfiles);
                    
                    mainWindowViewModel.ConfigurationProfileIds =
                        new ObservableCollection<string>(configurationProfiles.Select(p => p.Name));
                    
                    context.LogImportant($"Successfully added {addedCount} new profile(s)!");
                }
                else
                {
                    context.LogImportant("All discovered profiles already exist.");
                }
            }
            catch (Exception ex)
            {
                context.LogError($"Error during discovery: {ex.Message}");
                Logger.Error(ex, "Error during DbContext discovery");
            }
        }, mainWindowViewModel.WhenAnyValue(vm => vm.SolutionPath, path => !string.IsNullOrEmpty(path)));

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

            // Initialize empty collections - will be populated dynamically
            setupViewModel.DotnetEfVersions.Clear();
            setupViewModel.EfCoreDesignVersions.Clear();
            
            // Add current values to collections immediately so ComboBox isn't empty
            if (!string.IsNullOrEmpty(setupViewModel.DotnetEfVersionSelected))
            {
                setupViewModel.DotnetEfVersions.Add(setupViewModel.DotnetEfVersionSelected);
            }
            if (!string.IsNullOrEmpty(setupViewModel.EfCoreDesignVersionSelected))
            {
                setupViewModel.EfCoreDesignVersions.Add(setupViewModel.EfCoreDesignVersionSelected);
            }

            // Create the NuGet version loading command
            async Task LoadVersionsAsync()
            {
                setupViewModel.IsLoadingVersions = true;
                
                try
                {
                    var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, null);
                    var nugetService = new NuGetVersionService(context);
                    
                    context.LogInfo("Loading EF Core versions from NuGet...");
                    var efVersions = await nugetService.GetEfCoreVersionsAsync();
                    
                    // Store current selections
                    var currentDotnetEf = setupViewModel.DotnetEfVersionSelected;
                    var currentDesign = setupViewModel.EfCoreDesignVersionSelected;
                    
                    context.LogInfo($"Current selections before update - dotnet-ef: '{currentDotnetEf}', Design: '{currentDesign}'");
                    context.LogInfo($"Available tool versions: {string.Join(", ", efVersions.ToolVersions.Take(5))}... (showing first 5)");
                    
                    // Determine best selections first
                    string selectedDotnetEf;
                    string selectedDesign;
                    
                    if (!string.IsNullOrEmpty(currentDotnetEf))
                    {
                        var bestMatch = nugetService.FindBestMatchVersion(efVersions.ToolVersions, currentDotnetEf);
                        selectedDotnetEf = bestMatch ?? efVersions.ToolVersions.FirstOrDefault() ?? "8.0.0";
                    }
                    else
                    {
                        selectedDotnetEf = efVersions.ToolVersions.FirstOrDefault() ?? "8.0.0";
                    }
                    
                    if (!string.IsNullOrEmpty(currentDesign))
                    {
                        var bestMatch = nugetService.FindBestMatchVersion(efVersions.DesignVersions, currentDesign);
                        selectedDesign = bestMatch ?? efVersions.DesignVersions.FirstOrDefault() ?? "8.0.0";
                    }
                    else
                    {
                        selectedDesign = efVersions.DesignVersions.FirstOrDefault() ?? "8.0.0";
                    }
                    
                    // Update UI on main thread
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Clear and populate collections
                        setupViewModel.DotnetEfVersions.Clear();
                        setupViewModel.EfCoreDesignVersions.Clear();
                        
                        foreach (var version in efVersions.ToolVersions)
                        {
                            setupViewModel.DotnetEfVersions.Add(version);
                        }
                        
                        foreach (var version in efVersions.DesignVersions)
                        {
                            setupViewModel.EfCoreDesignVersions.Add(version);
                        }
                        
                        // Set selections after collections are populated
                        setupViewModel.DotnetEfVersionSelected = selectedDotnetEf;
                        setupViewModel.EfCoreDesignVersionSelected = selectedDesign;
                        
                        // Debug: Verify the selections were set
                        context.LogInfo($"After setting - dotnet-ef selected: '{setupViewModel.DotnetEfVersionSelected}'");
                        context.LogInfo($"After setting - design selected: '{setupViewModel.EfCoreDesignVersionSelected}'");
                        context.LogInfo($"Collection contains selected dotnet-ef: {setupViewModel.DotnetEfVersions.Contains(selectedDotnetEf)}");
                        context.LogInfo($"Collection contains selected design: {setupViewModel.EfCoreDesignVersions.Contains(selectedDesign)}");
                    });
                    
                    context.LogInfo($"Target selections - dotnet-ef: {selectedDotnetEf}, Design: {selectedDesign}");
                    
                    context.LogInfo($"Loaded {efVersions.ToolVersions.Count} dotnet-ef versions and {efVersions.DesignVersions.Count} EF Core Design versions");
                }
                catch (Exception ex)
                {
                    var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, null);
                    context.LogError($"Failed to load NuGet versions: {ex.Message}");
                    
                    // Fallback to basic versions if NuGet fails
                    var fallbackVersions = new List<string> { "6.6.6" };
                    setupViewModel.DotnetEfVersions.Clear();
                    setupViewModel.EfCoreDesignVersions.Clear();
                    
                    foreach (var version in fallbackVersions)
                    {
                        setupViewModel.DotnetEfVersions.Add(version);
                        setupViewModel.EfCoreDesignVersions.Add(version);
                    }
                    
                    // Set fallback selections
                    if (string.IsNullOrEmpty(setupViewModel.DotnetEfVersionSelected))
                    {
                        setupViewModel.DotnetEfVersionSelected = fallbackVersions.FirstOrDefault();
                    }
                    else if (!fallbackVersions.Contains(setupViewModel.DotnetEfVersionSelected))
                    {
                        // Add current selection to fallback list if not present
                        setupViewModel.DotnetEfVersions.Insert(0, setupViewModel.DotnetEfVersionSelected);
                    }
                    
                    if (string.IsNullOrEmpty(setupViewModel.EfCoreDesignVersionSelected))
                    {
                        setupViewModel.EfCoreDesignVersionSelected = fallbackVersions.FirstOrDefault();
                    }
                    else if (!fallbackVersions.Contains(setupViewModel.EfCoreDesignVersionSelected))
                    {
                        // Add current selection to fallback list if not present
                        setupViewModel.EfCoreDesignVersions.Insert(0, setupViewModel.EfCoreDesignVersionSelected);
                    }
                }
                finally
                {
                    setupViewModel.IsLoadingVersions = false;
                }
            }
            
            // Set up manual reload command
            setupViewModel.LoadNuGetVersions = ReactiveCommand.CreateFromTask(LoadVersionsAsync);
            
            // Automatically load versions when window opens
            _ = Task.Run(LoadVersionsAsync);

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

        mainWindowViewModel.RemoveProfile = ReactiveCommand.CreateFromTask(async () =>
        {
            if (string.IsNullOrEmpty(mainWindowViewModel.SelectedConfigurationProfile))
                return;

            var selectedProfile = configurationProfiles.First(p => 
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            // Create confirmation dialog
            var dialog = new Window
            {
                Title = "Confirm Profile Removal",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15
            };

            stackPanel.Children.Add(new TextBlock
            {
                Text = $"Are you sure you want to remove the profile:",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });

            stackPanel.Children.Add(new TextBlock
            {
                Text = selectedProfile.Name,
                FontWeight = FontWeight.Bold,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            var removeButton = new Button
            {
                Content = "Remove",
                Width = 80,
                Background = Brushes.Red,
                Foreground = Brushes.White
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80
            };

            bool? dialogResult = null;

            removeButton.Click += (s, e) =>
            {
                dialogResult = true;
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                dialogResult = false;
                dialog.Close();
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(removeButton);
            
            stackPanel.Children.Add(buttonPanel);
            dialog.Content = stackPanel;

            await dialog.ShowDialog(mainWindow);

            if (dialogResult == true)
            {
                configurationProfiles.Remove(selectedProfile);
                await ProfilesManager.SaveProfiles(configurationProfiles);

                mainWindowViewModel.ConfigurationProfileIds =
                    new ObservableCollection<string>(configurationProfiles.Select(p => p.Name));
                mainWindowViewModel.SelectedConfigurationProfile = null;

                var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, null);
                context.LogInfo($"Profile '{selectedProfile.Name}' has been removed.");
            }
        }, profileSelected);

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
            var action = new OptimizeAction();
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
        
        mainWindowViewModel.GenerateApplyScriptForLastMigration = ReactiveCommand.CreateFromTask(() =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            var action = new GenerateMigrationScriptAction(new IApplyLastMigrationScriptMode());
            return action.ExecuteAsync(context);
        }, profileSelected);
        
        mainWindowViewModel.GenerateRollbackScriptForLastMigration = ReactiveCommand.CreateFromTask(() =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            var action = new GenerateMigrationScriptAction(new IRollbackLastMigrationScriptMode());
            return action.ExecuteAsync(context);
        }, profileSelected);

        mainWindowViewModel.GenerateMigrationScript = ReactiveCommand.CreateFromTask(() =>
        {
            var selectedProfile = configurationProfiles.First(p =>
                p.Name == mainWindowViewModel.SelectedConfigurationProfile);

            var context = new ActionContext(mainWindow.LogViewer, mainWindow.LogScrollViewer, selectedProfile);
            var action = new GenerateMigrationScriptAction(new IFullMigrationScriptMode());
            return action.ExecuteAsync(context);
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
        
        mainWindow.LogViewer.Inlines!.Add(new Run("Command result will appear here."));

        mainWindow.DataContext = mainWindowViewModel;
        return mainWindow;
    }
}