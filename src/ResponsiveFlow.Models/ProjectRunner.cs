using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private const int ResponseChannelCapacity = 32;
    private const int RequestCount = 100;

    private readonly ConcurrentBag<ResponseInfo> _completedResponses = [];
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly Channel<ResponseInfo> _responseChannel;
    private readonly List<Uri> _uris;

    private ProjectRunner(
        List<Uri> uris,
        string outputDirectory,
        HttpClient httpClient,
        Channel<ResponseInfo> responseChannel,
        ILogger logger)
    {
        _uris = uris;
        OutputDirectory = outputDirectory;
        _httpClient = httpClient;
        _responseChannel = responseChannel;
        _logger = logger;
    }

    internal string OutputDirectory { get; }

    private ChannelReader<ResponseInfo> ResponseChannelReader => _responseChannel.Reader;

    private ChannelWriter<ResponseInfo> ResponseChannelWriter => _responseChannel.Writer;

    internal static ProjectRunner Create(ProjectDto projectDto, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var validUris = GetValidUris(projectDto);
        var startTime = DateTime.Now;
        string effectiveOutputDirectory = GetOutputDirectoryOrFallback(projectDto, startTime);
        var responseChannel = Channel.CreateBounded<ResponseInfo>(ResponseChannelCapacity);
        var logger = loggerFactory.CreateLogger<ProjectRunner>();
        return new(validUris, effectiveOutputDirectory, httpClient, responseChannel, logger);
    }

    internal Task<Report> RunAsync(CancellationToken cancellationToken)
    {
        if (ResponseChannelReader.Completion.IsCompleted)
            throw new InvalidOperationException("The response channel is already completed.");

        return RunCoreAsync(cancellationToken);
    }

    private async Task<Report> RunCoreAsync(CancellationToken cancellationToken)
    {
        var cancellationTokenRegistration = cancellationToken.Register(() => _ = ResponseChannelWriter.TryComplete());
        await using (cancellationTokenRegistration.ConfigureAwait(false))
        {
            var consumingTask = ConsumeAllAsync(cancellationToken);
            var producingTask = ProduceAllAsync(cancellationToken);
            await producingTask.ConfigureAwait(false);
            await consumingTask.ConfigureAwait(false);

            return new();
        }
    }

    private async Task ConsumeAllAsync(CancellationToken cancellationToken)
    {
        var responseInfoAsyncCollection = ResponseChannelReader.ReadAllAsync(cancellationToken);
        var consumingTask = Parallel.ForEachAsync(responseInfoAsyncCollection, cancellationToken, ConsumeBodyAsync);
        await consumingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task ProduceAllAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i < _uris.Count && !cancellationToken.IsCancellationRequested; ++i)
        {
            var uri = _uris[i];
            LogProcessUrl(uri.AbsoluteUri, i, _uris.Count);
            var uriWorkItems = Enumerable.Repeat(uri, RequestCount);
            int index = i;
            var producingTask = Parallel.ForEachAsync(
                uriWorkItems, cancellationToken, (u, c) => ProduceBodyAsync(index, u, c));
            await producingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private async ValueTask ConsumeBodyAsync(ResponseInfo responseInfo, CancellationToken cancellationToken)
    {
        _ = await responseInfo.Future.ConfigureAwait(false);
        _completedResponses.Add(responseInfo);
    }

    private ValueTask ProduceBodyAsync(int index, Uri uri, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.CompletedTask;
        var future = _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, cancellationToken);
        ResponseInfo responseInfo = new(index, uri, future);
        return ResponseChannelWriter.WriteAsync(responseInfo, cancellationToken);
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
