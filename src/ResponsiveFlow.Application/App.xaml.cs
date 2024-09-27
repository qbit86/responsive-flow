using System;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

public partial class App
{
    [STAThread]
    internal static void Main()
    {
        App application = new();
        application.InitializeComponent();

        ServiceCollection services = new();

        ConfigurationBuilder configurationBuilder = new();
        ConfigureConfiguration(configurationBuilder);
        var configurationRoot = configurationBuilder.Build();
        services.AddSingleton<IConfiguration>(configurationRoot);

        services.AddLogging(ConfigureLogging);
        services.AddSingleton<MainModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        var serviceProvider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(serviceProvider);

        _ = application.Run();
        return;

        void ConfigureLogging(ILoggingBuilder builder)
        {
            // https://github.com/dotnet/runtime/blob/v8.0.8/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L275-L317
            builder.AddConfiguration(configurationRoot.GetSection("Logging"));
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var window = Ioc.Default.GetRequiredService<MainWindow>();
        window.Show();
    }

    private static void ConfigureConfiguration(IConfigurationBuilder configurationBuilder)
    {
        // https://github.com/dotnet/runtime/blob/v8.0.8/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L236-L271
#if DEBUG
        string environmentName = Environments.Development;
#else
        string environmentName = Environments.Production;
#endif
        configurationBuilder.AddJsonFile("appsettings.json", true)
            .AddJsonFile($"appsettings.{environmentName}.json", true);

#if DEBUG
        configurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
#endif

        configurationBuilder.AddEnvironmentVariables();
        configurationBuilder.AddCommandLine(Environment.GetCommandLineArgs());
    }
}
