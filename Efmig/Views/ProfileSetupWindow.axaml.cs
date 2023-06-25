using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace Efmig.Views;

public partial class ProfileSetupWindow : ReactiveWindow<Window>
{
    public ProfileSetupWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void DotnetEfNugetLink_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        LinkOpener.Open("https://www.nuget.org/packages/dotnet-ef/");
    }

    private void MsEfCoreDesignNugetLink_OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        LinkOpener.Open("https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/");
    }
}