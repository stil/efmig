using Avalonia;
using Avalonia.Controls;

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

    private bool _initial = true;
    private void Control_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_initial)
        {
            _initial = false;
            return;
        }
        if (e.HeightChanged)
            ScrollOutputViewer.Height = e.NewSize.Height - 35;
    }
}