using System.Windows;

namespace ResponsiveFlow;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindowViewModel viewModel = new();
        MainWindow window = new(viewModel);
        window.Show();
    }
}
