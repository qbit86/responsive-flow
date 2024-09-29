using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
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
        _runCommand = new AsyncRelayCommand(ExecuteRunAsync, CanExecuteRun);
    }

    public IAsyncRelayCommand RunCommand => _runCommand;

    public string Title { get; } = CreateTitle();

    public ObservableCollection<InAppMessageViewModel> Messages { get; } = [];

    public void CancelRun() => _runCommand.Cancel();

    private async Task ExecuteRunAsync(CancellationToken cancellationToken)
    {
        var collectedDataFuture = _model.RunAsync(cancellationToken);

        // This loop polls the channel for in-app messages from the model
        // and updates the UI by posting the messages to an observable collection bound to the view.
        while (!collectedDataFuture.IsCompleted && !cancellationToken.IsCancellationRequested)
        {
            var messageFuture = _model.MessageChannelReader.ReadAsync(cancellationToken);
            try
            {
                await Dispatcher.Yield();
                var message = await messageFuture.ConfigureAwait(true);
                var messageViewModel = InAppMessageViewModel.Create(message);
                Messages.Add(messageViewModel);
            }
            catch (OperationCanceledException) { }
        }

        Task collectedDataTask = collectedDataFuture;
        await collectedDataTask.ConfigureAwait(
            ConfigureAwaitOptions.ContinueOnCapturedContext | ConfigureAwaitOptions.SuppressThrowing);
    }

    private bool CanExecuteRun() => _runCommand.ExecutionTask is null;

    private static string CreateTitle() =>
        TryGetVersion(out string? version) ? $"{nameof(ResponsiveFlow)} v{version}" : nameof(ResponsiveFlow);
}
