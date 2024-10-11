using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly CancellationTokenSource _stoppingCts = new();
    private Visibility _progressBarVisibility = Visibility.Collapsed;
    private double _progressValue;
    private string _title = CreateTitle();

    public MainWindowViewModel(MainModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        _model = model;
        _openCommand = new AsyncRelayCommand(ExecuteOpenAsync, CanExecuteOpen);
        _runCommand = new AsyncRelayCommand(ExecuteRunAsync, CanExecuteRun);
        _model.ProgressChanged += OnModelProgressChanged;
    }

    private static PropertyChangedEventArgs ProgressValueChangedEventArgs { get; } = new(nameof(ProgressValue));

    public ICommand OpenCommand => _openCommand;

    public ICommand RunCommand => _runCommand;

    public Visibility ProgressBarVisibility
    {
        get => _progressBarVisibility;
        private set => SetProperty(ref _progressBarVisibility, value);
    }

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

    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
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
                var future = JsonSerializer.DeserializeAsync<ProjectDto>(utf8Json, s_options, cancellationToken);
                var projectDto = await future.ConfigureAwait(true);
                _model.SetProject(projectDto);
                Title = FormatTitle(path);
                _runCommand.NotifyCanExecuteChanged();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            var message = InAppMessage.FromException(exception);
            var messageViewModel = InAppMessageViewModel.Create(message);
            Messages.Add(messageViewModel);
        }
    }

    private bool CanExecuteOpen() => _runCommand.ExecutionTask is not { Status: TaskStatus.Running };

    private async Task ExecuteRunAsync(CancellationToken cancellationToken)
    {
        try
        {
            _openCommand.NotifyCanExecuteChanged();
            ProgressBarVisibility = Visibility.Visible;
            var collectedDataFuture = _model.RunAsync(cancellationToken);
            _ = await collectedDataFuture.ConfigureAwait(ConfigureAwaitOptions.ContinueOnCapturedContext);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            var message = InAppMessage.FromException(exception);
            var messageViewModel = InAppMessageViewModel.Create(message);
            Messages.Add(messageViewModel);
        }
        finally
        {
            _openCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanExecuteRun() => _runCommand.ExecutionTask is null && _model.CanRun();

    private static string CreateTitle() =>
        TryGetVersion(out string? version) ? $"{nameof(ResponsiveFlow)} v{version}" : nameof(ResponsiveFlow);

    private static string FormatTitle(string path) => $"{nameof(ResponsiveFlow)} - {path}";
}
