using System;
using System.Windows;

namespace ResponsiveFlow;

public partial class App
{
    [STAThread]
    internal static void Main()
    {
        var application = new App();
        application.InitializeComponent();
        application.Run();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        MainWindowViewModel viewModel = new();
        MainWindow window = new(viewModel);
        window.Show();
    }
}
