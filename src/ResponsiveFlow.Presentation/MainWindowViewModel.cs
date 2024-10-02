using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ResponsiveFlow;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly MainModel _model;
    private readonly AsyncRelayCommand _runCommand;
    private readonly CancellationTokenSource _stoppingCts = new();
    private Visibility _progressBarVisibility = Visibility.Collapsed;
    private double _progressValue;

    public MainWindowViewModel(MainModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        _runCommand = new AsyncRelayCommand(ExecuteRunAsync, CanExecuteRun);
        _model.ProgressChanged += OnModelProgressChanged;
    }

    public IAsyncRelayCommand RunCommand => _runCommand;

    public string Title { get; } = CreateTitle();

    public Visibility ProgressBarVisibility
    {
        get => _progressBarVisibility;
        private set => SetProperty(ref _progressBarVisibility, value);
    }

    public double ProgressValue
    {
        get => _progressValue;
        private set => SetProperty(ref _progressValue, value);
    }

    public ObservableCollection<InAppMessageViewModel> Messages { get; } = [];

    public void Dispose()
    {
        _model.ProgressChanged -= OnModelProgressChanged;
        _stoppingCts.Dispose();
    }

    private void OnModelProgressChanged(object? _, double e) => ProgressValue = e;

    public void Run() => _ = RunUpdateLoopAsync(_stoppingCts.Token);

    public void Shutdown()
    {
        _runCommand.Cancel();
        _stoppingCts.Cancel();
    }

    private async Task RunUpdateLoopAsync(CancellationToken stoppingToken)
    {
        // This loop polls the channel for in-app messages from the model
        // and updates the UI by posting the messages to an observable collection bound to the view.
        while (!stoppingToken.IsCancellationRequested)
        {
            var messageFuture = _model.MessageChannelReader.ReadAsync(stoppingToken);
            try
            {
                await Dispatcher.Yield();
                var message = await messageFuture.ConfigureAwait(true);
                var messageViewModel = InAppMessageViewModel.Create(message);
                Messages.Add(messageViewModel);
            }
            catch (OperationCanceledException) { }
        }
    }

    private async Task ExecuteRunAsync(CancellationToken cancellationToken)
    {
        try
        {
            ProgressBarVisibility = Visibility.Visible;
            var collectedDataFuture = _model.RunAsync(cancellationToken);
            OnPropertyChanged(nameof(ProgressBarVisibility));
            _ = await collectedDataFuture.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            var message = InAppMessage.FromException(exception);
            var messageViewModel = InAppMessageViewModel.Create(message);
            Messages.Add(messageViewModel);
        }
    }

    private bool CanExecuteRun() => _runCommand.ExecutionTask is null;

    private static string CreateTitle() =>
        TryGetVersion(out string? version) ? $"{nameof(ResponsiveFlow)} v{version}" : nameof(ResponsiveFlow);
}
