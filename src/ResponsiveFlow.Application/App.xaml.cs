using System;
using System.Net.Http;
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

        services.Configure<ProjectDto>(configurationRoot.GetSection("Project"));
        services.AddSingleton(CreateHttpClient);
        services.AddSingleton<MainModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        var serviceProvider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(serviceProvider);

        _ = application.Run();
        return;

        void ConfigureLogging(ILoggingBuilder loggingBuilder)
        {
            // https://github.com/dotnet/runtime/blob/v8.0.8/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L298-L308
            loggingBuilder.AddConfiguration(configurationRoot.GetSection("Logging"));
#if DEBUG
            loggingBuilder.AddDebug();
#endif
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
        // https://github.com/dotnet/runtime/blob/v8.0.8/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L241-L259
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

    private static HttpClient CreateHttpClient(IServiceProvider serviceProvider) => new();
}
