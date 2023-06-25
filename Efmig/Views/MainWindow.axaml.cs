using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Efmig.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void Control_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.HeightChanged)
            ScrollOutputViewer.Height = e.NewSize.Height;
    }

    private void ProjectUrl_OnClick(object sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.InvokeAsync(() => { LinkOpener.Open("https://github.com/stil/efmig"); });
    }
}