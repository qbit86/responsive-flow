using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Machinery;
using Microsoft.Win32;

namespace ResponsiveFlow;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private static readonly JsonSerializerOptions s_options = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly MainModel _model;
    private readonly AsyncRelayCommand _openCommand;
    private readonly AsyncRelayCommand _runCommand;
    private readonly StateMachine<MainWindowViewModel, IEvent, State> _stateMachine;
    private readonly CancellationTokenSource _stoppingCts = new();
    private double _progressValue;

    public MainWindowViewModel(MainModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        _openCommand = new AsyncRelayCommand(ExecuteOpenAsync, CanExecuteOpen);
        _runCommand = new AsyncRelayCommand(ExecuteRunAsync, CanExecuteRun);
        _model.ProgressChanged += OnModelProgressChanged;
        _stateMachine = StateMachine<IEvent>.Create(this, (State)ProjectNotLoadedState.Instance);
    }

    public ICommand OpenCommand => _openCommand;

    public ICommand RunCommand => _runCommand;

    public Visibility ProgressBarVisibility => _stateMachine.CurrentState switch
    {
        ReadyToRunState or RunningState or CompletedState => Visibility.Visible,
        _ => Visibility.Collapsed
    };

    public double ProgressValue
    {
        get => _progressValue;
        private set
        {
            if (_progressValue.Equals(value))
                return;
            _progressValue = value;
            OnPropertyChanged(ProgressValueChangedEventArgs);
        }
    }

    public string StateStatus => _stateMachine.CurrentState switch
    {
        ProjectNotLoadedState => "Not loaded",
        LoadingState => "Loading",
        ReadyToRunState => "Ready to run",
        RunningState => "Running",
        CompletedState => "Completed",
        _ => string.Empty
    };

    public string Title => _stateMachine.CurrentState switch
    {
        LoadingState state => FormatTitle(state.ProjectPath),
        ReadyToRunState state => FormatTitle(state.ProjectPath),
        RunningState state => FormatTitle(state.ProjectPath),
        CompletedState state => FormatTitle(state.ProjectPath),
        _ => CreateTitle()
    };

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

        _ = _stateMachine.TryProcessEvent(CancelEvent.Instance);
    }

    private async Task ExecuteOpenAsync(CancellationToken cancellationToken)
    {
        try
        {
            OpenFileDialog dialog = new()
            {
                AddToRecent = true,
                CheckFileExists = true,
                DefaultExt = ".json",
                Filter = "JSON documents|*.json",
                ValidateNames = true
            };

            bool? result = dialog.ShowDialog();
            if (!result.GetValueOrDefault())
                return;

            string path = dialog.FileName;
            Stream utf8Json = File.OpenRead(path);
            await using (utf8Json)
            {
                _ = _stateMachine.TryProcessEvent(new OpenEvent(path));
                var future = JsonSerializer.DeserializeAsync<ProjectDto>(utf8Json, s_options, cancellationToken);
                var projectDto = await future.ConfigureAwait(true);
                IEvent ev = projectDto is null ? CancelEvent.Instance : new CompleteEvent(projectDto);
                _ = _stateMachine.TryProcessEvent(ev);
                _model.SetProject(projectDto);
            }
        }
        catch (OperationCanceledException)
        {
            _ = _stateMachine.TryProcessEvent(CancelEvent.Instance);
        }
        catch (Exception exception)
        {
            _ = _stateMachine.TryProcessEvent(CancelEvent.Instance);
            var message = InAppMessage.FromException(exception);
            var messageViewModel = InAppMessageViewModel.Create(message);
            Messages.Add(messageViewModel);
        }
    }

    private bool CanExecuteOpen() =>
        _stateMachine.CurrentState is ProjectNotLoadedState or ReadyToRunState or CompletedState;

    private async Task ExecuteRunAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = _stateMachine.TryProcessEvent(RunEvent.Instance);
            var collectedDataFuture = _model.RunAsync(cancellationToken);
            _ = await collectedDataFuture.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
            if (_stateMachine.CurrentState is ProjectLoadedState { Project: var project })
                _ = _stateMachine.TryProcessEvent(new CompleteEvent(project));
        }
        catch (OperationCanceledException)
        {
            _ = _stateMachine.TryProcessEvent(CancelEvent.Instance);
        }
        catch (Exception exception)
        {
            _ = _stateMachine.TryProcessEvent(CancelEvent.Instance);
            var message = InAppMessage.FromException(exception);
            var messageViewModel = InAppMessageViewModel.Create(message);
            Messages.Add(messageViewModel);
        }
    }

    private bool CanExecuteRun() => _stateMachine.CurrentState is ReadyToRunState or CompletedState;

    private static string CreateTitle() =>
        TryGetVersion(out string? version) ? $"{nameof(ResponsiveFlow)} v{version}" : nameof(ResponsiveFlow);

    private static string FormatTitle(string path) => $"{nameof(ResponsiveFlow)} - {path}";
}
