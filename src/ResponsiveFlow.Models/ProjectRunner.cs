using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ResponseFuture = System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>;

namespace ResponsiveFlow;

internal sealed partial class ProjectRunner
{
    private const int ResponseChannelCapacity = 32;
    private const int RequestCount = 100;

    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly Channel<ResponseFuture> _responseChannel;
    private readonly List<Uri> _uris;

    private ProjectRunner(
        List<Uri> uris,
        string outputDirectory,
        HttpClient httpClient,
        Channel<ResponseFuture> responseChannel,
        ILogger logger)
    {
        _uris = uris;
        OutputDirectory = outputDirectory;
        _httpClient = httpClient;
        _responseChannel = responseChannel;
        _logger = logger;
    }

    internal IReadOnlyList<Uri> Uris => _uris;

    internal string OutputDirectory { get; }

    private ChannelReader<ResponseFuture> ResponseChannelReader => _responseChannel.Reader;

    private ChannelWriter<ResponseFuture> ResponseChannelWriter => _responseChannel.Writer;

    internal static ProjectRunner Create(ProjectDto projectDto, HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(projectDto);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var validUris = GetValidUris(projectDto);
        var startTime = DateTime.Now;
        string effectiveOutputDirectory = GetOutputDirectoryOrFallback(projectDto, startTime);
        var responseChannel = Channel.CreateBounded<ResponseFuture>(ResponseChannelCapacity);
        var logger = loggerFactory.CreateLogger<ProjectRunner>();
        return new(validUris, effectiveOutputDirectory, httpClient, responseChannel, logger);
    }

    internal async Task<Report> RunAsync(CancellationToken cancellationToken)
    {
        var cancellationTokenRegistration = cancellationToken.Register(() => _ = ResponseChannelWriter.TryComplete());
        await using (cancellationTokenRegistration.ConfigureAwait(false))
        {
            for (int i = 0; i < _uris.Count && !cancellationToken.IsCancellationRequested; ++i)
            {
                var uri = _uris[i];
                LogProcessUrl(uri.AbsoluteUri, i, _uris.Count);
                var uriWorkItems = Enumerable.Repeat(uri, RequestCount);
                var producingTask = Parallel.ForEachAsync(uriWorkItems, cancellationToken, Produce);
                await producingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }

        return new();
    }

    private ValueTask Produce(Uri uri, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.CompletedTask;
        var responseFuture = _httpClient.GetAsync(uri, HttpCompletionOption.ResponseContentRead, cancellationToken);
        return ResponseChannelWriter.WriteAsync(responseFuture, cancellationToken);
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
