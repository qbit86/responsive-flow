using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal readonly record struct Attempt(int UriIndex, Uri Uri, int AttemptIndex);

internal sealed partial class ProjectRunner
{
    private const int RequestChannelCapacity = 32;
    private const int RequestCount = 100;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly ChannelWriter<InAppMessage> _messageChannelWriter;
    private readonly Channel<RequestCollectedData> _requestChannel;
    private readonly UriCollectedData[] _uriCollectedDataset;
    private readonly List<Uri> _uris;

    private ProjectRunner(
        List<Uri> uris,
        string outputDirectory,
        HttpClient httpClient,
        Channel<RequestCollectedData> requestChannel,
        ChannelWriter<InAppMessage> messageChannelWriter,
        UriCollectedData[] uriCollectedDataset,
        ILogger logger)
    {
        _uris = uris;
        OutputDirectory = outputDirectory;
        _httpClient = httpClient;
        _requestChannel = requestChannel;
        _messageChannelWriter = messageChannelWriter;
        _uriCollectedDataset = uriCollectedDataset;
        _logger = logger;
    }

    internal string OutputDirectory { get; }

    private ChannelReader<RequestCollectedData> RequestChannelReader => _requestChannel.Reader;

    private ChannelWriter<RequestCollectedData> RequestChannelWriter => _requestChannel.Writer;

    internal static ProjectRunner Create(
        ProjectDto projectDto,
        HttpClient httpClient,
        ChannelWriter<InAppMessage> messageChannelWriter,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(messageChannelWriter);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var validUris = GetValidUris(projectDto);
        var startTime = DateTime.Now;
        string effectiveOutputDirectory = GetOutputDirectoryOrFallback(projectDto, startTime);
        var requestChannel = Channel.CreateBounded<RequestCollectedData>(RequestChannelCapacity);
        var uriCollectedDataset = validUris.Select((uri, uriIndex) => new UriCollectedData(uriIndex, uri, []))
            .ToArray();
        var logger = loggerFactory.CreateLogger<ProjectRunner>();
        return new(validUris, effectiveOutputDirectory, httpClient,
            requestChannel, messageChannelWriter, uriCollectedDataset, logger);
    }

    internal Task<ProjectCollectedData> RunAsync(CancellationToken cancellationToken)
    {
        if (RequestChannelReader.Completion.IsCompleted)
            ThrowHelpers.ThrowInvalidOperationException("The request channel is already completed.");

        return RunCoreAsync(cancellationToken);
    }

    private async Task<ProjectCollectedData> RunCoreAsync(CancellationToken cancellationToken)
    {
        var cancellationTokenRegistration = cancellationToken.Register(() => _ = RequestChannelWriter.TryComplete());
        await using (cancellationTokenRegistration.ConfigureAwait(false))
        {
            var consumingTask = ConsumeAllAsync(cancellationToken);

            try
            {
                var producingTask = ProduceAllAsync(cancellationToken);
                await producingTask.ConfigureAwait(false);
            }
            finally
            {
                _ = RequestChannelWriter.TryComplete();
            }

            await consumingTask.ConfigureAwait(false);

            return new(_uriCollectedDataset);
        }
    }

    private async Task ConsumeAllAsync(CancellationToken cancellationToken)
    {
        var requestAsyncCollection = RequestChannelReader.ReadAllAsync(cancellationToken);
        var consumingTask = Parallel.ForEachAsync(requestAsyncCollection, cancellationToken, ConsumeBodyAsync);
        try
        {
            await consumingTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
    }

    private async Task ProduceAllAsync(CancellationToken cancellationToken)
    {
        for (int uriIndex = 0; uriIndex < _uris.Count && !cancellationToken.IsCancellationRequested; ++uriIndex)
        {
            var uri = _uris[uriIndex];
            int uriIndexCopy = uriIndex;
            LogProcessingUrl(uri.AbsoluteUri, uriIndexCopy, _uris.Count);
            var attempts = Enumerable.Repeat(uri, RequestCount)
                .Select((u, attemptIndex) => new Attempt(uriIndexCopy, u, attemptIndex));
            var producingTask = Parallel.ForEachAsync(attempts, cancellationToken, ProduceBodyAsync);
            try
            {
                await producingTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            finally
            {
                LogProcessedUrl(uri.AbsoluteUri, uriIndexCopy, _uris.Count);
            }
        }
    }

    private async ValueTask ConsumeBodyAsync(
        RequestCollectedData requestCollectedData, CancellationToken cancellationToken)
    {
        Task endingTimestampTask = requestCollectedData.EndingTimestampFuture;
        await endingTimestampTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        Task responseTask = requestCollectedData.ResponseFuture;
        await responseTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        var requests = _uriCollectedDataset[requestCollectedData.UriIndex].Requests;
        requests.Add(requestCollectedData);
        if (requestCollectedData.ResponseFuture.Exception is { } exception)
        {
            var innerExceptions = exception.Flatten().InnerExceptions;
            foreach (var innerException in innerExceptions)
            {
                var writeTask =
                    _messageChannelWriter.WriteAsync(InAppMessage.FromException(innerException), cancellationToken);
                await writeTask.ConfigureAwait(false);
            }
        }
    }

    private ValueTask ProduceBodyAsync(Attempt attempt, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.CompletedTask;
        (int uriIndex, var uri, int attemptIndex) = attempt;
        long startingTimestamp = Stopwatch.GetTimestamp();
        TaskCompletionSource<long> endingTimestampPromise = new();
        var responseFuture = GetAsync(uri, endingTimestampPromise, cancellationToken);
        RequestCollectedData requestCollectedData = new(
            uriIndex, uri, attemptIndex, startingTimestamp, endingTimestampPromise.Task, responseFuture);
        return RequestChannelWriter.WriteAsync(requestCollectedData, cancellationToken);
    }

    private async Task<HttpResponseMessage> GetAsync(
        Uri uri, TaskCompletionSource<long> endingTimestampPromise, CancellationToken cancellationToken)
    {
        try
        {
            var responseFuture = _httpClient.GetAsync(
                uri, HttpCompletionOption.ResponseContentRead, cancellationToken);
            return await responseFuture.ConfigureAwait(false);
        }
        finally
        {
            endingTimestampPromise.SetResult(Stopwatch.GetTimestamp());
        }
    }

    private static string GetOutputDirectoryOrFallback(ProjectDto projectDto, DateTime startTime)
    {
        string basename = $"{startTime.DayOfYear}_{startTime:HH-mm-ss}";
        if (projectDto.TryGetOutputDir(out string? outputRoot))
            return Path.Join(outputRoot, basename);
        return Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(ResponsiveFlow), basename);
    }

    private static List<Uri> GetValidUris(ProjectDto projectDto)
    {
        string[] urlStrings = projectDto.GetUrlsOrEmpty();
        List<Uri> uris = new(urlStrings.Length);
        foreach (string urlString in urlStrings)
        {
            if (Uri.TryCreate(urlString, UriKind.Absolute, out var uri))
                uris.Add(uri);
        }

        return uris;
    }
}
