using System;
using System.Windows;
using Microsoft.Extensions.Logging.Abstractions;

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
        MainModel model = new(NullLogger<MainModel>.Instance);
        MainWindowViewModel viewModel = new(model);
        MainWindow window = new(viewModel);
        window.Show();
    }
}
