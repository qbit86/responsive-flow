using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace ResponsiveFlow;

public sealed partial class MainWindowViewModel
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

    private Task ExecuteRunAsync(CancellationToken cancellationToken)
    {
        var task = _model.RunAsync(cancellationToken);
        // TODO: Start the loop of polling the channel for results and updating the UI.
        return task;
    }

    private static string CreateTitle() =>
        TryGetVersion(out string? version) ? $"{nameof(ResponsiveFlow)} v{version}" : nameof(ResponsiveFlow);
}
