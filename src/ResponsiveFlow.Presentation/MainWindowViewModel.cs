using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace ResponsiveFlow;

public sealed class MainWindowViewModel
{
    private readonly MainModel _model;
    private readonly AsyncRelayCommand _runCommand;

    public MainWindowViewModel(MainModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        _runCommand = new AsyncRelayCommand(ExecuteRunAsync);
    }

    public IAsyncRelayCommand RunCommand => _runCommand;

    public string Title { get; } = CreateTitle();

    public void CancelRun() => _runCommand.Cancel();

    private async Task ExecuteRunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            await Task.Delay(1000, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private static string CreateTitle() =>
        TryGetVersion(out string? version) ? $"{nameof(ResponsiveFlow)} {version}" : nameof(ResponsiveFlow);

    private static bool TryGetVersion([NotNullWhen(true)] out string? version)
    {
        var assembly = typeof(MainWindowViewModel).Assembly;
        var attributes = assembly.GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute));
        var informationalVersions =
            attributes.OfType<AssemblyInformationalVersionAttribute>().Select(it => it.InformationalVersion);
        string? informationalVersion = informationalVersions.FirstOrDefault(it => !string.IsNullOrWhiteSpace(it));
        version = informationalVersion ?? assembly.GetName().Version?.ToString();
        return !string.IsNullOrWhiteSpace(version);
    }
}
